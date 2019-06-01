using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

public class TutorialDirector : MonoBehaviour
{
    [SerializeField] RadarController radarController;

    private RadarTool radarTool;

    IEnumerator Start()
    {
        EnsureIsInitializedCorrectly();

        radarController.isGrabbable = false;

        yield return new WaitForSeconds(2.0f);

        yield return radarController.transform
            .DORotate(Vector3.up * 90.0f, 5.0f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
        
        radarController.StartUsing();
        yield return new WaitForSeconds(2.0f);
        radarController.StopUsing();
        
        yield return radarController.transform
            .DORotate(Vector3.up * -90.0f, 5.0f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
        
        radarController.StartUsing();
        yield return new WaitForSeconds(2.0f);
        radarController.StopUsing();

        yield return radarController.transform
            .DORotate(Vector3.zero, 5.0f)
            .WaitForCompletion();

        radarController.isGrabbable = true;
    }

    private void EnsureIsInitializedCorrectly()
    {
        Assert.IsNotNull(radarController);
        radarTool = radarController.GetComponent<RadarTool>();
        Assert.IsNotNull(radarTool);
    }
}
