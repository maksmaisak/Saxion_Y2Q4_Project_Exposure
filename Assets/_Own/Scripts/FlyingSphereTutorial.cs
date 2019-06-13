using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Assertions;

[RequireComponent(typeof(FlyingSphere))]
public class FlyingSphereTutorial : MonoBehaviour
{
    [SerializeField] AudioSource audioSourceSecondary;
    [SerializeField] AudioClip pulseSound;
    [SerializeField] float pulseDelay      = 2.0f;
    [SerializeField] float pulseDuration   = 1.0f;
    [SerializeField] float pulseInterval   = 0.3f;
    [SerializeField] float pulsePunchScale = 0.1f;
    [SerializeField] int   pulseVibrato = 1;
    [SerializeField] float stoppingDistance = 1.75f;
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

        StartCoroutine(PulseCoroutine());
        
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
        
        var targetPosition = camera.transform.position;

        float distanceToTarget = (targetPosition - transform.position).sqrMagnitude;

        bool isWithinStoppingDistance = distanceToTarget <= stoppingDistance * stoppingDistance;

        float targetSpeedMultiplier = 1.0f;

        if (isWithinStoppingDistance && !IsVisibleToCamera())
            targetSpeedMultiplier = 0.0f;
        
        wavesphere.speedMultiplier = Mathf.MoveTowards(
            wavesphere.speedMultiplier, 
            targetSpeedMultiplier, 
            Time.deltaTime
        );
    }

    private IEnumerator PulseCoroutine()
    {
        transform.DOKill();

        pulseSequence = DOTween.Sequence()
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
            .SetAutoKill(false)
            .Pause();
        
        while (pulseSequence != null)
        {
            yield return new WaitUntil(() => !IsVisibleToCamera());

            if (pulseSequence == null)
                break;
            pulseSequence.Restart();
            yield return pulseSequence.WaitForCompletion();
        }
    }

    private bool IsVisibleToCamera()
    {
        Vector3 viewportPos = camera.WorldToViewportPoint(transform.position, Camera.MonoOrStereoscopicEye.Mono);
        return viewportPos.z > 0.0f && visibleViewportRect.Contains(viewportPos);
    }
}
