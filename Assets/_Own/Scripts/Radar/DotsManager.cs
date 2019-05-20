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

   const int MaxNumDotsPerHighlight = 400;

   public DotsRegistry registry { get; private set; }

   private LightSection [] lightSections;
   private List<Vector3>[] positionsBuffers;

   private readonly Dictionary<Collider, int> colliderToLightSectionIndex = new Dictionary<Collider, int>();
   
   void Start()
   {
      Physics.queriesHitTriggers = false;
      
      registry = new DotsRegistry();

      lightSections = FindObjectsOfType<LightSection>().ToArray();
      positionsBuffers = Enumerable
         .Range(0, lightSections.Length)
         .Select(i => new List<Vector3>(MaxNumDotsPerHighlight))
         .ToArray();

      for (int i = 0; i < lightSections.Length; ++i)
      {
         var colliders = lightSections[i].GetGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Collider>())
            .Distinct();
         
         foreach (Collider col in colliders)
            colliderToLightSectionIndex.Add(col, i);
      }
      
      Assert.IsTrue(lightSections.Length > 0, "There must be at least one LightSection in the scene!");
   }

   public LayerMask GetDotsSurfaceLayerMask() => dotsSurfaceLayerMask;
   
   public void Highlight(RadarHighlightLocation location) => Highlight(location, dotsSurfaceLayerMask);

   public void Highlight(RadarHighlightLocation location, LayerMask layerMask)
   {
      //Debug.DrawLine(location.originalRay.origin, location.pointOnSurface, Color.green, 20.0f);
      
      Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, location.originalRay.direction);
      float distanceFromOrigin = Vector3.Distance(location.originalRay.origin, location.pointOnSurface);
      float displacementRadius = distanceFromOrigin * Mathf.Tan(Mathf.Deg2Rad * location.dotEmissionConeAngle);

      var results  = new NativeArray<RaycastHit>    (MaxNumDotsPerHighlight, Allocator.TempJob);
      var commands = new NativeArray<RaycastCommand>(MaxNumDotsPerHighlight, Allocator.TempJob);

      // Generate raycast commands
      for (int i = 0; i < MaxNumDotsPerHighlight; ++i)
      { 
         Vector3 target = location.pointOnSurface + rotation * (Random.insideUnitCircle * displacementRadius);
         commands[i] = new RaycastCommand(location.originalRay.origin, target - location.originalRay.origin, maxDotSpawnDistance, layerMask);
      }

      // Execute the raycasts
      JobHandle jobHandle = RaycastCommand.ScheduleBatch(commands, results, 1);
      jobHandle.Complete();
      
      // Process raycast results
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
         { 
            Debug.Log($"Collider {dotHit.collider} doesn't belong to any LightSection.");
            continue;
         }

         positionsBuffers[sectionIndex].Add(dotHit.point);
      }

      for (int i = 0; i < lightSections.Length; ++i)
      {
         LightSection section = lightSections[i];
         List<Vector3> positionsBuffer = positionsBuffers[i];

         section.AddDots(positionsBuffer);
         positionsBuffer.ForEach(registry.RegisterDot);
         
         positionsBuffer.Clear();
      }
      
      results.Dispose();
      commands.Dispose();
   }
   
   public void DrawDebugInfoInEditor()
   {
      registry?.DrawDebugInfoInEditor();
   }
}