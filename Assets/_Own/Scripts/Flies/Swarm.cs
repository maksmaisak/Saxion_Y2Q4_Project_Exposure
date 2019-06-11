using UnityEngine;

public class Swarm : MonoBehaviour
{
	[SerializeField] GameObject flyPrefab;
	[SerializeField] Transform goal;
	[SerializeField] GameObject objectToAvoid;

	public static GameObject sObjectToAvoid;
    
    private const int NumFlies = 20;
	public static readonly GameObject[] AllFlies = new GameObject[NumFlies];
	public static Vector3 GoalPos = Vector3.zero;
	
    void OnEnable()
	{
		sObjectToAvoid = objectToAvoid;
        
        GoalPos = goal.position;
        for (var i = 0; i < NumFlies; i++) 
            AllFlies[i] = Instantiate(flyPrefab, GoalPos, Quaternion.identity, transform);
    }
}
