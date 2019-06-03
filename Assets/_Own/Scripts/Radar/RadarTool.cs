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
using VRTK;
using Random = UnityEngine.Random;

public class RadarTool : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    private const int MaxNumRaysPerAxis = 21;

    [Header("Wave pulse settings")]
    [SerializeField] GameObject wavePulsePrefab;
    [SerializeField] [Range(0.0f, 360.0f)] float wavePulseAngleHorizontal = 90.0f;
    [SerializeField] [Range(0.0f, 360.0f)] float wavePulseAngleVertical   = 90.0f;
    [SerializeField] float wavePulseSpeed    = 10.0f;
    [SerializeField] float wavePulseMaxRange = 20.0f;
    [SerializeField] float sphereCastRadius = 0.2f;
    
    [Header("Wavesphere settings")]
    [SerializeField] [Range(0.2f, 5.0f)]  float minDistanceBetweenSpawnedWavespheres = 2.0f;
    [SerializeField] [Range(0.2f, 10.0f)] float maxNumWavespheresPerSecond = 2.0f;
    [SerializeField] float wavesphereSpeedMin = 2.5f;
    [SerializeField] float wavesphereSpeedMax = 4.5f;
    [FormerlySerializedAs("flyingSpherePrefab")] [SerializeField] FlyingSphere wavespherePrefab;
    [FormerlySerializedAs("flyingSphereTarget")] [SerializeField] Transform    wavesphereTarget;
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

    protected override void Awake()
    {
        base.Awake();
        
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
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

    public void On(OnRevealEvent message)
    {
        StopAllCoroutines();
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

    private struct CandidateLocation
    {
        public int hitIndex;
        public LightSection lightSection;
        public Vector3 point;
        public float speed;
        public float timeToArrive;
        public ulong numDots;
    }

    private void HandleSpherecastResults()
    {
        // The candidates are sorted into bands with similar distance.
        // Candidates in the same band preserve the initial order.
        const float DistanceBandWidth = 2.0f;
        const ulong NumDotsBandWidth = 20;

        var dotsManager = DotsManager.instance;
        DotsRegistry dotsRegistry = dotsManager.registry;
        ulong GetRoundedNumDotsAround(Vector3 point) => dotsRegistry.GetNumDotsAround(point) / NumDotsBandWidth;

        CandidateLocation[] candidateLocations = hits
            .Select((hit, i) => (hit, i))
            .Where(tuple => tuple.hit.collider)
            .Select(tuple => (tuple.hit, tuple.i, lightSection: dotsManager.GetSection(tuple.hit.collider)))
            .Where(tuple => tuple.lightSection && !tuple.lightSection.isRevealed)
            .Select(tuple =>
            {
                float speed = Random.Range(wavesphereSpeedMin, wavesphereSpeedMax);
                return new CandidateLocation
                {
                    hitIndex = tuple.i,
                    lightSection = tuple.lightSection,
                    point = tuple.hit.point,
                    speed = speed,
                    timeToArrive = tuple.hit.distance / speed,
                    numDots = GetRoundedNumDotsAround(tuple.hit.point),
                };
            })
            .OrderBy(l => Mathf.RoundToInt(hits[l.hitIndex].distance / DistanceBandWidth))
            .ToArray();

        if (candidateLocations.Length <= 0) 
            return;

        var usedCandidateIndices = new List<int>();
        
        float minTimeDistanceBetweenWavespheres = 1.0f / maxNumWavespheresPerSecond;
        float sqrMinDistance = minDistanceBetweenSpawnedWavespheres * minDistanceBetweenSpawnedWavespheres;
        bool IsTooCloseToAlreadyUsedLocations(int candidateIndex)
        {
            Vector3 point = candidateLocations[candidateIndex].point;
            float timeOfArrival = candidateLocations[candidateIndex].timeToArrive;
            return 
                usedCandidateIndices.Any(i => Vector3.SqrMagnitude(candidateLocations[i].point - point) < sqrMinDistance) || 
                usedCandidateIndices.Any(i => Mathf.Abs(candidateLocations[i].timeToArrive - timeOfArrival) < minTimeDistanceBetweenWavespheres);
        }
        
        while (usedCandidateIndices.Count < candidateLocations.Length)
        {
            int candidateIndex = candidateLocations
                .Select((l, i) => i)
                .Where(i => !usedCandidateIndices.Contains(i) && !IsTooCloseToAlreadyUsedLocations(i))
                .DefaultIfEmpty(-1)
                .ArgMin(i => i == -1 ? ulong.MaxValue : candidateLocations[i].numDots);

            if (candidateIndex == -1)
                break;

            usedCandidateIndices.Add(candidateIndex);
        }

        foreach (int candidateIndex in usedCandidateIndices)
        {
            ref var location = ref candidateLocations[candidateIndex];
            int i = location.hitIndex;
            SpawnWavesphere(hits[i], new Ray(commands[i].origin, commands[i].direction), location.speed, location.lightSection);
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

    private void SpawnWavesphere(RaycastHit hit, Ray originalRay, float speed, LightSection lightSection)
    {
        float dotConeAngle = baseDotConeAngle / Mathf.Pow(dotConeAngleFalloff * hit.distance + 1.0f, dotConeAngleFalloffPower);

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
            if (lightSection && lightSection.isRevealed)
                return;
            
            if (highlightWithoutWavespheres)
            {
                DotsManager.instance.Highlight(highlightLocation, originalRay.origin);
                return;
            }
            
            wavesphereTarget = wavesphereTarget ? wavesphereTarget : Camera.main.transform;
            
            FlyingSphere flyingSphere = Instantiate(wavespherePrefab, hit.point, Quaternion.identity);
            flyingSphere.Initialize(wavesphereTarget.position, speed, lightSection);
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
