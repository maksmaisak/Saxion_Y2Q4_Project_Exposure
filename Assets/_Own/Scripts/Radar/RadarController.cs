using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;
using VRTK.UnityEventHelper;

public class RadarController : VRTK_InteractableObject
{
    [Header("Radar Controller")] 
    [SerializeField] RadarTool radarTool;
    [SerializeField] float chargeUpDuration = 1.6f;
    [SerializeField] float releaseDelay = 0.3f;

    [Header("Spinning Thing Settings")] 
    [SerializeField] float maxAngularSpeed = 1500.0f;
    [SerializeField] float angularStepSpeed = 20.0f;
    [SerializeField] Transform spinningThingTransform;

    [Header("Particles Settings")] 
    [SerializeField] ParticleSystem chargeupParticleSystem;
    [SerializeField] float maxParticleEmissionRate = 100.0f;

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
    private bool forceCharged;

    private float spinningThingSpeed;
    private float lastChargeUpStartingTime;
    private float lastReleaseStartingTime;
    private float chargingUpDiff;
    
    private Coroutine interruptingSoundCoroutine;

    IEnumerator Start()
    {
        Assert.IsNotNull(shootClip);
        Assert.IsNotNull(chargeUpClip);
        Assert.IsNotNull(interruptClip);
        Assert.IsNotNull(releaseAudioSource);
        Assert.IsNotNull(chargeUpAudioSource);
        Assert.IsNotNull(spinningThingTransform);
        Assert.IsNotNull(chargeupParticleSystem);
        
        ParticleSystem.EmissionModule emission = chargeupParticleSystem.emission;
        emission.rateOverTime = 0.0f;

        yield return new WaitUntil(() => radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }
    
    protected override void Update()
    {
        base.Update();

        float multiplier = isChargingUp ? Time.deltaTime : -Time.deltaTime;
        
        spinningThingSpeed = Mathf.Clamp(spinningThingSpeed + angularStepSpeed * multiplier, 0.0f, maxAngularSpeed);
        spinningThingTransform.Rotate(spinningThingSpeed * Time.deltaTime * Vector3.up, Space.Self);

        ParticleSystem.EmissionModule emission = chargeupParticleSystem.emission;
        float currentEmissionRate = emission.rateOverTime.constant;
        emission.rateOverTime = Mathf.Clamp(currentEmissionRate + maxParticleEmissionRate * multiplier / chargeUpDuration, 0.0f, maxParticleEmissionRate);
        
        ChargeUp();
    }

    public float GetChargeupDuration() => chargeUpDuration;

    public void SetIsUsable(bool isUsable)
    {
        if (canUse && !isUsable)
        {
            StopAllCoroutines();

            if (isChargingUp)
                StartPlayingInterruptIfNeeded();
            
            lastChargeUpStartingTime = 0.0f;
            lastReleaseStartingTime = 0.0f;
            chargingUpDiff = 0.0f;
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

        if (isHandGrabbed)
            isChargingUp = true;
        else
            forceCharged = true;
    }

    public override void StopUsing(VRTK_InteractUse previousUsingObject = null, bool resetUsingObjectState = true)
    {
        base.StopUsing(previousUsingObject, resetUsingObjectState);

        if (!canUse)
            return;

        StartPlayingInterruptIfNeeded();
    }

    public override void StartTouching(VRTK_InteractTouch currentTouchingObject = null)
    {
        if (!isGrabbable)
            return;
        
        base.StartTouching(currentTouchingObject);
        if (!currentTouchingObject)
            return;

        var interactGrab = currentTouchingObject.GetComponent<VRTK_InteractGrab>();
        if (!interactGrab)
            return;
        
        interactGrab.AttemptGrab();
    }

    private void ChargeUp()
    {
        if (isChargingUp || forceCharged)
        {
            chargingUpDiff += Time.deltaTime;

            if (chargingUpDiff >= chargeUpDuration)
            {
                FadeInAndPlay(chargeUpAudioSource, shootClip, shootVolume, 0.0f);

                radarTool.Pulse();

                if (isHandGrabbed)
                    isChargingUp = false;

                if (forceCharged)
                    forceCharged = false;

                chargingUpDiff = 0;
            }
        }
        else
            chargingUpDiff = 0;
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
        if (forceCharged)
            forceCharged = false;

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

        isHandGrabbed = true;
    }
}

