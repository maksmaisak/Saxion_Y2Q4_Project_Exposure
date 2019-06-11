using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GlobalSwarm : MonoBehaviour
{
	[SerializeField] private GameObject flyPrefab;
	[SerializeField] private GameObject goalPrefab;
	[SerializeField] private GameObject objectToAvoid;

	public static GameObject sObjectToAviod;

	public const float AreaSize = 2f;
	public const float AreaHeight = 1f;

	private const int NumFlies = 20;
	public static readonly GameObject[] AllFlies = new GameObject[NumFlies];
	public static Vector3 GoalPos = Vector3.zero;
	
	private void OnEnable ()
	{
		sObjectToAviod = objectToAvoid;
		
		for (var i = 0; i < NumFlies; i++) 
		{
			var position = goalPrefab.transform.position;
			var pos = new Vector3 (position.x,position.y,position.y);
			
			AllFlies[i] = Instantiate (flyPrefab, pos, Quaternion.identity);
			AllFlies[i].transform.parent = transform;
		}

		GoalPos = goalPrefab.transform.position;
	}
}
