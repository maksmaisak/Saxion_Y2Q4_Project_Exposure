using System;
using UnityEngine;

/// Whatever data is necessary to highlight an area with a wavesphere.
public struct RadarHighlightLocation
{
    // A ray from the source of the wave to the target area
    public Ray originalRay;
    
    // A point in the target area around which the dots will be added
    public Vector3 pointOnSurface;
    
    // Dots will be added within a cone where the tip is at originalRay.origin.
    // The cone is pointing along originalRay.direction.
    // This is the angle of that cone in degrees.
    // Increasing this increases the spread of the dots.
    public float dotEmissionConeAngle;
    
    // Dots will only be added if they are close enough to the pointOnSurface (along originalRay).
    public float maxDotDistanceFromSurfacePointAlongOriginalRay;
}