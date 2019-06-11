using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

/// Exposes steering behaviors.
/// Attach to a gameobject, assign a rigidbody.
/// Call the public functions (preferably at FixedUpdate) to make the rigidbody move around using steering behaviors (http://red3d.com/cwr/steer/gdc99/).
/// Behaviors can be combined: for example, calling Wander, AvoidObstacles and LookWhereGoing makes the body wander randomly but avoiding collisions, and always facing the direction it's going.
public class SteeringManager : MonoBehaviour
{
    [Header("General settings")]
    [SerializeField] float maxSteeringForce = 10.0f;
    [SerializeField] float maxSpeed = 6.0f;
    [SerializeField] float maxRotationDegreesPerSecond = 180.0f;
    [SerializeField] new Rigidbody rigidbody;

    [Header("Collision avoidance settings")]
    [SerializeField] float collisionAvoidanceMultiplier = 20.0f;
    [SerializeField] float collisionAvoidanceRange      = 2.0f;
    
    [Header("Flocking settings")]
    [SerializeField] float separationFactor   = 1.0f;
    [SerializeField] float separationDistance = 5.0f;
    
    [Header("Wander settings")]
    [SerializeField] float circleRadius   = 2.0f;
    [SerializeField] float circleDistance = 2.0f;
    [SerializeField] float wanderAngle    = 20.0f;

    private Vector3 displacement;
    private float initialSteeringForce;
    private Vector3 steering = Vector3.zero;

    private Vector3 previousVelocity;
    private Vector3 previousSteering;

    public Vector3 velocity => rigidbody.velocity;

    void Start()
    {
        initialSteeringForce = maxSteeringForce;
        
        rigidbody = rigidbody ? rigidbody : GetComponentInChildren<Rigidbody>();
        Assert.IsNotNull(rigidbody);
        
        displacement = Vector3.forward * circleRadius;
    }

    void FixedUpdate()
    {
        Debug.DrawRay(transform.position, rigidbody.velocity, Color.red);

        if (maxSteeringForce >= 0.0f)
            steering = Vector3.ClampMagnitude(steering, maxSteeringForce);
        
        Debug.DrawRay(rigidbody.position, steering, Color.blue);
        rigidbody.AddForce(steering, ForceMode.Acceleration);
        rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);

