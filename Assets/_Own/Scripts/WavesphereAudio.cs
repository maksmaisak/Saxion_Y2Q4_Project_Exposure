using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavesphereAudio : Singleton<WavesphereAudio>
{
    [SerializeField] AudioClip[] grabAudioClips;
    [SerializeField] float delayTillReset = 1.0f;

    private int currentClipIndex = 0;
    private float lastCatchTime;

    public AudioClip GetGrabAudioClip()
    {
        if (grabAudioClips == null || grabAudioClips.Length == 0)
            return null;

        if (Time.time - lastCatchTime < lastCatchTime)
            StopAllCoroutines();

        AudioClip clip = grabAudioClips[currentClipIndex];
        currentClipIndex = (currentClipIndex + 1) % grabAudioClips.Length;

        this.Delay(delayTillReset, () => { currentClipIndex = 0; });

        lastCatchTime = Time.time;
        return clip;
    }
}
