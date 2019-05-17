using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
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
    [SerializeField] float wavePulseSpeed    = 10.0f;
    [SerializeField] float wavePulseMaxRange = 20.0f;
    [SerializeField] int maxNumWavespheresPerPulse = 10;
    [Space]
    [FormerlySerializedAs("flyingSpherePrefab")] [SerializeField] FlyingSphere wavespherePrefab;
    [FormerlySerializedAs("flyingSphereTarget")] [SerializeField] Transform    wavesphereTarget;
    [SerializeField] float minDistanceBetweenSpawnedWavespheres = 1.0f;

    private new Transform transform;
    
    private NativeArray<SpherecastCommand> commands;
    private NativeArray<RaycastHit>        hits;

    void Awake()
    {
        transform = GetComponent<Transform>();
        
        const int MaxNumSpherecasts = MaxNumRaysPerAxis * MaxNumRaysPerAxis;
        commands = new NativeArray<SpherecastCommand>(MaxNumSpherecasts, Allocator.Persistent);
        hits     = new NativeArray<RaycastHit>       (MaxNumSpherecasts, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        if (commands.IsCreated)
            commands.Dispose();
        
        if (hits.IsCreated)
            hits.Dispose();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        if (Input.GetKeyDown(KeyCode.Space)) 
            Probe();
    }
    
    public void Probe()
    {
        CreateWavePulse();
        
        GenerateSpherecastCommands(DotsManager.instance.GetDotsSurfaceLayerMask());
        SpherecastCommand.ScheduleBatch(commands, hits, 1).Complete();
        HandleSpherecastResults();
    }

    private void HandleSpherecastResults()
    {
        if (hits.Length <= 0) 
            return;
        
        var usedHitIndices = new List<int>();

        bool IsUsable((RaycastHit, int) tuple)
        {
            (RaycastHit hit, int index) = tuple;
            
            Vector3 point = hit.point;
            return usedHitIndices.All(i =>
                i == index || Vector3.Distance(hits[i].point, point) > minDistanceBetweenSpawnedWavespheres
            );
        }

        DotsRegistry dotsRegistry = DotsManager.instance.registry;
        while (usedHitIndices.Count < hits.Length && usedHitIndices.Count < maxNumWavespheresPerPulse)
        {
            int index = hits
                .Select((hit, i) => (hit, i))
                .Where(tuple => tuple.hit.collider && !usedHitIndices.Contains(tuple.i) && IsUsable(tuple))
                .DefaultIfEmpty((new RaycastHit(), -1))
                .ArgMin(tuple => dotsRegistry.GetNumDotsAround(tuple.Item1.point)).Item2;

            if (index == -1)
                break;

            usedHitIndices.Add(index);
        }

        foreach (int i in usedHitIndices)
        {
            float baseDotConeAngle = Mathf.Max(wavePulseAngleHorizontal, wavePulseAngleVertical) * 0.5f;
            float dotConeAngle = baseDotConeAngle / Mathf.Max(1.0f, hits[i].distance / dotConeAngleMultiplier);
            HandleHit(hits[i], new Ray(commands[i].origin, commands[i].direction), dotConeAngle);
        }
    }

    private void GenerateSpherecastCommands(LayerMask layerMask)
    {
        Vector3    origin   = transform.position;
        Quaternion rotation = transform.rotation;
        
        const float Step = MaxNumRaysPerAxis <= 1 ? 1.0f : 1.0f / (MaxNumRaysPerAxis - 1.0f);
        const float HalfStep = Step * 0.5f;
        
        int commandIndex = 0;
        for (int indexX = 0; indexX < MaxNumRaysPerAxis; ++indexX)
        {
            for (int indexY = 0; indexY < MaxNumRaysPerAxis; ++indexY)
            {
                var normalizedPos = MaxNumRaysPerAxis > 1
                    ? new Vector3(indexX * Step, indexY * Step)
                    : new Vector3(0.5f, 0.5f);

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
        wavesphereTarget = wavesphereTarget ? wavesphereTarget : Camera.main.transform;
        Vector3 originPosition = wavesphereTarget.position;

        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = originalRay,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRayDirection = maxDotDistanceFromSurfacePointAlongOriginalRayDirection
        };
        
        Assert.IsNotNull(wavespherePrefab);

        float hitDistance = Vector3.Distance(hit.point, originPosition);
        this.Delay(hitDistance / wavePulseSpeed, () =>
        {
            FlyingSphere flyingSphere = Instantiate(wavespherePrefab, hit.point, Quaternion.identity);
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
