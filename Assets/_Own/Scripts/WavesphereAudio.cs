using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavesphereAudio : Singleton<WavesphereAudio>
{
    [SerializeField] AudioClip[] grabAudioClips;

    private int currentClipIndex = 0;

    public AudioClip GetGrabAudioClip()
    {
        if (grabAudioClips == null || grabAudioClips.Length == 0)
            return null;
        
        AudioClip clip = grabAudioClips[currentClipIndex];
        currentClipIndex = (currentClipIndex + 1) % grabAudioClips.Length;
        return clip;
    }
}
