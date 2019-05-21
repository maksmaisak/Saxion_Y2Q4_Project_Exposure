using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Experimental.ParticleSystemJobs;
using Random = UnityEngine.Random;

/// <summary>
/// A collection of objects that are dark and shown with dots, but may be lit up.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class LightSection : MonoBehaviour
{
    private static readonly int ColorId    = Shader.PropertyToID("_BaseColor");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    struct GameObjectMaterialData
    {
        public Renderer renderer;
        public Material[] sharedMaterials;
        public Material[] sectionOnlyMaterials; // Same as sharedMaterials, but only shared between objects in the same section.
    }
    
    [SerializeField] List<Light> lights = new List<Light>();
    [SerializeField] List<GameObject> gameObjects = new List<GameObject>();
    [SerializeField] bool isGlobal = false;

    [Header("Hiding settings")] 
    [SerializeField] Material hiddenMaterial;
    [SerializeField] LayerMask exceptionLayer;

    [Header("Reveal settings")] 
    [SerializeField] int numDotsToReveal = 10000;
    [SerializeField] KeyCode fadeInKeyCode = KeyCode.Alpha1;
    [SerializeField] [Range(0.0f, 10.0f)] float revealDuration = 4.0f;

    private readonly List<GameObjectMaterialData> gameObjectMaterialData = new List<GameObjectMaterialData>();

    public bool isRevealed { get; private set; } = false;
    
    private ParticleSystem dotsParticleSystem;
    private int numDots = 0;
    
    void Awake()
    {
        if (isGlobal)
        {
            lights = new List<Light>(FindObjectsOfType<Light>());
            gameObjects = FindObjectsOfType<Renderer>()
                .Select(r => r.gameObject)
                .Where(go => !go.GetComponent<ParticleSystem>() && !exceptionLayer.ContainsLayer(go.layer))
                .ToList();
        }
        else
        {
            lights = GetComponentsInChildren<Light>().ToList();
            gameObjects = GetComponentsInChildren<Renderer>()
                .Select(r => r.gameObject)
                .Where(go => !go.GetComponent<ParticleSystem>() && !exceptionLayer.ContainsLayer(go.layer))
                .ToList();
        }
    }
    
    void Start()
    {
        dotsParticleSystem = GetComponentInChildren<ParticleSystem>();
        Assert.IsNotNull(dotsParticleSystem);
        
        HideRenderers();
        HideLights();
    }

    void Update()
    {
        if (!isRevealed && (numDots >= numDotsToReveal || Input.GetKeyDown(fadeInKeyCode)))
        {
            Reveal();
        }
    }

    public List<GameObject> GetGameObjects() => gameObjects;

    public void AddDots(IList<Vector3> positions)
    {
        if (isRevealed)
            return;
        
        numDots += positions.Count;

        int numParticlesAdded = positions.Count;
        int oldNumParticles   = dotsParticleSystem.particleCount;
        
        // Emit the particles
        dotsParticleSystem.Emit(numParticlesAdded);

        // Read out into a buffer, set positions, Write back in
        var particleBuffer = new ParticleSystem.Particle[positions.Count];
        dotsParticleSystem.GetParticles(particleBuffer, numParticlesAdded, oldNumParticles);
        for (int i = 0; i < numParticlesAdded; ++i)
            particleBuffer[i].position = positions[i];
        
        dotsParticleSystem.SetParticles(particleBuffer, numParticlesAdded, oldNumParticles);

        if (!dotsParticleSystem.isPlaying)
        {
            // To make it recalculate the bounds used for culling
            dotsParticleSystem.Simulate(0.01f, false, false, false);
        }
    }

    [ContextMenu("Reveal")]
    public void Reveal()
    {
        if (isRevealed)
            return;
        
        Debug.Log("Revealing LightSection: " + this);
        isRevealed = true;

        FadeInRenderers();

        FadeOutDots();

        FadeInLights();
    }
    
    private void HideRenderers()
    {
        Assert.IsNotNull(hiddenMaterial);
        
        gameObjects.RemoveAll(go => !go);
        foreach (GameObject go in gameObjects)
        {
            var renderer = go.GetComponent<Renderer>();
            if (!renderer)
                continue;

            Material[] rendererSharedMaterials = renderer.sharedMaterials;
            
            gameObjectMaterialData.Add(new GameObjectMaterialData 
            {
                renderer = renderer,
                sharedMaterials = (Material[])rendererSharedMaterials.Clone(),
                sectionOnlyMaterials = new Material[rendererSharedMaterials.Length]
            });

            for (int i = 0; i < renderer.sharedMaterials.Length; ++i)
                rendererSharedMaterials[i] = hiddenMaterial;

            renderer.sharedMaterials = rendererSharedMaterials;
        }
        
        // Ensure that different sections have different material instances.
        var materialToSectionMaterial = new Dictionary<Material, Material>();
        Material GetOrAddSectionMaterial(Material material)
        {
            bool wasPresent = materialToSectionMaterial.TryGetValue(material, out Material sectionMaterial);
            if (wasPresent) 
                return sectionMaterial;
            
            sectionMaterial = Instantiate(material);
            materialToSectionMaterial.Add(material, sectionMaterial);
            return sectionMaterial;
        }
        
        foreach (var data in gameObjectMaterialData)
            for (int i = 0; i < data.sharedMaterials.Length; ++i)
                data.sectionOnlyMaterials[i] = GetOrAddSectionMaterial(data.sharedMaterials[i]);
    }
    
    private void HideLights()
    {
        lights.ForEach(l => l.enabled = false);
    }
    
    private void FadeInRenderers()
    {
        foreach (GameObjectMaterialData data in gameObjectMaterialData)
        {
            if (!data.renderer)
                continue;

            data.renderer.sharedMaterials = data.sectionOnlyMaterials;
        }

        var sectionMaterials = gameObjectMaterialData
            .SelectMany(d => d.sectionOnlyMaterials)
            .Where(m => m.HasProperty(ColorId))
            .Distinct();

        foreach (var material in sectionMaterials)
        {
            var sequence = DOTween.Sequence();

            float targetAlpha = 1.0f;
            
            if (material.HasProperty(SrcBlendId))
            {
                int oldBlend = material.GetInt(SrcBlendId);
                int newBlend = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                if (oldBlend != newBlend)
                {
                    sequence.AppendCallback(() => material.SetInt(SrcBlendId, newBlend));
                    sequence.OnComplete(() => material.SetInt(SrcBlendId, oldBlend));
                }
                else
                {
                    targetAlpha = material.GetColor(ColorId).a;
                }
            }

            Color targetColor = material.GetColor(ColorId);
            Debug.Log("Color was: " + targetColor);
            targetColor.a = targetAlpha;
            material.SetColor(ColorId, Color.clear);
            var tweenAlpha = DOTween.To(
                () => material.GetColor(ColorId),
                color =>
                {
                    //Debug.Log("Set color: " + color);
                    material.SetColor(ColorId, color);
                },
                targetColor,
                revealDuration
            ).SetTarget(material);
            
            sequence.Append(tweenAlpha);
        }
    }

    private void FadeOutDots()
    {
        dotsParticleSystem.SetJob(new FadeOutParticlesJob {duration = revealDuration});
        dotsParticleSystem.Play();
    }
    
    private void FadeInLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = true;
            light.DOIntensity(0.0f, revealDuration).From().SetEase(Ease.OutQuad);
        }
    }

    struct FadeOutParticlesJob : IParticleSystemJob
    {
        [ReadOnly] public float duration;
        
        public void ProcessParticleSystem(ParticleSystemJobData jobData)
        {
            NativeArray<float> inverseStartLifetimes = jobData.inverseStartLifetimes;
            var random = new System.Random();
            var ease = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(Ease.OutQuad);
            for (int i = 0; i < jobData.count; ++i)
            {
                float t = ease(1.0f - (float)random.NextDouble(), 1.0f, 0.0f, 0.0f);
                inverseStartLifetimes[i] = 1.0f / (t * duration);
            }
        }
    }
}
