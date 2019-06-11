using UnityEngine;

public class Swarm : MonoBehaviour {


	public float angularSpeed = 1.0f;

	public float neighborDistance = 2.0f;  
	public float collisionDistance = 0.2f; 
	public float startingSpeed = 2f;
	public float maxSpeed = 1.0f;
	
	private float distanceToObjectToAvoid = 0.1f;
	private float speed  = 0.5f;
	private GameObject[] allFliesHandle;
	private Vector3 goalPosHandle;
	private Vector3 returnPosition = GlobalSwarm.GoalPos;

	public float sGoalSeeking = 1.0f;
	public float sAvoidance = 1.0f;
	public float sObjectToAviod = 1.0f; 

	private void Start () 
	{
		speed = Random.Range (startingSpeed/2.0f, startingSpeed);
		allFliesHandle = GlobalSwarm.AllFlies;

	}

	private void Update () 
	{
		ApplySwarm ();
		
		transform.Translate(0,0, Time.deltaTime * speed);
	}

	private void ApplyReturn()
	{
		var transform1 = transform;
		var direction = returnPosition - transform1.position; 
		
		transform.rotation = Quaternion.Slerp ( transform1.rotation,
			Quaternion.LookRotation (direction),
			angularSpeed * Time.deltaTime);
		
		speed = Mathf.Lerp (speed, startingSpeed, Time.deltaTime);
	}
	
	private void ApplySwarm()
	{

		var gSpeed = 0.1f;
		float dist;
		var vCohesion = Vector3.zero;
		var vAvoid = Vector3.zero;
		var vObjectToAvoid = Vector3.zero;
		var groupSize = 20;
		var sGo = sGoalSeeking;
		var sAv = sAvoidance;
		var sOb = sObjectToAviod;
		goalPosHandle = GlobalSwarm.GoalPos;
		
		
		foreach (var flies in allFliesHandle) 
		{
			if (flies == gameObject) continue;
			dist = Vector3.Distance(flies.transform.position, transform.position);


			if (!(dist <= neighborDistance)) continue;
			groupSize++;


			vCohesion += flies.transform.position;

			if (dist <= collisionDistance)
				vAvoid -= transform.position - flies.transform.position;
			
			var otherSpeed = flies.GetComponent<Swarm>().speed;
			gSpeed += otherSpeed;

		}
		
		if (groupSize > 20) {

			
			vCohesion /= groupSize;
			speed = gSpeed / groupSize;
			speed = Mathf.Clamp (speed, startingSpeed, maxSpeed);
		}
		
		
		dist = Vector3.Distance(GlobalSwarm.sObjectToAviod.transform.position, transform.position);

		if (dist <= distanceToObjectToAvoid) 
		{
			vObjectToAvoid -= transform.position - GlobalSwarm.sObjectToAviod.transform.position;

		}
		var transform1 = transform;
		var position = transform1.position;
		var vGoal = goalPosHandle - position;
		
		var direction = vCohesion * sAv + vGoal * sGo - vAvoid * sAv - vObjectToAvoid * sOb - position;
		
		transform.rotation = Quaternion.Slerp ( transform1.rotation,
			Quaternion.LookRotation (direction) ,
			angularSpeed * Time.deltaTime);
	}
}
