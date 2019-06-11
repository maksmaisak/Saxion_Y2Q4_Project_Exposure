using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Swarm : MonoBehaviour
{
	[SerializeField] Firefly fireflyPrefab;
	[SerializeField] Transform flockingCenterTransform;
    [SerializeField] int numFireflies = 10;
    
	public readonly List<Firefly> fireflies = new List<Firefly>();
    public readonly List<SteeringManager> steeringManagers = new List<SteeringManager>();

    public Vector3 flockingCenter => flockingCenterTransform.position;

    void OnEnable()
    {
        Vector3 origin = flockingCenterTransform.position;
        
        for (var i = 0; i < numFireflies; i++)
        {
            Firefly firefly = Instantiate(
                fireflyPrefab, 
                origin + Random.insideUnitSphere, Quaternion.identity, transform
            ).SetSwarm(this);
            
            fireflies.Add(firefly);
            steeringManagers.Add(firefly.GetComponentInChildren<SteeringManager>());
        }
    }
}
