using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// Whatever data is necessary to highlight an area with a wavesphere.
public struct RadarHighlightLocation
{
   public Ray originalRay;
   public Vector3 pointOnSurface;
   public Vector3 dotEmissionDirection;

   /// Specifies a cone starting a certain distance from the pointOnSurface. 
   /// Dots will be sprinkled from the tip of the cone along its inside.
   [Serializable]
   public struct DotEmissionShape
   {
      public float coneAngle;
      public float maxDistanceFromSurfacePointAlongOriginalRayDirection;
   }

   public DotEmissionShape dotEmissionShape;
}

[RequireComponent(typeof(ParticleSystem))]
public class DotsManager : Singleton<DotsManager>
{
   private ParticleSystem dotsParticleSystem;

   void Start()
   {
      dotsParticleSystem = GetComponent<ParticleSystem>();
   }
   
   public void Highlight(RadarHighlightLocation location)
   {
      Debug.DrawLine(location.originalRay.origin, location.pointOnSurface, Color.green, 20.0f);
      
      const int maxNumDots = 100;
      
      Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, location.dotEmissionDirection);
      float distanceFromCamera = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
      float displacementRadius = distanceFromCamera * 0.25f * Mathf.Tan(location.dotEmissionShape.coneAngle);

      for (int i = 0; i < maxNumDots; ++i)
      {
         Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
         Ray ray = new Ray(location.originalRay.origin, target - location.originalRay.origin);
         
         RaycastHit dotHit;
         bool didHitDot = Physics.Raycast(ray, out dotHit);
         if (!didHitDot)
            continue;

         if (Vector3.Dot(dotHit.point - location.pointOnSurface, location.originalRay.direction) > location.dotEmissionShape.maxDistanceFromSurfacePointAlongOriginalRayDirection)
            continue;

         Debug.DrawLine(ray.origin, dotHit.point, Color.cyan * 0.5f, 20.0f);

         AddDot(dotHit.point);
      }
   }
    
   private void AddDot(Vector3 position)
   {
      dotsParticleSystem = dotsParticleSystem ? dotsParticleSystem : GetComponent<ParticleSystem>();
      
      var emitParams = new ParticleSystem.EmitParams {position = position};
      dotsParticleSystem.Emit(emitParams, 1);
        
      // To make it recalculate the bounds used for culling
      dotsParticleSystem.Simulate(0.01f, false, false, false);
   }
}