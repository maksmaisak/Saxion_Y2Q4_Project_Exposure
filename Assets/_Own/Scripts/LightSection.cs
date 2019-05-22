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
using Random = UnityEngine.Random;

/// <summary>
/// A collection of objects that are dark and shown with dots, but may be lit up.
/// </summary>
public class LightSection : MonoBehaviour
{
    private static readonly int ColorId    = Shader.PropertyToID("_BaseColor");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    struct RendererData
    {
        public Renderer renderer;
        public Material[] sharedMaterials;
        public Material[] sectionOnlyMaterials; // Same as sharedMaterials, but only shared between objects in the same section.
    }
    
    struct GameObjectSavedData
    {
        public RendererData[] renderers;
        public ParticleSystem[] particleSystems;
    }

    [Header("General")]
    [SerializeField] ParticleSystem dotsParticleSystemPrefab;
    [Tooltip("A global section will contain all objects in the scene")]
    [SerializeField] bool isGlobal = false;

    [Header("Hiding settings")] 
    [SerializeField] Material hiddenMaterial;
    [SerializeField] LayerMask exceptionLayer;

    [Header("Reveal settings")] 
    [SerializeField] int numDotsToReveal = 10000;
    [SerializeField] KeyCode fadeInKeyCode = KeyCode.Alpha1;
    [SerializeField] [Range(0.0f, 10.0f)] float revealDuration = 4.0f;
    
    [Header("Debug")]
    [SerializeField] List<Light> lights = new List<Light>();
    [SerializeField] List<GameObject> gameObjects = new List<GameObject>();

    private readonly List<GameObjectSavedData> gameObjectData = new List<GameObjectSavedData>();

    public bool isRevealed { get; private set; } = false;
    
    private ParticleSystem dotsParticleSystem;
    private readonly ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[1_000_000];
    private int numDots = 0;
    
    void Awake()
    {
        Assert.IsNotNull(dotsParticleSystemPrefab);
        dotsParticleSystem = Instantiate(dotsParticleSystemPrefab, transform, worldPositionStays: true);
        Assert.IsNotNull(dotsParticleSystem);

        IEnumerable<GameObject> withRenderer;
        IEnumerable<GameObject> withParticleSystem;
        
        if (isGlobal)
        {
            lights = FindObjectsOfType<Light>().ToList();

            withRenderer       = FindObjectsOfType<Renderer>().Select(r => r.gameObject);
            withParticleSystem = FindObjectsOfType<ParticleSystem>().Select(r => r.gameObject);
        }
        else
        {
            lights = GetComponentsInChildren<Light>().ToList();

            withRenderer       = GetComponentsInChildren<Renderer>().Select(r => r.gameObject);
            withParticleSystem = GetComponentsInChildren<ParticleSystem>().Select(r => r.gameObject);
        }

        gameObjects = Enumerable.Union(withRenderer, withParticleSystem)
            .Distinct()
            .Where(go => go != dotsParticleSystem.gameObject && !exceptionLayer.ContainsLayer(go.layer))
            .ToList();
    }
    
    void Start()
    {
        HideGameObjects();
        HideLights();
    }

    void Update()
    {
        if ((!isRevealed && numDotsToReveal >= 0 && numDots >= numDotsToReveal) || Input.GetKeyDown(fadeInKeyCode))
            Reveal();
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

        // Read out into a buffer, set positions, write back in
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

        RevealGameObjects();
        HideDots();
        RevealLights();
        
        new OnRevealEvent().PostEvent();
    }
    
    private void HideGameObjects()
    {
        Assert.IsNotNull(hiddenMaterial);
        
        gameObjects.RemoveAll(go => !go);
        gameObjectData.AddRange(gameObjects.Select(GetSaveData));
        PreventMaterialSharingBetweenSectionsInSavedData();
    }
    
