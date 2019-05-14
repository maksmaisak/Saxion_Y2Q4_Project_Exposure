﻿using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class TestDots : MonoBehaviour
{
    [SerializeField] float sphereCastDistance = 200.0f;
    [SerializeField] float sphereCastRadius = 0.2f;

    [SerializeField] float dotEmissionConeAngle = 10.0f;
    [SerializeField] float maxDotDistanceFromSurfacePointAlongOriginalRayDirection = 1.0f;
    
    [SerializeField] FlyingSphere flyingSpherePrefab;
    [SerializeField] GameObject wavePulsePrefab;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => Camera.main);
        
        ProbeFrom(Camera.main);
        CreateWavePulse();

        //Probe(new Ray(transform.position, transform.forward));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (Input.GetKeyDown(KeyCode.Space))
            Probe(new Ray(transform.position, transform.forward), true);
        
        if (Input.GetKeyDown(KeyCode.P))
            CreateWavePulse();
    }

    private void ProbeFrom(Camera cam)
    {
        Assert.IsNotNull(cam);
        
        const int numRaysPerAxis = 20;
        for (int i = 0; i < numRaysPerAxis; ++i)
        {
            for (int j = 0; j < numRaysPerAxis; ++j)
            {
                var viewportPos = new Vector3(i / (float)numRaysPerAxis, j / (float)numRaysPerAxis);
                Ray ray = cam.ViewportPointToRay(viewportPos);

                Debug.DrawRay(ray.origin, ray.direction, Color.white, 10.0f, true);
                Probe(ray);
            }
        }

        CreateWavePulse();
    }

    private void CreateWavePulse()
    {
        Assert.IsNotNull(wavePulsePrefab);

        const float duration = 2.0f;

        GameObject pulse = Instantiate(wavePulsePrefab, transform.position, Quaternion.identity);
        Transform tf = pulse.transform;
        
        tf.localScale = Vector3.zero;
        tf.DOScale(20, duration).SetEase(Ease.Linear);
        
        Destroy(pulse, duration);
    }
    
    private void Probe(Ray ray, bool spawnFlyingSphere = false)
    {
        LayerMask layerMask = DotsManager.instance.GetDotsSurfaceLayerMask();
        RaycastHit hit;
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out hit, sphereCastDistance, layerMask, QueryTriggerInteraction.Ignore);
        if (!didHit)
            return;

        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = ray,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotEmissionConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRayDirection =
                maxDotDistanceFromSurfacePointAlongOriginalRayDirection
        };

        if (spawnFlyingSphere)
        {
            Assert.IsNotNull(flyingSpherePrefab);

            FlyingSphere flyingSphere = Instantiate(
                flyingSpherePrefab,
                hit.point,
                Quaternion.identity
            );

            flyingSphere.SetTarget(ray.origin);
            flyingSphere.highlightLocation = highlightLocation;
        }
        else 
        {
            // TODO: For testing purposes move this out of this block to paint the dots when space is hit.
            DotsManager.instance.Highlight(highlightLocation);
        }
    }
}
