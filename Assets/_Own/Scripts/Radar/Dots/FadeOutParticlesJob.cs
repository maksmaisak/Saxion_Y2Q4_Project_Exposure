using System;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.ParticleSystemJobs;

struct FadeOutParticlesJob : IParticleSystemJob
{
    [ReadOnly] public float dt;
    [ReadOnly] public Vector3 origin;
    [ReadOnly] public NativeArray<float> multipliers;

    private float t;

    public void ProcessParticleSystem(ParticleSystemJobData jobData)
    {
        if (t >= 1.0f)
            return;

        t += dt;
        
        var ease = EaseManager.ToEaseFunction(Ease.InOutQuad);
        NativeArray<Color32> colors = jobData.startColors;
        
        NativeArray<float> posX = jobData.positions.x;
        NativeArray<float> posY = jobData.positions.y;
        NativeArray<float> posZ = jobData.positions.z;
        
        int numParticles = jobData.count;
        for (int i = 0; i < numParticles; ++i)
        {
            float sqrDistance = (origin - new Vector3(posX[i], posY[i], posZ[i])).sqrMagnitude;
            float distanceMultiplier = 1.0f + 1.0f / (1.0f + sqrDistance * 0.1f);
            
            float tClamped = Mathf.Clamp01(t * distanceMultiplier * multipliers[i]);
            float tEased = ease(tClamped, 1.0f, 0.0f, 0.0f);
            
            Color32 color = colors[i];
            color.a = (byte)(byte.MaxValue * Mathf.Lerp(1.0f, 0.5f, tEased));
            colors[i] = color;
        }
    }
}