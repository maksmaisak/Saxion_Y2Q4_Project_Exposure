using UnityEngine;

public static class Vector3Extensions
{
    public static bool Longer(this Vector3 vec, float magnitude)
    {
        return vec.sqrMagnitude > magnitude * magnitude;
    }
    
    public static bool Shorter(this Vector3 vec, float magnitude)
    {
        return vec.sqrMagnitude < magnitude * magnitude;
    }
}