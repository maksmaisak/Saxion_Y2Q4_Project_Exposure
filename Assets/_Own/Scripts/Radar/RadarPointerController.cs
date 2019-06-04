using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class RadarPointerController : VRTK_StraightPointerRenderer
{
    [Header("Laser Settings")]
    [SerializeField] LayerMask laserPointerCollisionLayer;
    
    protected override void CreatePointerOriginTransformFollow()
    {
        base.CreatePointerOriginTransformFollow();

        pointerOriginTransformFollow.moment = VRTK_TransformFollow.FollowMoment.OnLateUpdate;
    }

    protected override void SetPointerAppearance(float tracerLength)
    {
        base.SetPointerAppearance(tracerLength);

        if (actualContainer != null)
        {
            if (destinationHit.collider != null)
            {
                if (!laserPointerCollisionLayer.ContainsLayer(destinationHit.collider.gameObject.layer))
                    return;
                
                actualContainer.transform.rotation = Quaternion.LookRotation(
                    destinationHit.collider.gameObject.transform.position - actualContainer.transform.position);
            }
            else
                actualContainer.transform.localRotation = Quaternion.identity;
        }
    }
}
