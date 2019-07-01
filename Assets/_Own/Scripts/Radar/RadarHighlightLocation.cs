using System;
using UnityEngine;

/// Whatever data is necessary to highlight an area with a wavesphere.
public struct RadarHighlightLocation
{
    // A ray from the source of the wave to the target area
    public Ray originalRay;
    
    // A point in the target area around which the dots will be added and from which a wavesphere will be spawned
    public Vector3 pointOnSurface;

    // The distance from the original ray origin to the point on surface
    public float distanceFromOrigin;
    
    // Dots will be added within a cone where the tip is at originalRay.origin.
    // The cone is pointing along originalRay.direction.
    // This is the angle of that cone in degrees.
    // Increasing this increases the spread of the dots.
    public float dotEmissionConeAngle;
    
    // Dots will only be added if they are close enough to the pointOnSurface (along originalRay)
    public float maxDotDistanceFromSurfacePointAlongOriginalRay;

    // The speed with which the wavesphere will move
    public float wavesphereSpeed;

    // The light section the location belongs to
    public LightSection lightSection;
}