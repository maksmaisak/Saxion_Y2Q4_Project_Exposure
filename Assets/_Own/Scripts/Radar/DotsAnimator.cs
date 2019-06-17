using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.ParticleSystemJobs;

/// Animates the dots created from one wavesphere
public class DotsAnimator : MonoBehaviour
{
    public bool isBusy { get; private set; }
    
    [SerializeField] float duration = 1.0f;
    [SerializeField] new ParticleSystem particleSystem;

    private List<Vector3> positionsBuffer = new List<Vector3>(DotsManager.MaxNumDotsPerHighlight);
    private List<Vector3> deltasBuffer    = new List<Vector3>(DotsManager.MaxNumDotsPerHighlight);
    private List<float> distancesBuffer   = new List<float>  (DotsManager.MaxNumDotsPerHighlight);
    private List<float> multipliersBuffer = new List<float>  (DotsManager.MaxNumDotsPerHighlight);
    
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

    struct MoveParticlesJob : IParticleSystemJob
    {
        [ReadOnly] public Vector3 origin;
        [ReadOnly] public NativeArray<Vector4> deltas;

        public float dt;
        private float t;
        
        public void ProcessParticleSystem(ParticleSystemJobData jobData)
        {
            if (t > 1.0f)
                return;
            
            var ease = EaseManager.ToEaseFunction(Ease.InOutQuad);

            t += dt;

            NativeArray<float> posX = jobData.positions.x;
            NativeArray<float> posY = jobData.positions.y;
            NativeArray<float> posZ = jobData.positions.z;
            
            int numParticles = jobData.count;
            for (int i = 0; i < numParticles; ++i)
            {
                Vector4 delta = deltas[i];
                float tClamped = Mathf.Clamp01(t * delta.w);

                float tEased = ease(tClamped, 1.0f, 0.0f, 0.0f);
                //float tEased = tClamped * tClamped;
                
                posX[i] = origin.x + delta.x * tEased;
                posY[i] = origin.y + delta.y * tEased;
                posZ[i] = origin.z + delta.z * tEased;
            }
        }
    }
    
    public void AnimateDots(IReadOnlyList<Vector3> positions, Vector3 origin, Action<DotsAnimator, IList<Vector3>> onDoneCallback)
    {
        Assert.IsFalse(isBusy);
        isBusy = true;

        int numDots = positions.Count;
        Assert.IsTrue(numDots <= DotsManager.MaxNumDotsPerHighlight, $"{numDots} > {DotsManager.MaxNumDotsPerHighlight}");
        
        Assert.AreEqual(positionsBuffer.Count, 0, "DotsAnimator.positionsBuffer is not empty!");
        positionsBuffer.AddRange(positions);
        
        FillDeltas(origin, numDots);

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

    private void FillDeltas(Vector3 origin, int numDots)
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