using System;
using UnityEngine;
using DG.Tweening;
public class FlyingSphereTutorial : MonoBehaviour
{
    [SerializeField]AudioSource audioSourceSecoundary;
    [SerializeField] AudioClip pulseSound;
    
    [SerializeField] float pulsePunchScale = 0.1f;
    [SerializeField] float pulseDuration   = 1.0f;
    [SerializeField] float pulseInterval   = 0.3f;
    [SerializeField] int   pulseElasticity = 1;
    
    private void Start()
    {
        audioSourceSecoundary = GetComponent<AudioSource>();
        PlaySequence();
    }

    private void PlaySequence()
    {
        transform.DOKill();
        DOTween.Sequence()
            .AppendCallback(() => {
                audioSourceSecoundary.clip = pulseSound;
                audioSourceSecoundary.loop = false;
                audioSourceSecoundary.Play();
            })
            .Append(transform.DOPunchScale(Vector3.one * pulsePunchScale, pulseDuration, pulseElasticity))
            .AppendInterval(pulseInterval)
            .SetLoops(-1, LoopType.Restart);
    }
}
