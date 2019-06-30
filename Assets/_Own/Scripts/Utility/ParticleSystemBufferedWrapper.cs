using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemBufferedWrapper
{
    public readonly ParticleSystem system;
    private readonly ParticleSystem.Particle[] buffer;

    public ParticleSystemBufferedWrapper(ParticleSystem system, int bufferSize = 10_000)
    {
        this.system = system;
        this.buffer = new ParticleSystem.Particle[bufferSize];
    }
    
    public void AddParticles(IList<Vector3> positions)
    {
        int numParticlesAdded = positions.Count;
        int oldNumParticles   = system.particleCount;
        
        // Emit the particles
        system.Emit(numParticlesAdded);

        // Read out into a buffer, set positions, write back in
        system.GetParticles(buffer, numParticlesAdded, oldNumParticles);
        for (int i = 0; i < numParticlesAdded; ++i)
            buffer[i].position = positions[i];
        
        system.SetParticles(buffer, numParticlesAdded, oldNumParticles);

        if (!system.isPlaying)
        {
            // To make the system recalculate the bounds used for culling
            system.Simulate(0.01f, false, false, false);
        }
    }
}