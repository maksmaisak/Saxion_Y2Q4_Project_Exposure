using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

/// Animates the dots created from one wavesphere
public class DotsAnimator : MonoBehaviour
{
    public bool isBusy { get; private set; }
    
    [SerializeField] new ParticleSystem particleSystem;

    private readonly List<Vector3> positionsBuffer = new List<Vector3>(DotsManager.MaxNumDotsPerHighlight);
    private readonly List<Vector3> deltasBuffer    = new List<Vector3>(DotsManager.MaxNumDotsPerHighlight);
    private readonly List<float> distancesBuffer   = new List<float>  (DotsManager.MaxNumDotsPerHighlight);
    private readonly List<float> multipliersBuffer = new List<float>  (DotsManager.MaxNumDotsPerHighlight);
    
    /// xyz: vector from origin to the end
    /// w: delta length relative to the longest delta (if half as long as the longest delta, then this is 0.5)
    private NativeArray<Vector4> deltas;

    void Awake()
    {
        particleSystem = particleSystem ? particleSystem : GetComponent<ParticleSystem>();
        Assert.IsNotNull(particleSystem);
        
        deltas = new NativeArray<Vector4>(DotsManager.MaxNumDotsPerHighlight, Allocator.Persistent);
    }
    
    void OnDestroy()
    {
        if (deltas.IsCreated)
            deltas.Dispose();
    }

    public void AnimateDots(IReadOnlyList<Vector3> positions, Vector3 origin, float duration = 1.0f, Action<DotsAnimator, IList<Vector3>> onDoneCallback = null)
    {
        Assert.IsFalse(isBusy);
        isBusy = true;

        int numDots = positions.Count;
        Assert.IsTrue(numDots <= DotsManager.MaxNumDotsPerHighlight, $"{numDots} > {DotsManager.MaxNumDotsPerHighlight}");
        Assert.AreEqual(positionsBuffer.Count, 0, "DotsAnimator.positionsBuffer is not empty!");
        positionsBuffer.AddRange(positions);
        GenerateDeltasFromPositions(origin, numDots);

        particleSystem.Emit(numDots);
        particleSystem.SetJob(new MoveParticlesJob
        {
            origin = origin,
            deltas = deltas,
            dt = Time.fixedDeltaTime / duration
        });
        
        this.Delay(duration, () =>
        {
            particleSystem.ClearJob();
            particleSystem.Clear();
            isBusy = false;
            
            onDoneCallback?.Invoke(this, positionsBuffer);
            positionsBuffer.Clear();
        });
    }

    private void GenerateDeltasFromPositions(Vector3 origin, int numDots)
    {
        deltasBuffer.Clear();
        deltasBuffer.AddRange(positionsBuffer.Select(p => p - origin));

        distancesBuffer.Clear();
        distancesBuffer.AddRange(deltasBuffer.Select(d => Mathf.Max(d.magnitude, float.Epsilon)));
        float longestDistance = distancesBuffer.Max();

        multipliersBuffer.Clear();
        multipliersBuffer.AddRange(distancesBuffer.Select(distance => longestDistance / distance));

        for (int i = 0; i < numDots; ++i)
        {
            Vector3 delta = deltasBuffer[i];
            deltas[i] = new Vector4(delta.x, delta.y, delta.z, multipliersBuffer[i]);
        }
    }
}