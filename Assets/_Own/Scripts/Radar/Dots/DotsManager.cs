using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

/// A globally-accessible service for managing dot-based object highlighting.
public class DotsManager : Singleton<DotsManager>
{
    public const int MaxNumDotsPerHighlight = 1600;

    [Tooltip("Dots can only appear on surfaces with these layers.")] 
    [SerializeField] LayerMask dotsSurfaceLayerMask = Physics.DefaultRaycastLayers;
    [SerializeField] float maxDotSpawnDistance = 200.0f;
    [SerializeField] int numDotsGeneratorsToPreCreate = 4;
    [Space]
    [SerializeField] DotsAnimator dotsAnimatorPrefab;
    [SerializeField] int numAnimatorsToPreCreate = 8;
    [SerializeField] float dotsAnimationDuration = 1.0f;

    public DotsRegistry registry { get; private set; }

    private LightSection [] lightSections;
    private List<Vector3>[] positionsBuffers;

    private readonly Dictionary<Collider, int> colliderToLightSectionIndex = new Dictionary<Collider, int>();
    private readonly List<DotsGenerator> allDotsGenerators = new List<DotsGenerator>();
    private readonly Stack<DotsGenerator> freeDotsGenerators = new Stack<DotsGenerator>();
    private readonly Stack<DotsAnimator>  freeDotsAnimators = new Stack<DotsAnimator>();
    
    void Start()
    {
        Physics.queriesHitTriggers = false;
        
        Assert.IsNotNull(dotsAnimatorPrefab);
      
        registry = new DotsRegistry();
        PreCreateDotsGenerators();
        PreCreateDotsAnimators();

        lightSections = FindObjectsOfType<LightSection>().ToArray();
        PopulateColliderToLightSectionIndex(colliderToLightSectionIndex);
        positionsBuffers = Enumerable
            .Range(0, lightSections.Length)
            .Select(i => new List<Vector3>(MaxNumDotsPerHighlight))
            .ToArray();

        Assert.IsTrue(lightSections.Length > 0, "There must be at least one LightSection in the scene!");
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var dotsGenerator in allDotsGenerators)
            dotsGenerator?.Dispose();
    }

    public LayerMask GetDotsSurfaceLayerMask() => dotsSurfaceLayerMask;
   
    public void Highlight(RadarHighlightLocation location, Vector3 dotsOrigin) => Highlight(location, dotsOrigin, dotsSurfaceLayerMask);

    public void Highlight(RadarHighlightLocation location, Vector3 dotsOrigin, LayerMask layerMask)
    {
        StartCoroutine(HighlightCoroutine(GetFreeDotsGenerator(), location, dotsOrigin, layerMask));
    }
    
    public LightSection GetSection(Collider collider)
    {
        bool isInSection = colliderToLightSectionIndex.TryGetValue(collider, out int sectionIndex);
        if (!isInSection)
            return null;

        return lightSections[sectionIndex];
    }
   
    public bool IsHidden(Collider collider)
    { 
        bool isInSection = colliderToLightSectionIndex.TryGetValue(collider, out int sectionIndex);
        if (!isInSection)
            return false;

        return !lightSections[sectionIndex].isRevealed;
    }
   
    public void DrawDebugInfoInEditor()
    {
        registry?.DrawDebugInfoInEditor();
    }

    private IEnumerator HighlightCoroutine(DotsGenerator dotsGenerator, RadarHighlightLocation location, Vector3 dotsOrigin, LayerMask layerMask)
    {
        Assert.IsFalse(dotsGenerator.isWorkingJob);
        
        dotsGenerator.Generate(ref location, dotsOrigin, layerMask);
        yield return new WaitUntil(() => dotsGenerator.isJobCompleted);
        
        Assert.IsTrue(dotsGenerator.isJobCompleted);
        ProcessHighlightJobResult(dotsGenerator.Complete());
        freeDotsGenerators.Push(dotsGenerator);
    }

    private void ProcessHighlightJobResult(DotsGenerator.Results results)
    {
        FillPositionBuffersFromRaycastResults(results.hits, ref results.highlightLocation);

        for (int i = 0; i < lightSections.Length; ++i)
        {
            List<Vector3> positionsBuffer = positionsBuffers[i];
            if (positionsBuffer.Count == 0)
                continue;
            
            positionsBuffer.ForEach(registry.RegisterDot);
            GetFreeDotsAnimator().AnimateDots(
                positionsBuffer, results.dotsOrigin, dotsAnimationDuration, 
                lightSections[i], 
                onDoneCallback: (animator, positions) => freeDotsAnimators.Push(animator)
            );
            
            new OnHighlightEvent(positionsBuffer.AsReadOnly()).SetDeliveryType(MessageDeliveryType.Immediate).PostEvent();
            positionsBuffer.Clear();
        }
    }
    
    private void FillPositionBuffersFromRaycastResults(NativeArray<RaycastHit> hits, ref RadarHighlightLocation location)
    {
        for (int i = 0; i < MaxNumDotsPerHighlight; ++i)
        {
            RaycastHit dotHit = hits[i];
            if (!dotHit.collider) // This only works as long as maxHits is one.
                continue;

            // Filter out ones that are much further away from ray origin than the sphere origin.
            Vector3 delta = dotHit.point - location.pointOnSurface;
            float distanceAlongRay = Vector3.Dot(delta, location.originalRay.direction);
            if (distanceAlongRay > location.maxDotDistanceFromSurfacePointAlongOriginalRay)
                continue;
            
            if (!colliderToLightSectionIndex.TryGetValue(dotHit.collider, out int sectionIndex))
                continue;

            if (lightSections[sectionIndex].isRevealed)
                continue;

            positionsBuffers[sectionIndex].Add(dotHit.point);
        }
    }
    
    private void PreCreateDotsGenerators()
    {
        while (freeDotsGenerators.Count < numDotsGeneratorsToPreCreate)
            freeDotsGenerators.Push(CreateDotsGenerator());
    }
    
    private void PreCreateDotsAnimators()
    {
        while (freeDotsAnimators.Count < numAnimatorsToPreCreate)
            freeDotsAnimators.Push(CreateDotsAnimator());
    }

    private DotsGenerator GetFreeDotsGenerator()
    {
        if (freeDotsGenerators.Count > 0)
            return freeDotsGenerators.Pop();

        return CreateDotsGenerator();
    }
    
    private DotsAnimator GetFreeDotsAnimator()
    {
        if (freeDotsAnimators.Count > 0)
            return freeDotsAnimators.Pop();

        return CreateDotsAnimator();
    }

    private DotsGenerator CreateDotsGenerator()
    {
        var generator = new DotsGenerator(maxDotSpawnDistance);
        allDotsGenerators.Add(generator);
        return generator;
    }

    private DotsAnimator CreateDotsAnimator()
    {
        return Instantiate(dotsAnimatorPrefab, parent: transform);
    }

    private void PopulateColliderToLightSectionIndex(Dictionary<Collider, int> dictionary)
    {
        for (int i = 0; i < lightSections.Length; ++i)
        {
            lightSections[i]
                .GetGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Collider>())
                .Distinct()
                .Each(c => dictionary.Add(c, i));
        }
    }
}