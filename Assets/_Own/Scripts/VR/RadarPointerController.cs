using System;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class RadarPointerController : MyBehaviour, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] VRTK_Pointer pointer;
    [SerializeField] RadarController radarController;

    void Start()
    {
        pointer = pointer ? pointer : GetComponentInChildren<VRTK_Pointer>();
        radarController = radarController ? radarController : FindObjectOfType<RadarController>();

        Assert.IsNotNull(pointer);
        pointer.Toggle(false);
    }

    // TODO only toggle if it's the current section getting revealed
    public void On(OnRevealEvent message)
    {
        Toggle(true);
    }
    
    public void On(OnTeleportEvent teleport)
    {
        LightSection lightSection = teleport.navpoint.GetComponentInParent<LightSection>();
        Toggle(lightSection && lightSection.isRevealed);
    }

    private void Toggle(bool isPointerOn)
    {
        pointer.Toggle(isPointerOn);
        
        if (radarController)
            radarController.enabled = !isPointerOn;
    }
}