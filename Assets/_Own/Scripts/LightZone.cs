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
public class LightZone : MonoBehaviour
{
    struct GameObjectSavedData
    {
        public Renderer renderer;
        public Material[] sharedMaterials;
    }
    
    [SerializeField] List<Light> lights = new List<Light>();
    [SerializeField] List<GameObject> gameObjects = new List<GameObject>();
    [SerializeField] bool isGlobal = false;

    [Space] 
    [SerializeField] private Material hiddenMaterial;

    [Space]
    [SerializeField] KeyCode fadeInKeyCode = KeyCode.Alpha1;

    private Dictionary<GameObject, GameObjectSavedData> savedGameObjectData = new Dictionary<GameObject, GameObjectSavedData>();

    private int colorPropertyId;

    void Awake()
    {
        colorPropertyId = Shader.PropertyToID("_Color");
    }

    void Start()
    {
        if (isGlobal)
        {
            lights = new List<Light>(FindObjectsOfType<Light>());
            gameObjects = new List<GameObject>(FindObjectsOfType<Renderer>().Select(r => r.gameObject).Where(go => !go.GetComponent<ParticleSystem>()));
        }
        
        HideAllRenderers();
    }

    void Update()
    {
        if (Input.GetKeyDown(fadeInKeyCode))
        {
            FadeIn();
        }
    }
    
    [ContextMenu("Fade in")]
    public void FadeIn()
    {
        foreach (var kvp in savedGameObjectData)
        {
            GameObject go = kvp.Key;
            if (!go || !kvp.Value.renderer)
                continue;

            FadeInRenderer(kvp.Value);
        }
        
        DotsManager.instance.FadeOutDots();

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
            
            savedGameObjectData.Add(go, new GameObjectSavedData
            {
                renderer = renderer,
                sharedMaterials = (Material[])rendererSharedMaterials.Clone()
            });

            for (int i = 0; i < renderer.sharedMaterials.Length; ++i)
            {
                rendererSharedMaterials[i] = hiddenMaterial;
            }

            renderer.sharedMaterials = rendererSharedMaterials;
        }
    }

    private void FadeInRenderer(GameObjectSavedData data)
    {
        data.renderer.sharedMaterials = data.sharedMaterials;

        // TODO use sharedMaterials here
        foreach (var material in data.renderer.materials)
        {
            if (!material.HasProperty(colorPropertyId))
                return;

            DOTween.ToAlpha(
                () => material.GetColor(colorPropertyId), 
                color => material.SetColor(colorPropertyId, color),
                0.0f,
                Random.Range(1.0f, 4.0f)
            ).SetTarget(material).From();
        }
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
