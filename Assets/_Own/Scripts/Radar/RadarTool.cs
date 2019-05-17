using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class RadarTool : MonoBehaviour
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
    [SerializeField] Transform flyingSphereTarget;
    [SerializeField] float minDistanceBetweenSpawnedWavespheres = 1.0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
            Probe();
    }
    
    public void Probe()
    {
        var dotsManager = DotsManager.instance;
        LayerMask layerMask = dotsManager.GetDotsSurfaceLayerMask();

        var rotation = Quaternion.LookRotation(transform.forward); // transform.rotation;
        Vector3 origin = transform.position;

        const int NumSpherecasts = MaxNumRaysPerAxis * MaxNumRaysPerAxis;
        var results  = new NativeArray<RaycastHit>       (NumSpherecasts, Allocator.TempJob);
        var commands = new NativeArray<SpherecastCommand>(NumSpherecasts, Allocator.TempJob);

        // Populated the commands
        int commandIndex = 0;
        
        const float Step = MaxNumRaysPerAxis <= 1 ? 1.0f : 1.0f / (MaxNumRaysPerAxis - 1.0f);
        const float HalfStep = Step * 0.5f;
        for (int indexX = 0; indexX < MaxNumRaysPerAxis; ++indexX)
        {
            for (int indexY = 0; indexY < MaxNumRaysPerAxis; ++indexY)
            { 
                var normalizedPos = MaxNumRaysPerAxis > 1 ? 
                    new Vector3(indexX * Step, indexY * Step) :
                    new Vector3(0.5f, 0.5f);

                // Randomize the ray direction a bit
                normalizedPos.x = Mathf.Clamp01(normalizedPos.x + Random.Range(-HalfStep, HalfStep));  
                normalizedPos.y = Mathf.Clamp01(normalizedPos.y + Random.Range(-HalfStep, HalfStep));  
        
                Ray ray = new Ray(origin, rotation * GetRayDirection(normalizedPos));
                commands[commandIndex++] = new SpherecastCommand(
                    ray.origin, 
                    sphereCastRadius, 
                    ray.direction,
                    wavePulseMaxRange,
                    layerMask
                );
                
                Debug.DrawRay(ray.origin, ray.direction * wavePulseMaxRange, Color.white, 10.0f, true);
            }
        }

        JobHandle jobHandle = SpherecastCommand.ScheduleBatch(commands, results, 1);
        jobHandle.Complete();

        // Handle the spherecast hits
        int numHits = 0;
        float baseDotConeAngle = Mathf.Max(wavePulseAngleHorizontal, wavePulseAngleVertical) * 0.5f;

        RaycastHit[] hits = results.Where(r => r.collider).ToArray();
        
        if (hits.Length > 0)
        {
            var usedHitIndices = new List<int>();
            Func<ValueTuple<RaycastHit, int>, bool> isUseable = tuple => {
                
                (RaycastHit hit, int index) = tuple;
                
                Vector3 point = hit.point;

                return usedHitIndices
                    .All(i => i == index || Vector3.Distance(hits[i].point, point) > minDistanceBetweenSpawnedWavespheres);
            };
            
            while (usedHitIndices.Count < hits.Length && usedHitIndices.Count < maxNumWavespheresPerPulse)
            {
                int index = hits
                    .Select((h, i) => (h, i))
                    .Where(tuple => !usedHitIndices.Contains(tuple.i) && isUseable(tuple))
                    .DefaultIfEmpty((new RaycastHit(), -1))
                    .ArgMin(tuple => dotsManager.registry.GetNumDotsAround(tuple.Item1.point)).Item2;

                if (index == -1)
                    break;
                
                usedHitIndices.Add(index);
            }
            
            foreach (RaycastHit hit in usedHitIndices.Select(i => hits[i]))
            {
                float dotConeAngle = baseDotConeAngle / Mathf.Max(1.0f, hit.distance / dotConeAngleMultiplier);
                bool didHit = HandleHit(hit, new Ray(origin, hit.point - origin), dotConeAngle);
                if (!didHit)
                    continue;
            
                numHits += 1;
                if (numHits >= maxNumWavespheresPerPulse)
                    break;
            }
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
        flyingSphereTarget = flyingSphereTarget ? flyingSphereTarget : Camera.main.transform;
        Vector3 originPosition = flyingSphereTarget.position;

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
