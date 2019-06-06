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
    [SerializeField] int   pulseElasticity = 1;

    private Sequence pulseSequence;
    
    private IEnumerator Start()
    {
        audioSourceSecondary = audioSourceSecondary ? audioSourceSecondary : GetComponent<AudioSource>();

        yield return new WaitForSeconds(pulseDelay);
        
        pulseSequence = PlaySequence();

        var sphere = GetComponent<FlyingSphere>();
        if (sphere)
        {
            sphere.onCaught.AddListener(() =>
            {
                pulseSequence?.Kill();
                pulseSequence = null;
            });
        }
    }

    private Sequence PlaySequence()
    {
        Debug.Log(this + " started playing sequence.");
        transform.DOKill();
        return DOTween.Sequence()
            .AppendCallback(() => Debug.Log(this + "started loop"))
            .AppendCallback(() =>
            {
                audioSourceSecondary.clip = pulseSound;
                audioSourceSecondary.loop = false;
                audioSourceSecondary.Play();
            })
            .Append(transform.DOPunchScale(Vector3.one * pulsePunchScale, pulseDuration, pulseElasticity))
            .AppendInterval(pulseInterval)
            .SetLoops(-1, LoopType.Restart);
    }
}
