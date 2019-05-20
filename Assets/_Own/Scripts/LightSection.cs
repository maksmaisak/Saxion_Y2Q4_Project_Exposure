using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

/// <summary>
/// A collection of objects that are dark and shown with dots, but may be lit up.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class LightSection : MonoBehaviour
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

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
        
        HideAllRenderers();
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
    
    private void HideAllRenderers()
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
    
    private void FadeInRenderers()
    {
        foreach (GameObjectMaterialData data in gameObjectMaterialData)
        {
            if (!data.renderer)
                continue;

            data.renderer.sharedMaterials = data.sectionOnlyMaterials;
        }

        foreach (var sectionMaterial in gameObjectMaterialData.SelectMany(d => d.sectionOnlyMaterials).Distinct())
        {
            if (!sectionMaterial.HasProperty(ColorPropertyId))
                return;

            DOTween.ToAlpha(
                () => sectionMaterial.GetColor(ColorPropertyId), 
                color => sectionMaterial.SetColor(ColorPropertyId, color),
                0.0f,
                Random.Range(1.0f, 4.0f)
            ).SetTarget(sectionMaterial).From();
        };
    }

    private void FadeOutDots(float duration = 2.0f)
    {
        // TODO: preallocate this and keep it bundled with its particle system. Big performance hit allocating this.
        var particleBuffer = new ParticleSystem.Particle[dotsParticleSystem.main.maxParticles];
        int numAliveParticles = dotsParticleSystem.GetParticles(particleBuffer);

        for (int i = 0; i < numAliveParticles; ++i)
            particleBuffer[i].remainingLifetime = particleBuffer[i].startLifetime = Random.Range(0.0f, duration);

        dotsParticleSystem.SetParticles(particleBuffer, numAliveParticles);
      
        dotsParticleSystem.GetParticles(particleBuffer, numAliveParticles);
        Assert.IsTrue(particleBuffer.Take(numAliveParticles).All(p => p.remainingLifetime <= duration));

        dotsParticleSystem.Play();
    }
    
    private void FadeInLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = true;
            light.DOIntensity(0.0f, 2.0f).From().SetEase(Ease.InQuad);
        }

        DOTween.To(
            () => RenderSettings.ambientLight,
            color => RenderSettings.ambientLight = color,
            Color.black,
            2.0f
        ).From();
    }
}
