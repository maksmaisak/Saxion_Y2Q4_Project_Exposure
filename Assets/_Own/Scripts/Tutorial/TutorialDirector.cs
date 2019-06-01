using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TutorialDirector : MonoBehaviour
{
    [SerializeField] RadarController radarController;
    
    IEnumerator Start()
    {
        Assert.IsNotNull(radarController);
        
        radarController.enabled = false;

        yield return new WaitForSeconds(5.0f);
        
        radarController.GetComponent<RadarTool>().Probe();
            
        yield return new WaitForSeconds(5.0f);

        radarController.enabled = true;
    }
}
