using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class TestDots : MonoBehaviour
{
    [SerializeField] float sphereCastRadius = 0.2f;
    [SerializeField] float maxDotDistanceFromSurfacePointAlongOriginalRayDirection = 1.0f;
    
    [Header("Wave pulse settings")]
    [SerializeField] GameObject wavePulsePrefab;
    [SerializeField] [Range(0.0f, 360.0f)] float wavePulseAngleHorizontal = 90.0f;
    [SerializeField] [Range(0.0f, 360.0f)] float wavePulseAngleVertical   = 90.0f;
    [SerializeField] float wavePulseSpeed = 10.0f;
    [SerializeField] float wavePulseMaxRange = 20.0f;
    [SerializeField] int maxNumWavespheresPerPulse = 10;
    [Space]
    [SerializeField] FlyingSphere flyingSpherePrefab;
    
    private new Camera camera;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => camera = Camera.main);
        
        //ProbeFrom(camera);

        //Probe(new Ray(transform.position, transform.forward));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (Input.GetKeyDown(KeyCode.Space) && camera)
            ProbeFrom(camera);
        //Probe(new Ray(transform.position, transform.forward), true);
        
        if (Input.GetKeyDown(KeyCode.P))
            CreateWavePulse();
    }

    private void ProbeFrom(Camera cam)
    {
        Assert.IsNotNull(cam);
        
        float[] ranges =
        {
            0.0f,
            1.0f,
            2.0f,
            4.0f,
            8.0f,
            16.0f,
            100.0f
        };

        const float NumRaysPerAxisPerUnitDistance = 2.0f;
        const int MaxNumRaysPerAxis = 11;

        int numRaysHit = 0;

        for (int r = 1; r < ranges.Length; ++r)
        {
            float minDistance = ranges[r - 1];
            float maxDistance = ranges[r];
            int numRaysPerAxis = Mathf.Min(MaxNumRaysPerAxis, Mathf.RoundToInt(Mathf.Max(1.0f, NumRaysPerAxisPerUnitDistance * maxDistance)));

            ProbeDistanceRange(cam, minDistance, maxDistance, numRaysPerAxis, ref numRaysHit);
            if (numRaysHit >= MaxNumRaysPerAxis)
                break;
        }

        CreateWavePulse();
    }

    private void ProbeDistanceRange(Camera cam, float minDistance, float maxDistance, int numRaysPerAxis, ref int numRaysHit)
    {
        var rotation = Quaternion.LookRotation(camera.transform.forward);
        
        var indices = Enumerable
            .Range(0, numRaysPerAxis)
            .SelectMany(x => Enumerable.Range(0, numRaysPerAxis).Select(y => (x, y)))
            .Shuffle();

        float step = numRaysPerAxis <= 1 ? 1.0f : 1.0f / (numRaysPerAxis - 1.0f);
        float halfStep = step * 0.5f;
        foreach ((int indexX, int indexY) in indices)
        {
            var normalizedPos = numRaysPerAxis > 1 ? 
                new Vector3(indexX * step, indexY * step) :
                new Vector3(0.5f, 0.5f);

            // Randomize the ray direction a bit
            normalizedPos.x = Mathf.Clamp01(normalizedPos.x + Random.Range(-halfStep, halfStep));  
            normalizedPos.y = Mathf.Clamp01(normalizedPos.y + Random.Range(-halfStep, halfStep));  
            
            Ray ray = new Ray(cam.transform.position, rotation * GetRayDirection(normalizedPos));
            
            Debug.DrawRay(ray.origin + ray.direction * minDistance, ray.direction * maxDistance, Color.white, 10.0f, true);
            if (Probe(ray, true, minDistance, maxDistance, 2.0f * Mathf.Max(wavePulseAngleHorizontal, wavePulseAngleVertical) / numRaysPerAxis))
                if (++numRaysHit >= maxNumWavespheresPerPulse)
                    return;
        }
    }

    private void CreateWavePulse()
    {
        Assert.IsNotNull(wavePulsePrefab);
        
        GameObject pulse = Instantiate(wavePulsePrefab, transform.position, Quaternion.identity);
        
        Transform tf = pulse.transform;
        tf.localScale = Vector3.zero;
        tf.DOScale(wavePulseMaxRange * 2.0f, wavePulseMaxRange / wavePulseSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(pulse));
    }
    
    private bool Probe(Ray ray, bool spawnFlyingSphere = false, float minDistance = 0, float maxDistance = 100, float dotConeAngle = 10.0f)
    {
        LayerMask layerMask = DotsManager.instance.GetDotsSurfaceLayerMask();
        RaycastHit hit;
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        if (!didHit)
            return false;

        // Use this instead of ray.origin because ray.origin is on not at the camera's position, but on its near plane.
        Vector3 originPosition = transform.position;
        float hitDistance = Vector3.Distance(hit.point, originPosition);
        if (hitDistance < minDistance || hitDistance > maxDistance)
            return false;

        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = ray,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRayDirection = maxDotDistanceFromSurfacePointAlongOriginalRayDirection
        };

        if (spawnFlyingSphere)
        {
            Assert.IsNotNull(flyingSpherePrefab);
            
            this.Delay(hitDistance / wavePulseSpeed, () =>
            {
                FlyingSphere flyingSphere = Instantiate(flyingSpherePrefab, hit.point, Quaternion.identity);
                flyingSphere.SetTarget(originPosition);
                flyingSphere.highlightLocation = highlightLocation;
            });
        }
        else 
        {
            // TODO: For testing purposes move this out of this block to paint the dots when space is hit.
            DotsManager.instance.Highlight(highlightLocation);
        }

        return true;
    }
    
    private Vector3 GetRayDirection(Vector3 normalizedPos)
    {
        float angleX = Mathf.Deg2Rad * 0.5f * Mathf.Lerp(-wavePulseAngleHorizontal, wavePulseAngleHorizontal, normalizedPos.x);
        float angleY = Mathf.Deg2Rad * 0.5f * Mathf.Lerp(-wavePulseAngleVertical  , wavePulseAngleVertical  , normalizedPos.y);

        float cos = Mathf.Cos(angleX);
        Vector3 direction = new Vector3(
            Mathf.Sin(angleX),
            Mathf.Sin(angleY) * cos,
            cos
        );
        return direction;
    }
}
