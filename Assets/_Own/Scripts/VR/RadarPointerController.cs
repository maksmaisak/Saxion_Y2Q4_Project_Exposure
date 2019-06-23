using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class RadarPointerController : MyBehaviour, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] VRTK_Pointer pointer;
    [SerializeField] RadarController radarController;

    private Navpoint[] navpoints;
    
    void Start()
    {
        navpoints = FindObjectsOfType<Navpoint>();
        pointer = pointer ? pointer : GetComponentInChildren<VRTK_Pointer>();
        radarController = radarController ? radarController : FindObjectOfType<RadarController>();

        Assert.IsNotNull(pointer);
        pointer.Toggle(true);
    }

    // TODO only toggle if it's the current section getting revealed
    public void On(OnRevealEvent message)
    {
        Toggle(false);
    }
    
    public void On(OnTeleportEvent teleport)
    {
        LightSection lightSection = teleport.navpoint.GetComponentInParent<LightSection>();
        Toggle(!lightSection || !lightSection.isRevealed);
    }

    private void Toggle(bool isRadarControllerOn)
    {
        bool areAnyNavpointsNotUsed = navpoints.Any(n => !n.isUsed);
        pointer.Toggle(!isRadarControllerOn && areAnyNavpointsNotUsed);
        
        if (radarController)
            radarController.SetIsUsable(isRadarControllerOn && areAnyNavpointsNotUsed);
    }
}