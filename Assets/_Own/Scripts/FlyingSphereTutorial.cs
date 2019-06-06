using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

[RequireComponent(typeof(FlyingSphere))]
public class FlyingSphereTutorial : MonoBehaviour
{
    [SerializeField] AudioSource audioSourceSecondary;
    [SerializeField] AudioClip pulseSound;
    [SerializeField] float pulseDelay = 2.0f;
    [SerializeField] float pulseDuration   = 1.0f;
    [SerializeField] float pulseInterval   = 0.3f;
    [SerializeField] float pulsePunchScale = 0.1f;
    [SerializeField] int   pulseVibrato = 1;
    [Space]
    [SerializeField] Rect visibleViewportRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

    private new Camera camera;
    private FlyingSphere wavesphere;
    private Sequence pulseSequence;

    IEnumerator Start()
    {
        audioSourceSecondary = audioSourceSecondary ? audioSourceSecondary : GetComponent<AudioSource>();
        
        wavesphere = GetComponent<FlyingSphere>();
        Assert.IsNotNull(wavesphere);
        
        yield return new WaitUntil(() => camera = Camera.main);
        
        yield return new WaitForSeconds(pulseDelay);
        
        pulseSequence = PlayPulseSequence();
        
        wavesphere.onCaught.AddListener(() =>
        {
            pulseSequence?.Kill();
            pulseSequence = null;

            if (audioSourceSecondary && audioSourceSecondary.isPlaying)
                audioSourceSecondary.DOFade(0.0f, 0.5f);
        });
    }

    void Update()
    {
        if (!camera)
            return;
        
        bool isVisible = IsVisibleToCamera();
        wavesphere.speedMultiplier = Mathf.MoveTowards(wavesphere.speedMultiplier, isVisible ? 1.0f : 0.0f, Time.deltaTime);
    }

    private Sequence PlayPulseSequence()
    {
        transform.DOKill();
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                if (!audioSourceSecondary)
                    return;
                audioSourceSecondary.clip = pulseSound;
                audioSourceSecondary.loop = false;
                audioSourceSecondary.Play();
            })
            .Append(transform.DOPunchScale(Vector3.one * pulsePunchScale, pulseDuration, pulseVibrato))
            .AppendInterval(pulseInterval)
            .SetLoops(-1, LoopType.Restart);
    }

    private bool IsVisibleToCamera()
    {
        Vector3 viewportPos = camera.WorldToViewportPoint(transform.position, Camera.MonoOrStereoscopicEye.Mono);
        return viewportPos.z > 0 && visibleViewportRect.Contains(viewportPos);
    }
}
