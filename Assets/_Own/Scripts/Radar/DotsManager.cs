using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// A globally-accessible service for managing dot-based object highlighting.
[RequireComponent(typeof(ParticleSystem))]
public class DotsManager : Singleton<DotsManager>
{
   [Tooltip("Dots can only appear on surfaces with these layers.")] 
   [SerializeField] LayerMask dotsSurfaceLayerMask = Physics.DefaultRaycastLayers;
   [SerializeField] float maxDotSpawnDistance = 200.0f;
   
   private ParticleSystem dotsParticleSystem;

   void Start()
   {
      dotsParticleSystem = GetComponent<ParticleSystem>();
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
      Debug.DrawLine(location.originalRay.origin, location.pointOnSurface, Color.green, 20.0f);
      
      Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, location.originalRay.direction);
      float distanceFromCamera = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
      float displacementRadius = distanceFromCamera * Mathf.Tan(Mathf.Deg2Rad * location.dotEmissionConeAngle);
      
      const int MaxNumDots = 400;
      var positions = new List<Vector3>(MaxNumDots);
      for (int i = 0; i < MaxNumDots; ++i)
      {
         Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
         Ray ray = new Ray(location.originalRay.origin, target - location.originalRay.origin);
         
         //Debug.DrawRay(ray.origin, ray.direction, Color.white * 0.5f, 20.0f);

         bool didHitDot = Physics.Raycast(ray, out RaycastHit dotHit, maxDotSpawnDistance, layerMask, QueryTriggerInteraction.Ignore);
         if (!didHitDot)
            continue;

         if (Vector3.Dot(dotHit.point - location.pointOnSurface, location.originalRay.direction) > location.maxDotDistanceFromSurfacePointAlongOriginalRayDirection)
            continue;

         Debug.DrawLine(ray.origin, dotHit.point, Color.cyan * 0.25f, 20.0f);

         // TODO Doing a coroutine ends up being slow, find a way to emit all at the same time and have them fadein.
         // Color over lifetime doesn't work because the particles have infinite lifetime. Store the time of emission in custom particle data?
         positions.Add(dotHit.point);
         //AddDot(dotHit.point);
      }

      StartCoroutine(AddDotsOverTime(positions));
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
    
   private void AddDot(Vector3 position)
   {
      dotsParticleSystem = dotsParticleSystem ? dotsParticleSystem : GetComponent<ParticleSystem>();
      
      var emitParams = new ParticleSystem.EmitParams
      {
         position = position,
      };
      emitParams.ResetStartColor();
      dotsParticleSystem.Emit(emitParams, 1);

      // To make it recalculate the bounds used for culling
      dotsParticleSystem.Simulate(0.01f, false, false, false);
   }
}