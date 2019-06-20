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
    [Space] 
    [SerializeField] DotsAnimator dotsAnimatorPrefab;
    [SerializeField] int numAnimatorsToPreCreate = 8;
    [SerializeField] float dotsAnimationDuration = 1.0f;

    public DotsRegistry registry { get; private set; }

    private LightSection [] lightSections;
    private List<Vector3>[] positionsBuffers;

    private readonly Dictionary<Collider, int> colliderToLightSectionIndex = new Dictionary<Collider, int>();
    private readonly Stack<DotsAnimator> freeDotsAnimators = new Stack<DotsAnimator>();
    
    private NativeArray<RaycastCommand> commands;
    private NativeArray<RaycastHit>     hits;

    protected override void Awake()
    {
        base.Awake();
        
        commands = new NativeArray<RaycastCommand>(MaxNumDotsPerHighlight, Allocator.Persistent);
        hits     = new NativeArray<RaycastHit>    (MaxNumDotsPerHighlight, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (commands.IsCreated)
            commands.Dispose();
        
        if (hits.IsCreated)
            hits.Dispose();
    }

    void Start()
    {
        Physics.queriesHitTriggers = false;
        
        Assert.IsNotNull(dotsAnimatorPrefab);
      
        registry = new DotsRegistry();
        PreCreateDotsAnimators();

        lightSections = FindObjectsOfType<LightSection>().ToArray();
        positionsBuffers = Enumerable
            .Range(0, lightSections.Length)
            .Select(i => new List<Vector3>(MaxNumDotsPerHighlight))
            .ToArray();

        for (int i = 0; i < lightSections.Length; ++i)
        {
            var colliders = lightSections[i].GetGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Collider>())
                .Distinct();
         
            foreach (Collider col in colliders) 
                colliderToLightSectionIndex.Add(col, i);
        }

        Assert.IsTrue(lightSections.Length > 0, "There must be at least one LightSection in the scene!");
    }

    public LayerMask GetDotsSurfaceLayerMask() => dotsSurfaceLayerMask;
   
    public void Highlight(RadarHighlightLocation location, Vector3 dotsOrigin) => Highlight(location, dotsOrigin, dotsSurfaceLayerMask);

    public void Highlight(RadarHighlightLocation location, Vector3 dotsOrigin, LayerMask layerMask)
    {
        //Debug.DrawLine(location.originalRay.origin, location.pointOnSurface, Color.green, 20.0f);
        
        GenerateRaycastCommands(ref location, layerMask);
        RaycastCommand.ScheduleBatch(commands, hits, 1).Complete();
        FillPositionBuffersFromRaycastResults(ref location);

        for (int i = 0; i < lightSections.Length; ++i)
        {
            List<Vector3> positionsBuffer = positionsBuffers[i];
            if (positionsBuffer.Count == 0)
                continue;
            
            GetFreeDotsAnimator().AnimateDots(positionsBuffer, dotsOrigin, dotsAnimationDuration, onDoneCallback: (animator, positions) =>
            {
                freeDotsAnimators.Push(animator);
            });
            lightSections[i].AddDots(positionsBuffer, dotsAnimationDuration);
            positionsBuffer.ForEach(registry.RegisterDot);
            
            new OnHighlightEvent(positionsBuffer.AsReadOnly()).SetDeliveryType(MessageDeliveryType.Immediate).PostEvent();
            positionsBuffer.Clear();
        }
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
    
    private void GenerateRaycastCommands(ref RadarHighlightLocation location, LayerMask layerMask)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, location.originalRay.direction);
        float distanceFromOrigin = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
        float displacementRadius = distanceFromOrigin * Mathf.Tan(Mathf.Deg2Rad * location.dotEmissionConeAngle);
        
        for (int i = 0; i < MaxNumDotsPerHighlight; ++i)
        {
            Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
            commands[i] = new RaycastCommand(location.originalRay.origin, target - location.originalRay.origin, maxDotSpawnDistance, layerMask);
        }
    }
    
    private void FillPositionBuffersFromRaycastResults(ref RadarHighlightLocation location)
    {
        for (int i = 0; i < MaxNumDotsPerHighlight; ++i)
        {
            RaycastHit dotHit = hits[i];
            if (!dotHit.collider) // This only works as long as maxHits is one.
                continue;

            Vector3 delta = dotHit.point - location.pointOnSurface;
            
            // The one below is actually correct, but this worked better, keeping it for now until (TODO) an angle-based solution is made.
            float distanceAlongRay = Vector3.Dot(delta, location.originalRay.direction);
            //float distanceAlongRay = Mathf.Abs(Vector3.Dot(delta, location.originalRay.direction));
            if (distanceAlongRay > location.maxDotDistanceFromSurfacePointAlongOriginalRay)
                continue;

            if (!colliderToLightSectionIndex.TryGetValue(dotHit.collider, out int sectionIndex))
                continue;

            if (lightSections[sectionIndex].isRevealed)
                continue;

            positionsBuffers[sectionIndex].Add(dotHit.point);
        }
    }

    private DotsAnimator GetFreeDotsAnimator()
    {
        if (freeDotsAnimators.Count > 0)
            return freeDotsAnimators.Pop();

        return CreateDotsAnimator();
    }
    
    private void PreCreateDotsAnimators()
    {
        while (freeDotsAnimators.Count < numAnimatorsToPreCreate)
            freeDotsAnimators.Push(CreateDotsAnimator());
    }

    private DotsAnimator CreateDotsAnimator()
    {
        return Instantiate(dotsAnimatorPrefab, parent: transform);
    }
}