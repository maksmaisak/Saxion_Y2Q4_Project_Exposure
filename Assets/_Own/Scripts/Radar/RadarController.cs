using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.PlayerLoop;
using VRTK;

public class RadarController : VRTK_InteractableObject
{
    [Header("Radar Controller")] 
    [SerializeField] RadarTool radarTool;
    [SerializeField] float chargeUpDuration = 1.6f;
    [SerializeField] float releaseDelay = 0.3f;

    [Header("Spinning Thing Settings")] 
    [SerializeField] float maxAngularSpeed = 1500.0f;
    [SerializeField] float angularStepSpeed = 20.0f;
    [SerializeField] GameObject spinningThingGameObject;
    
    [Header("Audio Settings")]
    [SerializeField] AudioClip shootClip;
    [SerializeField] AudioClip chargeUpClip;
    [SerializeField] AudioClip interruptClip;
    [SerializeField] float shootVolume = 0.8f;
    [SerializeField] float chargeUpVolume = 0.6f;
    [SerializeField] float interruptVolume = 0.35f;
    [SerializeField] float fadeInDuration = 0.02f;
    [SerializeField] float fadeOutDuration = 0.05f;
    [SerializeField] AudioSource chargeUpAudioSource;
    [SerializeField] AudioSource releaseAudioSource;

    private bool canUse = true;
    private bool isHandGrabbed;
    private bool isChargingUp;

    private float spinningThingSpeed;
    private float lastChargeUpStartingTime;
    private float lastReleaseStartingTime;
    
    private Coroutine interruptingSoundCoroutine;
    private Coroutine fireRadarCoroutine;

    IEnumerator Start()
    {
        Assert.IsNotNull(shootClip);
        Assert.IsNotNull(chargeUpClip);
        Assert.IsNotNull(interruptClip);
        Assert.IsNotNull(releaseAudioSource);
        Assert.IsNotNull(chargeUpAudioSource);
        Assert.IsNotNull(spinningThingGameObject);

        yield return new WaitUntil(() => radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    public void SetIsUsable(bool isUsable)
    {
        if (canUse && !isUsable)
        {
            StopAllCoroutines();

            if(isChargingUp)
                StartPlayingInterruptIfNeeded();
            
            lastChargeUpStartingTime = 0.0f;
            lastReleaseStartingTime = 0.0f;
        }

        canUse = isUsable;
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        base.StartUsing(currentUsingObject);

        if (!canUse)
            return;

        float timeSinceReleaseStarted = Time.time - lastReleaseStartingTime;
        if (timeSinceReleaseStarted <= releaseDelay && interruptingSoundCoroutine != null)
            StopCoroutine(interruptingSoundCoroutine);
        
        lastChargeUpStartingTime = Time.time;

        FadeInAndPlay(chargeUpAudioSource, chargeUpClip, chargeUpVolume, fadeInDuration);

        if(fireRadarCoroutine != null)
            StopCoroutine(fireRadarCoroutine);
        
        // Maybe use DOTween and instead of chargeUpDuration use clip.length (however this is easier to change)
        fireRadarCoroutine = this.Delay(chargeUpDuration, () =>
        {
            FadeInAndPlay(chargeUpAudioSource, shootClip, shootVolume, 0.0f);

            radarTool.Probe();
            
            TutorialController.instance?.DeleteGameObject();
            
            if (isHandGrabbed)
                isChargingUp = false;
        });

        if (isHandGrabbed)
            isChargingUp = true;
    }

    public override void StopUsing(VRTK_InteractUse previousUsingObject = null, bool resetUsingObjectState = true)
    {
        base.StopUsing(previousUsingObject, resetUsingObjectState);

        if (!canUse)
            return;

        StartPlayingInterruptIfNeeded();
    }

    protected override void Update()
    {
        base.Update();

        spinningThingSpeed = Mathf.Clamp(
            isChargingUp
                ? spinningThingSpeed + angularStepSpeed * Time.deltaTime
                : spinningThingSpeed - angularStepSpeed * Time.deltaTime,
            0.0f, maxAngularSpeed);
        
        spinningThingGameObject.transform.Rotate(spinningThingSpeed * Time.deltaTime * Vector3.up, Space.Self);
    }

    private void StartPlayingInterruptIfNeeded()
    {
        float timeSinceChargeupStarted = Time.time - lastChargeUpStartingTime;
        if (timeSinceChargeupStarted >= chargeUpDuration)
            return;

        lastReleaseStartingTime = Time.time;

        interruptingSoundCoroutine = StartCoroutine(PlayInterrupt());
    }

    private IEnumerator PlayInterrupt()
    {
        StopAllCoroutines();

        FadeOutAndStop(chargeUpAudioSource, fadeOutDuration);
        
        yield return new WaitForSeconds(releaseDelay);

        releaseAudioSource.volume = interruptVolume;
        releaseAudioSource.clip = interruptClip;
        releaseAudioSource.Play();

        if (isHandGrabbed)
            isChargingUp = false;
    }

    private void FadeInAndPlay(AudioSource source, AudioClip clip, float volume, float duration)
    {
        source.DOFade(volume, duration).OnComplete(() =>
        {
            source.clip = clip;
            source.Play();
        });
    }

    private void FadeOutAndStop(AudioSource source, float newFadeOutDuration = 0.1f) =>
        source.DOFade(0, newFadeOutDuration);

    public override void Grabbed(VRTK_InteractGrab currentGrabbingObject = null)
    {
        base.Grabbed(currentGrabbingObject);
        
        TutorialController.instance?.ApplyHighlightToObject();
        
        isHandGrabbed = true;
    }
}

