using UnityEngine;

/// Whatever data is necessary to highlight an area with a wavesphere.
public struct RadarHighlightLocation
{
   public Ray originalRay;
   public Vector3 pointOnSurface;
   public Vector3 distributionDirection; // The direction in which to sprinkle the dots.
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
      const int maxNumDots = 100;
            
      Quaternion rotation = Quaternion.LookRotation(location.distributionDirection);
      Vector3 origin = location.pointOnSurface - location.distributionDirection;
      
      for (int i = 0; i < maxNumDots; ++i)
      {
         Vector3 origin2 = origin + rotation * (Random.insideUnitCircle * 0.5f);            
         RaycastHit dotHit;
         bool didHitDot = Physics.Raycast(new Ray(origin2, location.distributionDirection), out dotHit);
         if (!didHitDot)
            continue;

         /*if (Vector3.SqrMagnitude(dotHit.point - location.originalRay.position) > 5.0f * 5.0f)
             continue;*/
            
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