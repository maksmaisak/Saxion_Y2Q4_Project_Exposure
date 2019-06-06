using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class FlyingSphereTutorial : MonoBehaviour
{
    [SerializeField] AudioSource audioSourceSecondary;
    [SerializeField] AudioClip pulseSound;
    [SerializeField] float pulseDelay = 2.0f;
    [SerializeField] float pulsePunchScale = 0.1f;
    [SerializeField] float pulseDuration   = 1.0f;
    [SerializeField] float pulseInterval   = 0.3f;
    [SerializeField] int   pulseVibrato = 1;

    private Sequence pulseSequence;
    
    private IEnumerator Start()
    {
        audioSourceSecondary = audioSourceSecondary ? audioSourceSecondary : GetComponent<AudioSource>();

        yield return new WaitForSeconds(pulseDelay);
        
        pulseSequence = PlayPulseSequence();

        var sphere = GetComponent<FlyingSphere>();
        if (sphere)
        {
            sphere.onCaught.AddListener(() =>
            {
                pulseSequence?.Kill();
                pulseSequence = null;

                if (audioSourceSecondary && audioSourceSecondary.isPlaying)
                    audioSourceSecondary.DOFade(0.0f, 0.5f);
            });
        }
    }

    private Sequence PlayPulseSequence()
    {
        transform.DOKill();
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                audioSourceSecondary.clip = pulseSound;
                audioSourceSecondary.loop = false;
                audioSourceSecondary.Play();
            })
            .Append(transform.DOPunchScale(Vector3.one * pulsePunchScale, pulseDuration, pulseVibrato))
            .AppendInterval(pulseInterval)
            .SetLoops(-1, LoopType.Restart);
    }
}
