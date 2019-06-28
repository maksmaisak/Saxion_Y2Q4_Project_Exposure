using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavesphereAudio : Singleton<WavesphereAudio>
{
    [SerializeField] AudioClip[] grabAudioClips;
    [Space]
    [SerializeField] float maxTimeBetweenConsecutiveSounds = 4.0f;

    private int currentClipIndex = 0;

    public AudioClip GetGrabAudioClip()
    {
        if (grabAudioClips == null || grabAudioClips.Length == 0)
            return null;

        StopAllCoroutines();

        AudioClip clip = grabAudioClips[currentClipIndex];
        currentClipIndex = (currentClipIndex + 1) % grabAudioClips.Length;

        this.Delay(maxTimeBetweenConsecutiveSounds, () => currentClipIndex = 0);

        return clip;
    }
}
