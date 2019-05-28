using System;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class RadarPointerController : MyBehaviour, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] VRTK_Pointer pointer;

    void Start()
    {
        pointer = pointer ? pointer : GetComponentInChildren<VRTK_Pointer>();
        Assert.IsNotNull(pointer);
        pointer.Toggle(false);
    }

    // TODO only toggle if it's the current section getting revealed
    public void On(OnRevealEvent message)
    {
        pointer.Toggle(true);
    }
    
    public void On(OnTeleportEvent teleport)
    {
        LightSection lightSection = teleport.navpoint.GetComponentInParent<LightSection>();
        pointer.Toggle(lightSection && lightSection.isRevealed);
    }
}