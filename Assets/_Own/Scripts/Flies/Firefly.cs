using System;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(SteeringManager))]
public class Firefly : MonoBehaviour 
{
    [SerializeField] SteeringManager steeringManager;
    [SerializeField] float flockingRadiusInner = 0.8f;
    [SerializeField] float flockingRadiusOuter = 1.0f;
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
            .AvoidObstacles()
            .StayInSphere(swarm.flockingCenter, flockingRadiusInner, flockingRadiusOuter)
            .LookWhereGoing();
    }
    
    public Firefly SetSwarm(Swarm newSwarm)
    {
        this.swarm = newSwarm;
        return this;
    }
}
