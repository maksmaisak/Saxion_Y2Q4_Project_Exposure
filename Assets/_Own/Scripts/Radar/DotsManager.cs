using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

/// A globally-accessible service for managing dot-based object highlighting.
[RequireComponent(typeof(ParticleSystem))]
public class DotsManager : Singleton<DotsManager>
{
   [Tooltip("Dots can only appear on surfaces with these layers.")] 
   [SerializeField] LayerMask dotsSurfaceLayerMask = Physics.DefaultRaycastLayers;
   [SerializeField] float maxDotSpawnDistance = 200.0f;
   
   public DotsRegistry registry { get; private set; }

   private ParticleSystem dotsParticleSystem;

   void Start()
   {
      Physics.queriesHitTriggers = false;
      dotsParticleSystem = GetComponent<ParticleSystem>();
      
      registry = new DotsRegistry();
   }

   void Update()
   {
      Assert.AreEqual(dotsParticleSystem.particleCount, registry.totalNumDots);
   }

   public LayerMask GetDotsSurfaceLayerMask() => dotsSurfaceLayerMask;

   public void FadeOutDots(float duration = 2.0f)
   {
      // TODO: preallocate this and keep it bundled with its particle system. Big performance hit allocating this.
      var particles = new ParticleSystem.Particle[dotsParticleSystem.main.maxParticles];
      int numAliveParticles = dotsParticleSystem.GetParticles(particles);

      for (int i = 0; i < numAliveParticles; ++i)
         particles[i].remainingLifetime = particles[i].startLifetime = Random.Range(0.0f, duration);

      dotsParticleSystem.SetParticles(particles, numAliveParticles);
        
      dotsParticleSystem.Play();
   }

   public void Highlight(RadarHighlightLocation location) => Highlight(location, dotsSurfaceLayerMask);

   public void Highlight(RadarHighlightLocation location, LayerMask layerMask)
   {
      //Debug.DrawLine(location.originalRay.origin, location.pointOnSurface, Color.green, 20.0f);
      
      Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, location.originalRay.direction);
      float distanceFromCamera = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
      float displacementRadius = distanceFromCamera * Mathf.Tan(Mathf.Deg2Rad * location.dotEmissionConeAngle);
      
      const int MaxNumDots = 400;
      var results  = new NativeArray<RaycastHit>    (MaxNumDots, Allocator.TempJob);
      var commands = new NativeArray<RaycastCommand>(MaxNumDots, Allocator.TempJob);

      for (int i = 0; i < MaxNumDots; ++i)
      {
         Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
         commands[i] = new RaycastCommand(location.originalRay.origin, target - location.originalRay.origin, maxDotSpawnDistance, layerMask);
      }

      JobHandle jobHandle = RaycastCommand.ScheduleBatch(commands, results, 1, default(JobHandle));
      jobHandle.Complete();

      var positions = new List<Vector3>(MaxNumDots);
      for (int i = 0; i < MaxNumDots; ++i)
      {
         RaycastHit dotHit = results[i];
         if (!dotHit.collider) // This only works as long as maxHits is one.
            continue;
         
         if (Vector3.Dot(dotHit.point - location.pointOnSurface, location.originalRay.direction) > location.maxDotDistanceFromSurfacePointAlongOriginalRayDirection)
            continue;

         //Debug.DrawLine(commands[i].from, dotHit.point, Color.cyan * 0.25f, 20.0f);

         // TODO Doing a coroutine ends up being slow, find a way to emit all at the same time and have them fadein.
         // Color over lifetime doesn't work because the particles have infinite lifetime. Store the time of emission in custom particle data?
         positions.Add(dotHit.point);
         //AddDot(dotHit.point);
      }
      
      results.Dispose();
      commands.Dispose();
      
      AddDotsImmediately(positions);

      //StartCoroutine(AddDotsOverTime(positions));
   }
   
   public void DrawDebugInfoInEditor()
   {
      registry?.DrawDebugInfoInEditor();
   }
   
   private IEnumerator AddDotsOverTime(IList<Vector3> positions, float totalTime = 0.5f)
   {
      float numDotsPerFrame = positions.Count * Time.deltaTime / totalTime;
      float timeBetweenDots = Time.deltaTime / numDotsPerFrame;

      float numDotsLeftThisFrame = numDotsPerFrame;
      for (int i = positions.Count - 1; i >= 0; --i)
      {
         AddDot(positions[i]);
         numDotsLeftThisFrame -= 1.0f;
         if (numDotsLeftThisFrame >= 1.0f)
            continue;

         yield return numDotsPerFrame < 1.0f ? new WaitForSeconds(timeBetweenDots) : null;
         numDotsLeftThisFrame += numDotsPerFrame;
      }
   }

   private void AddDotsImmediately(IList<Vector3> positions)
   {
      int numParticlesAdded = positions.Count;
      int oldNumParticles   = dotsParticleSystem.particleCount;
      
      // Emit the particles
      dotsParticleSystem.Emit(numParticlesAdded);

      // Read out into a buffer, set positions, Write back in
      var particleBuffer = new ParticleSystem.Particle[positions.Count];
      dotsParticleSystem.GetParticles(particleBuffer, numParticlesAdded, oldNumParticles);
      for (int i = 0; i < numParticlesAdded; ++i)
      {
         registry.RegisterDot(positions[i]);
         particleBuffer[i].position = positions[i];
      }
      dotsParticleSystem.SetParticles(particleBuffer, numParticlesAdded, oldNumParticles);
      
      // To make it recalculate the bounds used for culling
      dotsParticleSystem.Simulate(0.01f, false, false, false);
   }
    
   private void AddDot(Vector3 position)
   {
      registry.RegisterDot(position);

      dotsParticleSystem = dotsParticleSystem ? dotsParticleSystem : GetComponent<ParticleSystem>();
      
      var emitParams = new ParticleSystem.EmitParams {position = position};
      emitParams.ResetStartColor();
      dotsParticleSystem.Emit(emitParams, 1);

      // To make it recalculate the bounds used for culling
      dotsParticleSystem.Simulate(0.01f, false, false, false);
   }
}