    private GameObjectSavedData GetSaveData(GameObject go) 
    {
        var data = new GameObjectSavedData();
            
        data.renderers = go.GetComponents<Renderer>().Select(r =>
        {
            Material[] rendererSharedMaterials = r.sharedMaterials;

            r.sharedMaterials = Enumerable.Repeat(hiddenMaterial, rendererSharedMaterials.Length).ToArray();

            return new RendererData {
                renderer = r,
                sharedMaterials = (Material[])rendererSharedMaterials.Clone(),
                sectionOnlyMaterials = new Material[rendererSharedMaterials.Length]
            };
        }).ToArray();

        data.particleSystems = go.GetComponents<ParticleSystem>().Select(ps =>
        {
            ps.Stop();
            return ps;
        }).ToArray();

        return data;
    }

    /// Ensures that different sections have different material instances.
    private void PreventMaterialSharingBetweenSectionsInSavedData()
    {
        var materialToSectionMaterial = new Dictionary<Material, Material>();
        Material GetOrAddSectionMaterial(Material material)
        {
            if (!material)
                return null;
            
            bool wasPresent = materialToSectionMaterial.TryGetValue(material, out Material sectionMaterial);
            if (wasPresent) 
                return sectionMaterial;
            
            sectionMaterial = Instantiate(material);
            materialToSectionMaterial.Add(material, sectionMaterial);
            return sectionMaterial;
        }
        
        foreach (RendererData rendererData in gameObjectData.SelectMany(d => d.renderers))
            for (int i = 0; i < rendererData.sharedMaterials.Length; ++i)
                rendererData.sectionOnlyMaterials[i] = GetOrAddSectionMaterial(rendererData.sharedMaterials[i]);
    }

    private void HideLights()
    {
        lights.ForEach(l => l.enabled = false);
    }
    
    private void RevealGameObjects()
    {
        // Switch to section-wide shared materials for the duration of the tweens.
        gameObjectData
            .SelectMany(d => d.renderers)
            .Where(d => d.renderer)
            .Each(d => d.renderer.sharedMaterials = d.sectionOnlyMaterials);

        foreach (ParticleSystem particleSystem in gameObjectData.SelectMany(d => d.particleSystems).Where(p => p && !p.isPlaying))
            particleSystem.Play();
        
        // Fade in the section-wide materials
        var sectionSequence = DOTween.Sequence();
        gameObjectData
            .SelectMany(d => d.renderers)
            .SelectMany(d => d.sectionOnlyMaterials)
            .Where(m => m && m.HasProperty(ColorId))
            .Distinct()
            .Each(material => sectionSequence.Join(FadeInMaterial(material)));

        // Switch to globally shared materials again once done tweening
        sectionSequence.OnComplete(() =>
        {
            gameObjectData
                .SelectMany(d => d.renderers)
                .Where(d => d.renderer)
                .Each(d => d.renderer.sharedMaterials = d.sharedMaterials);
        });
        
        gameObjectData.Clear();
    }
    
    private void HideDots()
    {
        Assert.IsTrue(dotsParticleSystem.particleCount <= particleBuffer.Length, $"The dots particle system has more than {particleBuffer.Length} particles, which is not supported.");

        var random = new System.Random();
        var ease = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(Ease.OutQuad);
        
        int numParticles = dotsParticleSystem.GetParticles(particleBuffer, particleBuffer.Length);
        for (int i = 0; i < numParticles; ++i)
        {
            float t = ease(1.0f - (float)random.NextDouble(), 1.0f, 0.0f, 0.0f);
            particleBuffer[i].startLifetime = particleBuffer[i].remainingLifetime = t * revealDuration;
        }
        
        dotsParticleSystem.SetParticles(particleBuffer, numParticles);
    }
    
    private void RevealLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = true;
            light.DOIntensity(0.0f, revealDuration).From().SetEase(Ease.OutQuad);
        }
    }
    
    private Sequence FadeInMaterial(Material material)
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
        targetColor.a = targetAlpha;
        material.SetColor(ColorId, Color.clear);
        var tweenAlpha = DOTween.To(
            () => material.GetColor(ColorId),
            color => material.SetColor(ColorId, color),
            targetColor,
            revealDuration
        ).SetTarget(material);

        sequence.Append(tweenAlpha);
        
        return sequence;
    }
}
