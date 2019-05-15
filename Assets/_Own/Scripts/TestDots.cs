using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DG.Tweening;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class TestDots : MonoBehaviour
{
    private const int MaxNumRaysPerAxis = 21;
    
    [SerializeField] float sphereCastRadius = 0.2f;
    [SerializeField] float maxDotDistanceFromSurfacePointAlongOriginalRayDirection = 1.0f;
    [SerializeField] [Range(0.05f, 5.0f)] float dotConeAngleMultiplier = 1.0f;
    
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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (Input.GetKeyDown(KeyCode.Space) && camera)
            ProbeFrom(camera);
        
        if (Input.GetKeyDown(KeyCode.P))
            CreateWavePulse();
    }

    private void ProbeFrom(Camera cam)
    {
        Assert.IsNotNull(cam);
        
        LayerMask layerMask = DotsManager.instance.GetDotsSurfaceLayerMask();

        var rotation = Quaternion.LookRotation(camera.transform.forward); // camera.transform.rotation;
        Vector3 origin = camera.transform.position;

        const int NumSpherecasts = MaxNumRaysPerAxis * MaxNumRaysPerAxis;
        var results  = new NativeArray<RaycastHit>       (NumSpherecasts, Allocator.TempJob);
        var commands = new NativeArray<SpherecastCommand>(NumSpherecasts, Allocator.TempJob);

        // Populated the commands
        int commandIndex = 0;
        
        float step = MaxNumRaysPerAxis <= 1 ? 1.0f : 1.0f / (MaxNumRaysPerAxis - 1.0f);
        float halfStep = step * 0.5f;
        for (int indexX = 0; indexX < MaxNumRaysPerAxis; ++indexX)
        {
            for (int indexY = 0; indexY < MaxNumRaysPerAxis; ++indexY)
            { 
                var normalizedPos = MaxNumRaysPerAxis > 1 ? 
                    new Vector3(indexX * step, indexY * step) :
                    new Vector3(0.5f, 0.5f);

                // Randomize the ray direction a bit
                normalizedPos.x = Mathf.Clamp01(normalizedPos.x + Random.Range(-halfStep, halfStep));  
                normalizedPos.y = Mathf.Clamp01(normalizedPos.y + Random.Range(-halfStep, halfStep));  
        
                Ray ray = new Ray(cam.transform.position, rotation * GetRayDirection(normalizedPos));
                commands[commandIndex++] = new SpherecastCommand(
                    ray.origin, 
                    sphereCastRadius, 
                    ray.direction,
                    wavePulseMaxRange,
                    layerMask
                );
                
                //Debug.DrawRay(ray.origin, ray.direction * sphereCastRange, Color.white, 10.0f, true);
            }
        }

        JobHandle jobHandle = SpherecastCommand.ScheduleBatch(commands, results, 1);
        jobHandle.Complete();

        // Handle the spherecast hits
        int numHits = 0;
        float baseDotConeAngle = Mathf.Max(wavePulseAngleHorizontal, wavePulseAngleVertical) * 0.5f;
        foreach (RaycastHit hit in results
            .Where(r => r.collider)
            .Shuffle())
            //.OrderBy(r => r.distance))
        {
            float dotConeAngle = baseDotConeAngle / Mathf.Max(1.0f, hit.distance / dotConeAngleMultiplier);
            bool didHit = HandleHit(hit, new Ray(origin, hit.point - origin), dotConeAngle);
            if (!didHit) 
                continue;
            
            numHits += 1;
            if (numHits >= maxNumWavespheresPerPulse)
                break;
        }

        results.Dispose();
        commands.Dispose();

        CreateWavePulse();
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

    private bool HandleHit(RaycastHit hit, Ray originalRay, float dotConeAngle = 10.0f)
    {
        Vector3 originPosition = transform.position;

        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = originalRay,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRayDirection = maxDotDistanceFromSurfacePointAlongOriginalRayDirection
        };
        
        Assert.IsNotNull(flyingSpherePrefab);

        float hitDistance = Vector3.Distance(hit.point, originPosition);
        this.Delay(hitDistance / wavePulseSpeed, () =>
        {
            FlyingSphere flyingSphere = Instantiate(flyingSpherePrefab, hit.point, Quaternion.identity);
            flyingSphere.SetTarget(originPosition);
            flyingSphere.highlightLocation = highlightLocation;
        });

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
