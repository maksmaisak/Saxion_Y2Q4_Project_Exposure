using UnityEngine;
using UnityEngine.Serialization;

public class Firefly : MonoBehaviour {
    
	public float angularSpeed = 1.0f;

	public float neighborDistance = 2.0f;  
	public float collisionDistance = 0.2f;
	public float startingSpeed = 2f;
	public float maxSpeed = 1.0f;
	
	private float distanceToObjectToAvoid = 0.1f;
	private float speed  = 0.5f;
	private GameObject[] allFliesHandle;
	private Vector3 goalPosHandle;
	private Vector3 returnPosition = Swarm.GoalPos;

	public float sGoalSeeking = 1.0f;
	public float sAvoidance = 1.0f;
	public float sObjectToAvoid = 1.0f; 

    void Start() 
	{
		speed = Random.Range(startingSpeed * 0.5f, startingSpeed);
		allFliesHandle = Swarm.AllFlies;
    }

    void Update() 
	{
		ApplySwarm();
        transform.Translate(0, 0, Time.deltaTime * speed);
	}

    private void ApplySwarm()
	{
        float gSpeed = 0.1f;
        float dist = 0.0f;
        
		Vector3 vCohesion = Vector3.zero;
		Vector3 vAvoid = Vector3.zero;
		Vector3 vObjectToAvoid = Vector3.zero;
		int groupSize = 20;
		float sGo = sGoalSeeking;
		float sAv = sAvoidance;
		float sOb = sObjectToAvoid;
		goalPosHandle = Swarm.GoalPos;
        
		foreach (GameObject fly in allFliesHandle) 
		{
			if (fly == gameObject) 
                continue;
            
			dist = Vector3.Distance(fly.transform.position, transform.position);
            if (dist > neighborDistance) 
                continue;
            
			groupSize++;
            
			vCohesion += fly.transform.position;
            if (dist <= collisionDistance)
				vAvoid -= transform.position - fly.transform.position;
			
			float otherSpeed = fly.GetComponent<Firefly>().speed;
			gSpeed += otherSpeed;
        }
		
		if (groupSize > 20) 
        {
            vCohesion /= groupSize;
			speed = gSpeed / groupSize;
			speed = Mathf.Clamp (speed, startingSpeed, maxSpeed);
		}

        dist = Vector3.Distance(Swarm.sObjectToAvoid.transform.position, transform.position);

		if (dist <= distanceToObjectToAvoid) 
		{
			vObjectToAvoid -= transform.position - Swarm.sObjectToAvoid.transform.position;
        }
		Transform tf = transform;
		Vector3 position = tf.position;
		Vector3 vGoal = goalPosHandle - position;
		
		Vector3 direction = 
            vCohesion * sAv + 
            vGoal * sGo + 
            (vAvoid * -sAv) +
            (vObjectToAvoid * -sOb) - position;
        
        transform.rotation = Quaternion.Slerp(tf.rotation,
			Quaternion.LookRotation(direction),
			angularSpeed * Time.deltaTime
        );
	}
}
