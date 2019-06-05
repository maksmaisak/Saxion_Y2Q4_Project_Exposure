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
}
