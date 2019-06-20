using System;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.ParticleSystemJobs;

struct FadeOutParticlesJob : IParticleSystemJob
{
    [ReadOnly] public float dt;
    [ReadOnly] public NativeArray<float> multipliers;

    private float t;

    public void ProcessParticleSystem(ParticleSystemJobData jobData)
    {
        if (t >= 1.0f)
            return;

        t += dt;
        
        var ease = EaseManager.ToEaseFunction(Ease.InQuad);
        NativeArray<Color32> colors = jobData.startColors;
        
        int numParticles = jobData.count;
        for (int i = 0; i < numParticles; ++i)
        {
            float tClamped = Mathf.Clamp01(t * multipliers[i]);
            float tEased = ease(tClamped, 1.0f, 0.0f, 0.0f);
            
            Color32 color = colors[i];
            color.a = (byte)(byte.MaxValue * (1.0f - tEased));
            colors[i] = color;
        }
    }
}