using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

/// Represents a radar pulse. Uses a threaded job to generate RadarHighlightLocation|s
/// Call DoPulse to start the job, then call Complete to get complete it and get the RadarHighlightLocation|s
public class RadarPulse : IDisposable
{
    private const int MaxNumRaysPerAxis = 21;
    private struct CandidateLocation
    {
        public int hitIndex;
        public LightSection lightSection;
        public Vector3 point;
        public float speed;
        public float timeToArrive;
        public ulong numDots;
    }

    private readonly (int indexX, int indexY)[] rayIndices;
    private NativeArray<SpherecastCommand> commands;
    private NativeArray<RaycastHit>        hits;
    private JobHandle? currentJobHandle;

    public bool drawSpherecastRays { get; set; }
    public PulseSettings pulseSettings { get; set; }
    public bool isWorkingJob => currentJobHandle.HasValue;
    public bool isJobCompleted => currentJobHandle.HasValue && currentJobHandle.Value.IsCompleted;

    public RadarPulse()
    {
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

    public void Dispose()
    {
        if (commands.IsCreated)
            commands.Dispose();

        if (hits.IsCreated)
            hits.Dispose();
    }

    public void DoPulse(Vector3 origin, Quaternion rotation, LayerMask layerMask)
    {
        Assert.IsFalse(currentJobHandle.HasValue);
        
        GenerateSpherecastCommands(origin, rotation, layerMask);
        currentJobHandle = SpherecastCommand.ScheduleBatch(commands, hits, 1);
    }

    public IEnumerable<RadarHighlightLocation> Complete()
    {
        Assert.IsTrue(currentJobHandle.HasValue);
        
        currentJobHandle.Value.Complete();
        currentJobHandle = null;

        CandidateLocation[] candidateLocations = GetCandidateLocationsFromSpherecastResults();
        if (candidateLocations.Length <= 0) 
            return Enumerable.Empty<RadarHighlightLocation>();

        return GetHighlightLocations(candidateLocations);
    }
    
    private void GenerateSpherecastCommands(Vector3 origin, Quaternion rotation, LayerMask layerMask)
    {
        for (int i = 0; i < rayIndices.Length; ++i)
        {
            Vector2 normalizedPos = GetNormalizedPosition(rayIndices[i].indexX, rayIndices[i].indexY);
            Vector3 direction = rotation * GetRayDirection(normalizedPos, pulseSettings.wavePulseAngleHorizontal, pulseSettings.wavePulseAngleVertical);
            commands[i] = new SpherecastCommand(
                origin,
                pulseSettings.sphereCastRadius,
                direction,
                pulseSettings.wavePulseMaxRange,
                layerMask
            );

            if (drawSpherecastRays)
                Debug.DrawRay(origin, direction * pulseSettings.wavePulseMaxRange, Color.white * 0.1f, 10.0f, true);
        }
    }

    private Vector2 GetNormalizedPosition(int indexX, int indexY)
    {
        const float Step = MaxNumRaysPerAxis <= 1 ? 1.0f : 1.0f / (MaxNumRaysPerAxis - 1.0f);
        const float HalfStep = Step * 0.5f;
        
        Vector2 normalizedPos = MaxNumRaysPerAxis > 1 ? 
            new Vector2(indexX * Step, indexY * Step) : 
            new Vector2(0.5f, 0.5f);
        
        // Randomize the ray direction a bit
        normalizedPos.x = Mathf.Clamp01(normalizedPos.x + Random.Range(-HalfStep, HalfStep));
        normalizedPos.y = Mathf.Clamp01(normalizedPos.y + Random.Range(-HalfStep, HalfStep));

        return normalizedPos;
    }

    private Vector3 GetRayDirection(Vector2 normalizedPos, float rangeHorizontal, float rangeVertical)
    {
        float angleX = Mathf.Deg2Rad * 0.5f * Mathf.Lerp(-rangeHorizontal, rangeHorizontal, normalizedPos.x);
        float angleY = Mathf.Deg2Rad * 0.5f * Mathf.Lerp(-rangeVertical  , rangeVertical  , normalizedPos.y);

        float cos = Mathf.Cos(angleX);
        Vector3 direction = new Vector3(
            Mathf.Sin(angleX),
            Mathf.Sin(angleY) * cos,
            cos
        );
        return direction;
    }
    
    private CandidateLocation[] GetCandidateLocationsFromSpherecastResults()
    {
        // The candidates are sorted into bands with similar distance.
        // Candidates in the same band preserve the initial order.
        const float DistanceBandWidth = 2.0f;
        const ulong NumDotsBandWidth = 80;

        var dotsManager = DotsManager.instance;
        DotsRegistry dotsRegistry = dotsManager.registry;
        
        ulong GetRoundedNumDotsAround(Vector3 point) => dotsRegistry.GetNumDotsAround(point) / NumDotsBandWidth;
        int GetRoundedHitDistance(int hitIndex) => Mathf.RoundToInt(hits[hitIndex].distance / DistanceBandWidth);

        return hits
            .Select((hit, i) => (hit, i))
            .Where(tuple => tuple.hit.collider)
            .Select(tuple => (tuple.hit, tuple.i, lightSection: dotsManager.GetSection(tuple.hit.collider)))
            .Where(tuple => tuple.lightSection && !tuple.lightSection.isRevealed)
            .Select(tuple =>
            {
                float speed = Random.Range(pulseSettings.wavesphereSpeedMin, pulseSettings.wavesphereSpeedMax);
                return new CandidateLocation
                {
                    hitIndex = tuple.i,
                    lightSection = tuple.lightSection,
                    point = tuple.hit.point,
                    speed = speed,
                    timeToArrive = tuple.hit.distance / pulseSettings.wavePulseSpeed + tuple.hit.distance / speed,
                    numDots = GetRoundedNumDotsAround(tuple.hit.point),
                };
            })
            .OrderBy(l => GetRoundedHitDistance(l.hitIndex))
            .ToArray();
    }

    private IEnumerable<RadarHighlightLocation> GetHighlightLocations(CandidateLocation[] candidateLocations)
    {
        var usedCandidateIndices = new List<int>();
        
        float minTimeDistanceBetweenWavespheres = 1.0f / pulseSettings.maxNumWavespheresPerSecond.Lerp(AdaptiveDifficulty.instance.difficulty);
        float sqrMinDistance = pulseSettings.minDistanceBetweenSpawnedWavespheres * pulseSettings.minDistanceBetweenSpawnedWavespheres;
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
            if (pulseSettings.maxNumWavespheresPerPulse >= 0 && usedCandidateIndices.Count >= pulseSettings.maxNumWavespheresPerPulse)
                break;

            int candidateIndex = candidateLocations
                .Select((l, i) => i)
                .Where(i => !usedCandidateIndices.Contains(i) && !IsTooCloseToAlreadyUsedLocations(i))
                .DefaultIfEmpty(-1)
                .ArgMin(i => i == -1 ? ulong.MaxValue : candidateLocations[i].numDots);

            if (candidateIndex == -1)
                break;

            usedCandidateIndices.Add(candidateIndex);
        }
        
        return usedCandidateIndices.Select(locationIndex => ToRadarHighlightLocation(candidateLocations[locationIndex]));
    }

    private RadarHighlightLocation ToRadarHighlightLocation(CandidateLocation candidateLocation)
    {
        float hitDistance = hits[candidateLocation.hitIndex].distance;
        float dotConeAngle = pulseSettings.baseDotConeAngle / Mathf.Pow(pulseSettings.dotConeAngleFalloff * hitDistance + 1.0f, pulseSettings.dotConeAngleFalloffPower);
        
        Ray originalRay = new Ray(
            commands[candidateLocation.hitIndex].origin,
            commands[candidateLocation.hitIndex].direction
        );
        
        return new RadarHighlightLocation {
            originalRay = originalRay,
            pointOnSurface = candidateLocation.point,
            distanceFromOrigin = hitDistance,
            dotEmissionConeAngle = dotConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRay = pulseSettings.maxDotDistanceFromSurfacePointAlongOriginalRay,
            wavesphereSpeed = candidateLocation.speed,
            lightSection = candidateLocation.lightSection
        };
    }
}