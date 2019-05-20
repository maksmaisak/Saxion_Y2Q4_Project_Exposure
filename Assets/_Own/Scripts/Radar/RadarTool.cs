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

    [Header("Wave pulse settings")]
    [SerializeField] GameObject wavePulsePrefab;
    [SerializeField] [Range(0.0f, 360.0f)] float wavePulseAngleHorizontal = 90.0f;
    [SerializeField] [Range(0.0f, 360.0f)] float wavePulseAngleVertical   = 90.0f;
    [SerializeField] float wavePulseSpeed    = 10.0f;
    [SerializeField] float wavePulseMaxRange = 20.0f;
    [SerializeField] float sphereCastRadius = 0.2f;
    [SerializeField] int maxNumWavespheresPerPulse = 10;
    
    [Header("Wavesphere settings")]
    [FormerlySerializedAs("flyingSpherePrefab")] [SerializeField] FlyingSphere wavespherePrefab;
    [FormerlySerializedAs("flyingSphereTarget")] [SerializeField] Transform    wavesphereTarget;
    [SerializeField] float minDistanceBetweenSpawnedWavespheres = 2.0f;
    [SerializeField] [Range(0.0f, 360.0f)] float baseDotConeAngle = 20.0f;
    [SerializeField] [Range(0.01f, 1.0f)] float dotConeAngleFalloff = 0.02f;
    [SerializeField] [Range(0.1f , 5.0f)] float dotConeAngleFalloffPower = 1.0f;
    [SerializeField] float maxDotDistanceFromSurfacePointAlongOriginalRay = 1.0f;

    [Header("Debug settings")] 
    [SerializeField] bool highlightWithoutWavespheres = false;
    [SerializeField] bool drawSpherecastRays          = false;

    private new Transform transform;
    
    private (int indexX, int indexY)[] rayIndices;
    private NativeArray<SpherecastCommand> commands;
    private NativeArray<RaycastHit>        hits;
    
    private static readonly int CosHalfVerticalAngle   = Shader.PropertyToID("_CosHalfVerticalAngle");
    private static readonly int CosHalfHorizontalAngle = Shader.PropertyToID("_CosHalfHorizontalAngle");

    void Awake()
    {
        transform = GetComponent<Transform>();
        
        // A list of (indexX, indexY) pairs, ordered so that the ones in the middle are first.
        const int MidIndex = MaxNumRaysPerAxis / 2;
        rayIndices = Enumerable
            .Range(0, MaxNumRaysPerAxis)
            .SelectMany(x => Enumerable.Range(0, MaxNumRaysPerAxis).Select(y => (x, y)))
            .OrderBy(tuple => Mathf.Abs(tuple.x - MidIndex) + Mathf.Abs(tuple.y - MidIndex))
            .ToArray();
        
        const int MaxNumSpherecasts = MaxNumRaysPerAxis * MaxNumRaysPerAxis;
        commands = new NativeArray<SpherecastCommand>(MaxNumSpherecasts, Allocator.Persistent);
        hits     = new NativeArray<RaycastHit>       (MaxNumSpherecasts, Allocator.Persistent);
    }

    void OnDestroy()
    {
        if (commands.IsCreated)
            commands.Dispose();
        
        if (hits.IsCreated)
            hits.Dispose();
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.R))
          //  SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
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
    
    private void GenerateSpherecastCommands(LayerMask layerMask)
    {
        Vector3    origin   = transform.position;
        Quaternion rotation = transform.rotation;
        
        const float Step = MaxNumRaysPerAxis <= 1 ? 1.0f : 1.0f / (MaxNumRaysPerAxis - 1.0f);
        const float HalfStep = Step * 0.5f;
        
        for (int i = 0; i < rayIndices.Length; ++i)
        {
            (int indexX, int indexY) = rayIndices[i];
            
            var normalizedPos = MaxNumRaysPerAxis > 1
                ? new Vector3(indexX * Step, indexY * Step)
                : new Vector3(0.5f, 0.5f);

            // Randomize the ray direction a bit
            normalizedPos.x = Mathf.Clamp01(normalizedPos.x + Random.Range(-HalfStep, HalfStep));
            normalizedPos.y = Mathf.Clamp01(normalizedPos.y + Random.Range(-HalfStep, HalfStep));

            Ray ray = new Ray(origin, rotation * GetRayDirection(normalizedPos));
            commands[i] = new SpherecastCommand(
                ray.origin,
                sphereCastRadius,
                ray.direction,
                wavePulseMaxRange,
                layerMask
            );

            if (drawSpherecastRays)
                Debug.DrawRay(ray.origin, ray.direction * wavePulseMaxRange, Color.white * 0.1f, 10.0f, true);
        }
    }

    private void HandleSpherecastResults()
    {
        // The candidates are sorted into bands with similar distance.
        // Candidates in the same band preserve the initial order.
        const float DistanceBandWidth = 2.0f;

        (RaycastHit hit, int index)[] candidateHits = hits
            .Select((hit, index) => (hit, index))
            .Where(tuple => tuple.hit.collider)
            .OrderBy(tuple => Mathf.RoundToInt(tuple.hit.distance / DistanceBandWidth))
            .ToArray();
        
        if (candidateHits.Length <= 0) 
            return;

        var usedHitIndices = new List<int>(maxNumWavespheresPerPulse);

        bool IsTooCloseToAlreadyUsedLocations(Vector3 point)
        {
            float sqrMinDistance = minDistanceBetweenSpawnedWavespheres * minDistanceBetweenSpawnedWavespheres;
            return usedHitIndices.Any(i => Vector3.SqrMagnitude(hits[i].point - point) < sqrMinDistance);
        }

        DotsRegistry dotsRegistry = DotsManager.instance.registry;
        const ulong NumDotsBandWidth = 20;
        ulong GetRoundedNumDotsAround(Vector3 point) => dotsRegistry.GetNumDotsAround(point) / NumDotsBandWidth;
        
        while (usedHitIndices.Count < candidateHits.Length && usedHitIndices.Count < maxNumWavespheresPerPulse)
        {
            int index = candidateHits
                .Where(tuple => !usedHitIndices.Contains(tuple.index) && !IsTooCloseToAlreadyUsedLocations(tuple.hit.point))
                .DefaultIfEmpty((hit: new RaycastHit(), index: -1))
                .ArgMin(tuple => GetRoundedNumDotsAround(tuple.hit.point)).index;

            if (index == -1)
                break;

            usedHitIndices.Add(index);
        }

        foreach (int i in usedHitIndices)
        {
            float dotConeAngle = baseDotConeAngle / Mathf.Pow(dotConeAngleFalloff * hits[i].distance + 1.0f, dotConeAngleFalloffPower);
            HandleHit(hits[i], new Ray(commands[i].origin, commands[i].direction), dotConeAngle);
        }
    }

    private void CreateWavePulse()
    {
        Assert.IsNotNull(wavePulsePrefab);
        
        GameObject pulse = Instantiate(wavePulsePrefab, transform.position, transform.rotation);
        
        Transform tf = pulse.transform;
        tf.localScale = Vector3.zero;
        tf.DOScale(wavePulseMaxRange * 2.0f, wavePulseMaxRange / wavePulseSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(pulse));
        
        var material = pulse.GetComponent<Renderer>().material;
        material.SetFloat(CosHalfHorizontalAngle, Mathf.Cos(Mathf.Deg2Rad * wavePulseAngleHorizontal * 0.5f));
        material.SetFloat(CosHalfVerticalAngle  , Mathf.Cos(Mathf.Deg2Rad * wavePulseAngleVertical   * 0.5f));
    }

    private void HandleHit(RaycastHit hit, Ray originalRay, float dotConeAngle = 10.0f)
    {
        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = originalRay,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRay = maxDotDistanceFromSurfacePointAlongOriginalRay
        };
        
        Assert.IsNotNull(wavespherePrefab);
        
        this.Delay(hit.distance / wavePulseSpeed, () =>
        {
            if (highlightWithoutWavespheres)
            {
                DotsManager.instance.Highlight(highlightLocation);
                return;
            }
            
            wavesphereTarget = wavesphereTarget ? wavesphereTarget : Camera.main.transform;
            
            FlyingSphere flyingSphere = Instantiate(wavespherePrefab, hit.point, Quaternion.identity);
            flyingSphere.SetTarget(wavesphereTarget.position);
            flyingSphere.highlightLocation = highlightLocation;
        });
    }
    
    private Vector3 GetRayDirection(Vector2 normalizedPos)
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
