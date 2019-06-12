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

    protected override void SetSnappedObjectPosition(GameObject obj)
    {
        base.SetSnappedObjectPosition(obj);

        transform.parent = controllerAttachPoint.transform;
    }

    public override bool StartGrab(GameObject grabbingObject, GameObject givenGrabbedObject, Rigidbody givenControllerAttachPoint)
    {
        if (!base.StartGrab(grabbingObject, givenGrabbedObject, givenControllerAttachPoint))
            return false;

        StopAllCoroutines();

        rb.useGravity = false;
        rb.detectCollisions = false;
        rb.isKinematic = true;
        
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
        rb.isKinematic = false;

        foreach (Collider col in attachedColliders)
            col.enabled = true;
    }
    
    protected override void Initialise() {}
}
