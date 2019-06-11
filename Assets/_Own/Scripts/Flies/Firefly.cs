using System;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(SteeringManager))]
public class Firefly : MonoBehaviour 
{
    [SerializeField] SteeringManager steeringManager;
    [SerializeField] float maxDistanceFromFlockingCenter = 1.0f;
    private Swarm swarm;

    void Start()
    {
        steeringManager = steeringManager ? steeringManager : GetComponentInChildren<SteeringManager>();
        Assert.IsNotNull(steeringManager);
    }

    void FixedUpdate()
    {
        if (!swarm)
            return;

        steeringManager
            .Flock(swarm.steeringManagers)
            .LookWhereGoing();

        if ((swarm.flockingCenter - transform.position).sqrMagnitude > maxDistanceFromFlockingCenter * maxDistanceFromFlockingCenter)
            steeringManager.Seek(swarm.flockingCenter);
    }
    
    public Firefly SetSwarm(Swarm newSwarm)
    {
        this.swarm = newSwarm;
        return this;
    }
}
