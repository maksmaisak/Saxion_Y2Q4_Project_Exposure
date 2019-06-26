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
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// A collection of objects that are dark and shown with dots, but may be lit up.
/// </summary>
public class LightSection : MonoBehaviour
{
    private static readonly int ColorId    = Shader.PropertyToID("_BaseColor");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    struct RendererData
    {
        public Renderer renderer;
        public Material[] sharedMaterials;
        public Material[] sectionOnlyMaterials; // Same as sharedMaterials, but only shared between objects in the same section.
    }
    
    struct GameObjectSavedData
    {
        public RendererData[] renderers;
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
    [SerializeField] [Range(0.0f, 10.0f)] float revealDuration = 4.0f;
    public UnityEvent onReveal;
    
    [Header("Debug")]
    [SerializeField] KeyCode fadeInKeyCode = KeyCode.Alpha1;
    [SerializeField] List<Light> lights = new List<Light>();
    [SerializeField] List<GameObject> gameObjects;

    private readonly List<GameObjectSavedData> gameObjectData = new List<GameObjectSavedData>();

    public bool isRevealed { get; private set; } = false;
    
    private ParticleSystem dotsParticleSystem;
    private NativeArray<float> multipliersBuffer;
    private int numDots = 0;
    
    void Awake()
    {
        Assert.IsNotNull(dotsParticleSystemPrefab);
        dotsParticleSystem = Instantiate(dotsParticleSystemPrefab, transform, worldPositionStays: true);
        Assert.IsNotNull(dotsParticleSystem);
        multipliersBuffer = new NativeArray<float>(Mathf.Max(dotsParticleSystem.main.maxParticles, 1_000_000), Allocator.Persistent);
        for (int i = 0; i < multipliersBuffer.Length; ++i)
            multipliersBuffer[i] = 1.0f / Random.Range(1.0f, 1.01f);

        gameObjects = FindGameObjects();
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

    void OnDestroy()
    {
        if (multipliersBuffer.IsCreated)
            multipliersBuffer.Dispose();
    }

    public List<GameObject> GetGameObjects() => gameObjects;

    public float GetRevealProgress() => Mathf.Clamp01((float)numDots / numDotsToReveal);
    
    public void AddDots(IList<Vector3> positions, float particleAppearDelay = 0.0f)
    {
        if (isRevealed)
            return;
        
        numDots += positions.Count;

        if (particleAppearDelay <= 0.0f)
        {
            dotsParticleSystem.AddParticles(positions);
        }
        else
        {
            // TODO preallocate the buffer
            var buffer = new List<Vector3>(positions);
            this.Delay(particleAppearDelay, () => dotsParticleSystem.AddParticles(buffer));
        }
    }

    [ContextMenu("Reveal")]
    public void Reveal()
    {
        if (isRevealed)
            return;
        
        Debug.Log("Revealing LightSection: " + this);
        isRevealed = true;

        StopAllCoroutines();
        RevealGameObjects();
        HideDots();
        RevealLights();
        
        onReveal?.Invoke();
        new OnRevealEvent(this).PostEvent();
    }

    private List<GameObject> FindGameObjects()
    {
        IEnumerable<GameObject> withRenderer;
        IEnumerable<GameObject> withCollider;
        
        if (isGlobal)
        {
            lights = FindObjectsOfType<Light>().ToList();

            withRenderer       = FindObjectsOfType<Renderer>().Select(r => r.gameObject);
            withCollider       = FindObjectsOfType<Collider>().Select(c => c.gameObject);
        }
        else
        {
            lights = GetComponentsInChildren<Light>().ToList();

            withRenderer       = GetComponentsInChildren<Renderer>().Select(r => r.gameObject);
            withCollider       = GetComponentsInChildren<Collider>().Select(c => c.gameObject);
        }

        return withRenderer
            .Union(withCollider)
            .Distinct()
            .Where(go => go != dotsParticleSystem.gameObject && !exceptionLayer.ContainsLayer(go.layer))
            .ToList();
    }
    
    private void HideGameObjects()
    {
        Assert.IsNotNull(hiddenMaterial);
        
        gameObjects.RemoveAll(go => !go);
        gameObjectData.AddRange(gameObjects.Select(MakeSaveData));
        PreventMaterialSharingBetweenSectionsInSavedData();
    }
    
    private GameObjectSavedData MakeSaveData(GameObject go) 
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
        Assert.IsTrue(dotsParticleSystem.particleCount <= multipliersBuffer.Length, $"The dots particle system has more than {multipliersBuffer.Length} particles, which is not supported.");

        var camera = Camera.main;
        dotsParticleSystem.SetJob(new FadeOutParticlesJob
        {
            multipliers = multipliersBuffer, 
            dt = Time.fixedDeltaTime / revealDuration,
            origin = camera ? camera.transform.position : transform.position
        });
        
        this.Delay(revealDuration, () =>
        {
            dotsParticleSystem.ClearJob();
            dotsParticleSystem.Clear();
        });
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
                sequence.onComplete += () => material.SetInt(SrcBlendId, oldBlend);
            }
            else
            {
                targetAlpha = material.GetColor(ColorId).a;
            }
        }

        if (material.HasProperty(ColorId))
        {
            Color targetColor = material.GetColor(ColorId);
            targetColor.a = targetAlpha;
            material.SetColor(ColorId, Color.clear);
            var tweenColor = DOTween.To(
                () => material.GetColor(ColorId),
                color => material.SetColor(ColorId, color),
                targetColor,
                revealDuration
            ).SetTarget(material);
            sequence.Append(tweenColor);
        }

        if (material.HasProperty(EmissionColorId))
        {
            var tweenEmission = DOTween.To(
                () => material.GetColor(EmissionColorId),
                color => material.SetColor(EmissionColorId, color),
                Color.black,
                revealDuration
            ).SetTarget(material).From();
            sequence.Join(tweenEmission);
        }

        return sequence;
    }
}
