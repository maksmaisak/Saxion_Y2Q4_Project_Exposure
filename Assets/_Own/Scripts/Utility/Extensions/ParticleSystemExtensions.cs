using System.Collections.Generic;
using UnityEngine;

public static class ParticleSystemExtensions
{
    public static void AddParticles(this ParticleSystem particleSystem, IList<Vector3> positions)
    {
        int numParticlesAdded = positions.Count;
        int oldNumParticles   = particleSystem.particleCount;
        
        // Emit the particles
        particleSystem.Emit(numParticlesAdded);

        // Read out into a buffer, set positions, write back in
        // TODO have a wrapper around a ParticleSystem that preallocates the buffer.
        var particleBuffer = new ParticleSystem.Particle[positions.Count];
        particleSystem.GetParticles(particleBuffer, numParticlesAdded, oldNumParticles);
        for (int i = 0; i < numParticlesAdded; ++i)
            particleBuffer[i].position = positions[i];
        
        particleSystem.SetParticles(particleBuffer, numParticlesAdded, oldNumParticles);

        if (!particleSystem.isPlaying)
        {
            // To make the system recalculate the bounds used for culling
            particleSystem.Simulate(0.01f, false, false, false);
        }
    }
}