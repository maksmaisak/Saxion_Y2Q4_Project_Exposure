using VRTK;

public class RadarPointerRenderer : VRTK_StraightPointerRenderer
{
    protected override void CreatePointerOriginTransformFollow()
    {
        base.CreatePointerOriginTransformFollow();

        pointerOriginTransformFollow.moment = VRTK_TransformFollow.FollowMoment.OnLateUpdate;
    }
}