        previousVelocity = rigidbody.velocity;
        previousSteering = steering;
        steering = Vector3.zero;
    }

    public SteeringManager Seek(Vector3 desiredPosition, float slowingRadius = 0.0f)
    {
        steering += DoSeek(desiredPosition, slowingRadius);
        return this;
    }

    public SteeringManager Flee(Vector3 targetToFlee)
    {
        steering += DoFlee(targetToFlee);
        return this;
    }

    public SteeringManager SeekOnYAxis(float targetHeight)
    {
        steering += DoSeekOnYAxis(targetHeight);
        return this;
    }

    public SteeringManager AvoidObstacles()
    {
        steering += DoObstaclesAvoidance();
        return this;
    }

    public SteeringManager FlockingSeparation(IEnumerable<SteeringManager> others)
    {
        steering += DoFlockingSeparation(others);
        return this;
    }

    public SteeringManager CompensateExternalForces()
    {
        steering += DoCompensateExternalForces();
        return this;
    }

    public SteeringManager Custom(Vector3 customSteeringForce)
    {
        steering += customSteeringForce;
        return this;
    }
    
    public SteeringManager LookWhereGoing()
    {
        if (rigidbody.velocity.sqrMagnitude <= float.Epsilon)
            SmoothRotateTowards(Quaternion.LookRotation(rigidbody.velocity));
        
        return this;
    }

    public SteeringManager LookAt(Vector3 targetPosition)
    {
        var targetRotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);
        SmoothRotateTowards(targetRotation);
        return this;
    }

    public SteeringManager SmoothRotateTowards(Quaternion targetRotation)
    {
        rigidbody.MoveRotation(Quaternion.RotateTowards(rigidbody.rotation, targetRotation, maxRotationDegreesPerSecond * Time.deltaTime));
        return this;
    }

    public SteeringManager SetMaxSteeringForce(float newSteeringForce)
    {
        maxSteeringForce = newSteeringForce;
        return this;
    }
    
    public SteeringManager Wander()
    {
        steering += DoWander();
        return this;
    }

    public float GetInitalSteeringForce()
    {
        return initialSteeringForce;
    }

    private Vector3 DoSeek(Vector3 target, float slowingRadius = 0f)
    {
        Vector3 toTarget = target - transform.position;
        Vector3 desiredVelocity = toTarget.normalized * maxSpeed;
        float distance = toTarget.magnitude;

        if (distance <= slowingRadius)
        {
            desiredVelocity *= distance / slowingRadius;
        }

        Vector3 force = desiredVelocity - rigidbody.velocity;
        return force;
    }

    private Vector3 DoFlee(Vector3 target)
    {
        Vector3 fromTarget = transform.position - target;
        Vector3 desiredVelocity = fromTarget.normalized * maxSpeed;
        Vector3 force = desiredVelocity - rigidbody.velocity;
        return force;
    }

    private Vector3 DoSeekOnYAxis(float targetHeight)
    {
        float desiredVelocityY = Mathf.Sign(targetHeight - transform.position.y) * maxSpeed;
        Vector3 force = Vector3.up * (desiredVelocityY - rigidbody.velocity.y);

        return force;
    }

    private Vector3 DoFlockingSeparation(IEnumerable<SteeringManager> others)
    {
        Vector3 totalForce = Vector3.zero;
        float sqrSeparationDistance = separationDistance * separationDistance;

        foreach (SteeringManager other in others)
        {
            if (this == other) continue;

            Vector3 position = transform.position;
            Vector3 otherPosition = other.transform.position;

            Vector3 delta = position - otherPosition;
            if (delta.sqrMagnitude <= sqrSeparationDistance)
            {
                totalForce += position - otherPosition;
            }
        }

        totalForce *= separationFactor;

        return totalForce;
    }

    private Vector3 DoObstaclesAvoidance()
    {
        Vector3 force = Vector3.zero;

        if (Physics.SphereCast(transform.position, 2, rigidbody.velocity.normalized, out RaycastHit hit, 0.2f))
        {
            if (hit.transform != this.transform && !hit.transform.CompareTag("Player"))
            {
                force += collisionAvoidanceMultiplier * hit.normal;
            }
        }

        if (Physics.SphereCast(transform.position, 0.25f, rigidbody.velocity.normalized, out RaycastHit hitForward, collisionAvoidanceRange))
        {
            if (hitForward.transform != this.transform && !hitForward.transform.CompareTag("Player"))
            {
                force += collisionAvoidanceMultiplier * hitForward.normal;
            }
        }

        if (Physics.SphereCast(transform.position, 0.25f, this.transform.right, out RaycastHit hitRight, collisionAvoidanceRange))
        {
            if (hitRight.transform != this.transform && !hitRight.transform.CompareTag("Player"))
            {
                force += collisionAvoidanceMultiplier * hitRight.normal;
            }
        }

        if (Physics.SphereCast(transform.position, 0.25f, -this.transform.right, out RaycastHit hitLeft, collisionAvoidanceRange))
        {
            if (hitLeft.transform != this.transform && !hitLeft.transform.CompareTag("Player"))
            {
                force += collisionAvoidanceMultiplier * hitLeft.normal;
            }
        }

        return force;
    }

    private Vector3 DoCompensateExternalForces()
    {
        Vector3 acceleration = rigidbody.velocity - previousVelocity;
        Vector3 externalAcceleration = acceleration - previousSteering;
        return -externalAcceleration;
    }
    
    private Vector3 DoWander()
    {
        Vector3 changeOfRotationEulerAngles = new Vector3(
            Random.Range(-wanderAngle, wanderAngle), 
            Random.Range(-wanderAngle, wanderAngle), 
            Random.Range(-wanderAngle, wanderAngle)
        ) * Time.fixedDeltaTime;
        Quaternion changeOfRotation = Quaternion.Euler(changeOfRotationEulerAngles);

        Quaternion newRotation = Quaternion.LookRotation(displacement) * changeOfRotation;
        newRotation.ToAngleAxis(out float _, out displacement);
        displacement *= circleRadius;
        
        Vector3 circleCenter = rigidbody.velocity.normalized * circleDistance;
        return DoSeek(rigidbody.position + circleCenter + transform.TransformVector(displacement));
    }
}

