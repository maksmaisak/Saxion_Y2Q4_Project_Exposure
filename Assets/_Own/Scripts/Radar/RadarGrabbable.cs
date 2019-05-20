using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK.GrabAttachMechanics;

[RequireComponent(typeof(Rigidbody))]
public class RadarGrabbable : VRTK_FixedJointGrabAttach
{
    private Rigidbody rb;
    private Collider[] attachedColliders;
    
    private void Start()
    {
        rb = rb ? rb : GetComponent<Rigidbody>();
        attachedColliders = GetComponentsInChildren<Collider>();
    }
    
    public override bool StartGrab(GameObject grabbingObject, GameObject givenGrabbedObject, Rigidbody givenControllerAttachPoint)
    {
        if (!base.StartGrab(grabbingObject, givenGrabbedObject, givenControllerAttachPoint))
            return false;

        StopAllCoroutines();

        rb.useGravity = false;
        rb.detectCollisions = false;
        
        this.Delay(0.5f, () =>
        {
            foreach (Collider col in attachedColliders)
                col.enabled = false;
        });

        return true;
    }

    public override void StopGrab(bool applyGrabbingObjectVelocity)
    {
        base.StopGrab(applyGrabbingObjectVelocity);
        
        StopAllCoroutines();

        rb.useGravity = true;
        rb.detectCollisions = true;

        foreach (Collider col in attachedColliders)
            col.enabled = true;
    }
    
    protected override void Initialise() {}
}
