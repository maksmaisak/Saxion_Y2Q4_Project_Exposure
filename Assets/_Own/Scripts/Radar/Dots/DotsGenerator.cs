using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class DotsGenerator : IDisposable
{
    public struct Results
    {
        public NativeArray<RaycastHit> hits;
        public RadarHighlightLocation highlightLocation;
        public Vector3 dotsOrigin;
    }

    private NativeArray<RaycastCommand> commands;
    private NativeArray<RaycastHit>     hits;
    private JobHandle? currentJobHandle;

    private readonly float maxDotSpawnDistance = 100.0f;
    private RadarHighlightLocation currentHighlightLocation;
    private Vector3 currentDotsOrigin;
    
    public bool isWorkingJob   => currentJobHandle.HasValue;
    public bool isJobCompleted => currentJobHandle.HasValue && currentJobHandle.Value.IsCompleted;

    public DotsGenerator(float maxDotSpawnDistance)
    {
        commands = new NativeArray<RaycastCommand>(DotsManager.MaxNumDotsPerHighlight, Allocator.Persistent);
        hits     = new NativeArray<RaycastHit>    (DotsManager.MaxNumDotsPerHighlight, Allocator.Persistent);

        this.maxDotSpawnDistance = maxDotSpawnDistance;
    }
    
    public void Dispose()
    {
        if (commands.IsCreated)
            commands.Dispose();
        
        if (hits.IsCreated)
            hits.Dispose();
    }

    public void Generate(ref RadarHighlightLocation location, Vector3 dotsOrigin, LayerMask layerMask)
    {
        Assert.IsFalse(currentJobHandle.HasValue);

        currentHighlightLocation = location;
        currentDotsOrigin = dotsOrigin;
        
        GenerateRaycastCommands(ref location, layerMask);
        currentJobHandle = RaycastCommand.ScheduleBatch(commands, hits, 1);
    }

    public Results Complete()
    {
        Assert.IsTrue(currentJobHandle.HasValue);

        currentJobHandle.Value.Complete();
        currentJobHandle = null;

        return new Results {
            hits = hits,
            highlightLocation = currentHighlightLocation,
            dotsOrigin = currentDotsOrigin
        };
    }
    
    private void GenerateRaycastCommands(ref RadarHighlightLocation location, LayerMask layerMask)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, location.originalRay.direction);
        float distanceFromOrigin = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
        float displacementRadius = distanceFromOrigin * Mathf.Tan(Mathf.Deg2Rad * location.dotEmissionConeAngle);
        
        for (int i = 0; i < DotsManager.MaxNumDotsPerHighlight; ++i)
        {
            Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
            commands[i] = new RaycastCommand(location.originalRay.origin, target - location.originalRay.origin, maxDotSpawnDistance, layerMask);
        }
    }
}