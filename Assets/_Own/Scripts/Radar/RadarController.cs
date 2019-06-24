using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class RadarController : VRTK_InteractableObject
{
    [Serializable]
    private struct AudioSourceSettings
    {
        public static AudioSourceSettings GetDefault() => new AudioSourceSettings
        {
            fadeInDuration = 0.05f,
            fadeOutDuration = 0.05f
        };
        
        public AudioSource audioSource;
        public float fadeInDuration;
        public float fadeOutDuration;

        [HideInInspector] public float volume;
        [HideInInspector] public float duration;
    }
    
    [Header("Radar Controller")] 
    [SerializeField] RadarTool radarTool;
    [SerializeField] float chargeUpDuration = 1.6f;
    [SerializeField] float releaseDelay = 0.3f;

    [Header("Spinning Thing Settings")] 
    [SerializeField] float maxAngularSpeed = 1500.0f;
    [SerializeField] Transform spinningThingTransform;

    [Header("Particles Settings")] 
    [SerializeField] ParticleSystem chargeupParticleSystem;

    [Header("Audio Settings")]
    [SerializeField] AudioSourceSettings audioChargeup  = AudioSourceSettings.GetDefault();
    [SerializeField] AudioSourceSettings audioShoot     = AudioSourceSettings.GetDefault();
    [SerializeField] AudioSourceSettings audioInterrupt = AudioSourceSettings.GetDefault();
    [SerializeField] AudioSourceSettings audioGrabbed   = AudioSourceSettings.GetDefault();

    private bool canUse = true;
    private bool isChargingUp;
    
    // From 0 to 1. Goes from 0 to 1 as you charge up. Pulse happens when it reaches 1
    private float chargeup;
    
    private float maxParticleEmissionRate;

    private Coroutine interruptCoroutine;
    
    IEnumerator Start()
    {
        void Initialize(ref AudioSourceSettings s)
        {
            Assert.IsNotNull(s.audioSource);
            Assert.IsNotNull(s.audioSource.clip);

            s.volume = s.audioSource.volume;
            s.duration = s.audioSource.pitch * s.audioSource.clip.length;
        }
        
        Initialize(ref audioChargeup);
        Initialize(ref audioShoot);
        Initialize(ref audioInterrupt);
        Initialize(ref audioGrabbed);

        Assert.IsNotNull(spinningThingTransform);
        Assert.IsNotNull(chargeupParticleSystem);
        
        ParticleSystem.EmissionModule emission = chargeupParticleSystem.emission;
        maxParticleEmissionRate = emission.rateOverTime.constant;
        emission.rateOverTime = 0.0f;

        yield return new WaitUntil(() => radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }
    
    protected override void Update()
    {
        base.Update();
        
        ChargeUp();
        
        float spinningThingSpeed = IsGrabbed() ? Mathf.Lerp(0.0f, maxAngularSpeed, chargeup) : 0.0f;
        spinningThingTransform.Rotate(spinningThingSpeed * Time.deltaTime * Vector3.up, Space.Self);
        
        ParticleSystem.EmissionModule emission = chargeupParticleSystem.emission;
        emission.rateOverTime = Mathf.Lerp(0.0f, maxParticleEmissionRate, chargeup);
    }

    public float GetChargeupDuration() => chargeUpDuration;

    public void SetIsUsable(bool isUsable)
    {
        if (canUse && !isUsable)
        {
            StopAllCoroutines();

            if (isChargingUp)
                interruptCoroutine = StartCoroutine(InterruptCoroutine());

            chargeup = 0.0f;
        }

        canUse = isUsable;
    }

    public override void Grabbed(VRTK_InteractGrab currentGrabbingObject = null)
    {
        base.Grabbed(currentGrabbingObject);
        FadeInAndPlay(audioGrabbed);
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
        
        transform.DOKill();

        interactGrab.AttemptGrab();
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        base.StartUsing(currentUsingObject);
        
        if (!canUse)
            return;

        if (interruptCoroutine != null)
        {
            StopCoroutine(interruptCoroutine);
            FadeOut(audioInterrupt);
            interruptCoroutine = null;
        }

        isChargingUp = true;
        
        FadeInAndPlay(audioChargeup);
        audioChargeup.audioSource.time = chargeup * audioChargeup.duration;
    }

    public override void StopUsing(VRTK_InteractUse previousUsingObject = null, bool resetUsingObjectState = true)
    {
        base.StopUsing(previousUsingObject, resetUsingObjectState);

        if (!canUse)
            return;

        if (isChargingUp)
        {
            Assert.IsNull(interruptCoroutine);
            interruptCoroutine = StartCoroutine(InterruptCoroutine());
        }
    }

    private void ChargeUp()
    {
        if (!isChargingUp)
        {
            chargeup = Mathf.Clamp01(chargeup - Time.deltaTime / (releaseDelay + audioInterrupt.duration));
            return;
        }
        
        chargeup += Time.deltaTime / chargeUpDuration;
        if (chargeup < 1.0f)
            return;
        
        isChargingUp = false;
        chargeup = 0.0f;

        radarTool.Pulse();
        FadeInAndPlay(audioShoot);
    }

    private IEnumerator InterruptCoroutine()
    {
        yield return new WaitForSeconds(releaseDelay);

        if (!isChargingUp)
            yield break;

        isChargingUp = false;
        FadeOut(audioChargeup);
        FadeInAndPlay(audioInterrupt);
    }

    private void FadeInAndPlay(AudioSourceSettings s)
    {
        var source = s.audioSource;
        source.DOKill();
        source.DOFade(s.volume, s.fadeInDuration);

        source.time = 0.0f;
        source.Play();
    }

    private void FadeOut(AudioSourceSettings s)
    {
        s.audioSource.DOKill();
        s.audioSource.DOFade(0.0f, s.fadeOutDuration);
    }
}

