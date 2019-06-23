using DG.Tweening;
using DG.Tweening.Core.Easing;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.ParticleSystemJobs;

struct MoveParticlesJob : IParticleSystemJob
{
    [ReadOnly] public Vector3 origin;
    [ReadOnly] public NativeArray<Vector4> deltas;
    [ReadOnly] public float dt;
    
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