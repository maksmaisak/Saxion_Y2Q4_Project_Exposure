using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.ParticleSystemJobs;

/// Animates the dots created from one wavesphere
public class DotsAnimator : MonoBehaviour
{
    public bool isBusy { get; private set; }

    [SerializeField] float duration = 1.0f;
    [SerializeField] new ParticleSystem particleSystem;

    [SerializeField] List<Vector3> positionsBuffer = new List<Vector3>(DotsManager.MaxNumDotsPerHighlight);
    [SerializeField] NativeArray<Vector4> deltas;

    void Awake()
    {
        particleSystem = particleSystem ? particleSystem : GetComponent<ParticleSystem>();
        Assert.IsNotNull(particleSystem);
        
        deltas = new NativeArray<Vector4>(DotsManager.MaxNumDotsPerHighlight, Allocator.Persistent);
    }
    
    private void OnDestroy()
    {
        if (deltas.IsCreated)
            deltas.Dispose();
    }

    struct MoveParticlesJob : IParticleSystemJob
    {
        [ReadOnly] public Vector3 origin;
        [ReadOnly] public NativeArray<Vector4> deltas;

        public float dt;
        private float t;
        
        public void ProcessParticleSystem(ParticleSystemJobData jobData)
        {
            t += dt;
            
            var posX = jobData.positions.x;
            var posY = jobData.positions.y;
            var posZ = jobData.positions.z;

            int numParticles = jobData.count;
            for (int i = 0; i < numParticles; ++i)
            {
                Vector4 delta = deltas[i];
                float tClamped = Mathf.Clamp01(t * delta.w);
                posX[i] = origin.x + delta.x * tClamped;
                posY[i] = origin.y + delta.y * tClamped;
                posZ[i] = origin.z + delta.z * tClamped;
            }
        }
    }

    public void AnimateDots(IReadOnlyList<Vector3> positions, Vector3 origin, Action<DotsAnimator, IList<Vector3>> onDoneCallback)
    {
        int numDots = positions.Count;
        if (numDots > DotsManager.MaxNumDotsPerHighlight)
            Debug.Log("");
        
        Assert.IsTrue(numDots <= DotsManager.MaxNumDotsPerHighlight, $"{positions.Count} > {DotsManager.MaxNumDotsPerHighlight}");
        Assert.AreEqual(positionsBuffer.Count, 0);
        positionsBuffer.AddRange(positions);
        
        isBusy = true;
        for (int i = 0; i < numDots; ++i)
            deltas[i] = positionsBuffer[i] - origin;

        var distances = deltas.Take(positions.Count).Select(d => Mathf.Max(d.magnitude, float.Epsilon)).ToArray();
        float longestDistance = distances.Max();
        var multipliers = distances.Select(distance => longestDistance / distance).ToArray();
        for (int i = 0; i < numDots; ++i)
        {
            Vector3 vector = deltas[i];
            deltas[i] = new Vector4(vector.x, vector.y, vector.z, multipliers[i]);
        }
        
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
}