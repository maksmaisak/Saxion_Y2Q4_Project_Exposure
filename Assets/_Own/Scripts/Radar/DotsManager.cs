using System;
using UnityEngine;
using UnityEngine.Serialization;
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
      public float distanceFromSurface;
      public float coneAngle;
      [FormerlySerializedAs("radius")] public float maxDistanceFromSurfacePointAlongOriginalRayDirection;
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
      Debug.DrawRay(location.pointOnSurface, location.dotEmissionDirection, Color.green, 10.0f);
      
      const int maxNumDots = 100;

      float distanceFromCamera = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
      float distanceFromSurface = Mathf.Min(
         location.dotEmissionShape.distanceFromSurface, 
         distanceFromCamera
      );
      Vector3 origin = location.pointOnSurface - location.dotEmissionDirection * distanceFromSurface;
            
      Quaternion rotation = Quaternion.LookRotation(location.dotEmissionDirection);
      //Quaternion rotation = Quaternion.FromToRotation(Vector3.right, location.dotEmissionDirection);

      //float cosAngle = Mathf.Cos(location.dotEmissionShape.coneAngle);
      float displacementRadius = distanceFromCamera * 0.25f * Mathf.Tan(location.dotEmissionShape.coneAngle);

      for (int i = 0; i < maxNumDots; ++i)
      {
         Vector3 origin2 = origin + rotation * (Random.insideUnitCircle * displacementRadius);       
         Ray ray = new Ray(origin2, location.dotEmissionDirection);

         /*
         float x = Random.Range(cosAngle, 1.0f);
         float multiplier = Mathf.Sqrt(1.0f - x * x);
         const float PI2 = Mathf.PI * 2.0f;
         float phi = Random.Range(0.0f, PI2);
         var direction = rotation * new Vector3(x, multiplier * Mathf.Cos(phi), multiplier * Mathf.Sin(phi));
         Ray ray = new Ray(origin, direction);
         */
         
         RaycastHit dotHit;
         bool didHitDot = Physics.Raycast(ray, out dotHit);
         if (!didHitDot)
            continue;

         if (Vector3.Dot(dotHit.point - location.pointOnSurface, location.originalRay.direction) > location.dotEmissionShape.maxDistanceFromSurfacePointAlongOriginalRayDirection)
            continue;
            
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