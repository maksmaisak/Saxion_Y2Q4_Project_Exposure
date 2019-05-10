using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// A collection of objects that are dark and shown with dots, but may be lit up.
/// </summary>
public class LightZone : MonoBehaviour
{
    [SerializeField] List<Light> lights = new List<Light>();
    [SerializeField] List<GameObject> gameObjects = new List<GameObject>();
    [SerializeField] bool isGlobal = false;

    [Space]
    [SerializeField] KeyCode fadeInKeyCode = KeyCode.Alpha1;

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
            gameObjects = new List<GameObject>(FindObjectsOfType<Renderer>().Select(r => r.gameObject));
        }
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
        foreach (GameObject go in gameObjects)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer)
                FadeInRenderer(renderer);
            
            var particleSystem = go.GetComponent<ParticleSystem>();
            if (particleSystem)
                FadeOutParticles(particleSystem);
        }

        FadeInLights();
    }

    private void FadeInRenderer(Renderer renderer)
    {
        renderer.enabled = true;
        
        Material material = renderer.material;

        if (!material.HasProperty(colorPropertyId))
            return;

        DOTween.ToAlpha(
            () => material.GetColor(colorPropertyId), 
            color => material.SetColor(colorPropertyId, color),
            0.0f,
            Random.Range(1.0f, 4.0f)
        ).SetTarget(material).From();
    }

    void FadeOutParticles(ParticleSystem particleSystem)
    {
        // TODO: preallocate this and keep it bundled with its particle system.
        var particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        int numAliveParticles = particleSystem.GetParticles(particles);

        for (int i = 0; i < numAliveParticles; ++i)
            particles[i].remainingLifetime = particles[i].startLifetime = Random.Range(0.0f, 2.0f);

        particleSystem.SetParticles(particles, numAliveParticles);
        
        particleSystem.Play();
        Debug.Log(particleSystem.gameObject.name);
        Debug.Log(particleSystem.isPlaying);
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
