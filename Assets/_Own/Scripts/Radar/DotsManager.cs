using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

/// A globally-accessible service for managing dot-based object highlighting.
public class DotsManager : Singleton<DotsManager>
{
   [Tooltip("Dots can only appear on surfaces with these layers.")] 
   [SerializeField] LayerMask dotsSurfaceLayerMask = Physics.DefaultRaycastLayers;
   [SerializeField] float maxDotSpawnDistance = 200.0f;
   [SerializeField] ParticleSystem dotsParticleSystemPrefab;

   const int MaxNumDotsPerHighlight = 400;

   public DotsRegistry registry { get; private set; }

   struct LightSectionInfo
   {
      public LightSection section;
      public List<Vector3> positionsBuffer;
      public ParticleSystem dotsParticleSystem;
   }

   private LightSectionInfo[] lightSectionInfos;
   
   private Dictionary<Collider, int> colliderToLightSectionIndex = new Dictionary<Collider, int>();
   
   void Start()
   {
      Physics.queriesHitTriggers = false;
      
      registry = new DotsRegistry();
      
      lightSectionInfos = FindObjectsOfType<LightSection>().Select(s => new LightSectionInfo
      {
         section = s,
         positionsBuffer = new List<Vector3>(MaxNumDotsPerHighlight),
         dotsParticleSystem = AddParticleSystem(s)
      }).ToArray();

      for (int i = 0; i < lightSectionInfos.Length; ++i)
      {
         var colliders = lightSectionInfos[i].section.GetGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Collider>())
            .Distinct();
         
         foreach (Collider col in colliders)
            colliderToLightSectionIndex.Add(col, i);
      }
   }

   void Update()
   {
      Assert.AreEqual(lightSectionInfos.Sum(i => i.dotsParticleSystem.particleCount), registry.totalNumDots);
   }

   public LayerMask GetDotsSurfaceLayerMask() => dotsSurfaceLayerMask;

   public void FadeOutDots(ParticleSystem dotsParticleSystem, float duration = 2.0f)
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

      var results  = new NativeArray<RaycastHit>    (MaxNumDotsPerHighlight, Allocator.TempJob);
      var commands = new NativeArray<RaycastCommand>(MaxNumDotsPerHighlight, Allocator.TempJob);

      for (int i = 0; i < MaxNumDotsPerHighlight; ++i)
      {
         Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
         commands[i] = new RaycastCommand(location.originalRay.origin, target - location.originalRay.origin, maxDotSpawnDistance, layerMask);
      }

      JobHandle jobHandle = RaycastCommand.ScheduleBatch(commands, results, 1);
      jobHandle.Complete();
      
      for (int i = 0; i < MaxNumDotsPerHighlight; ++i)
      {
         RaycastHit dotHit = results[i];
         if (!dotHit.collider) // This only works as long as maxHits is one.
            continue;
         
         Vector3 delta = dotHit.point - location.pointOnSurface;
         float distanceAlongRay = Vector3.Dot(delta, location.originalRay.direction);
         if (distanceAlongRay > location.maxDotDistanceFromSurfacePointAlongOriginalRay)
            continue;

         //Debug.DrawLine(commands[i].from, dotHit.point, Color.cyan * 0.25f, 20.0f);

         // TODO Doing a coroutine ends up being slow, find a way to emit all at the same time and have them fade in.
         // Color over lifetime doesn't work because the particles have infinite lifetime. Store the time of emission in custom particle data?
         
         if (!colliderToLightSectionIndex.TryGetValue(dotHit.collider, out int sectionIndex))
            continue;
            
         lightSectionInfos[sectionIndex].positionsBuffer.Add(dotHit.point);
      }
      
      results.Dispose();
      commands.Dispose();

      for (int i = 0; i < lightSectionInfos.Length; ++i)
      {
         var positionsBuffer = lightSectionInfos[i].positionsBuffer;
         AddDotsImmediately(lightSectionInfos[i].dotsParticleSystem, positionsBuffer);
         positionsBuffer.Clear();
      }

      //StartCoroutine(AddDotsOverTime(positions));
   }
   
   public void DrawDebugInfoInEditor()
   {
      registry?.DrawDebugInfoInEditor();
   }
   
   private ParticleSystem AddParticleSystem(LightSection lightSection)
   {
      Assert.IsNotNull(dotsParticleSystemPrefab);
      return Instantiate(dotsParticleSystemPrefab, lightSection.transform);
   }

   private void AddDotsImmediately(ParticleSystem dotsParticleSystem, IList<Vector3> positions)
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
}