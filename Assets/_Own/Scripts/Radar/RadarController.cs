using System.Collections;
using DG.Tweening;
using UnityEngine;
using VRTK;

public class RadarController : VRTK_InteractableObject
{
    [SerializeField] RadarTool radarTool;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        base.StartUsing(currentUsingObject);
        Debug.Log("Radar fired!");

        radarTool.Probe();
    }
}

