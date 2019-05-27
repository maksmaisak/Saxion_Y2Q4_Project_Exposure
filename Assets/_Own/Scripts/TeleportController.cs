using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class TeleportController : VRTK_HeightAdjustTeleport
{
    [SerializeField] GameObject floatingPlatform;

    protected override void OnTeleported(object sender, DestinationMarkerEventArgs e)
    {
        base.OnTeleported(sender, e);

        floatingPlatform.transform.rotation = e.target.transform.rotation;
        floatingPlatform.transform.position = e.destinationPosition;
    }
}
