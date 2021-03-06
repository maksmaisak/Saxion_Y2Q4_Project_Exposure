﻿using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class TutorialDirector : MyBehaviour
{
    [SerializeField] float delayBeforeStart = 2.0f;
    
    [Space] 
    [SerializeField] RadarController radarController;
    [SerializeField] Transform rotateTransform;
    [SerializeField] float rotatingDuration = 4.235f;

    [Header("Wave Pulse Settings")] 
    [SerializeField] Wavesphere overrideWavespherePrefab;
    [SerializeField] float overridePulseSpeed = 1.0f;
    [SerializeField] float overrideWavesphereSpeed = 1.0f;
    [SerializeField] int overrideMaxNumWavespheresPerPulse = -1;
    
    [Space] 
    [SerializeField] TutorialMachineOpen opening;
    [SerializeField] float handTutorialAppearDelay = 0.01f;
    [SerializeField] float controllerTutorialAppearDelay = 0.7f;
    [SerializeField] HandTutorial handTutorial;

    [Header("Audio Settings")]
    [SerializeField] AudioSource engineAudioSource;
    [SerializeField] AudioClip engineStartUpClip;
    [SerializeField] AudioClip engineRunClip;
    [SerializeField] AudioClip engineRunLoopClip;
    [SerializeField] AudioClip engineSlowDownClip;
    
    [Header("Machine Move Away Settings")]
    [SerializeField] float delayAfterGunIsGrabbed = 0.5f;
    [SerializeField] float machineOffsetY = -2.5f;
    [SerializeField] float machineOffsetZ = -1.5f;
    
    [Header("Infographics Settings")]
    [SerializeField] Infographics infographics;
    [SerializeField] float infographicsAppearDelay = 0.5f;

    [Header("Controller Settings")] 
    [SerializeField] ControllerTutorial leftController;
    [SerializeField] ControllerTutorial rightController;
    
    private RadarTool radarTool;
    private bool isTutorialRunning;
    private bool didPlayerPulse;

    void Start()
    {
        EnsureIsInitializedCorrectly();

        radarController.isGrabbable = false;

        SetUpPlayerPulseDetection();
    }

    private void SetUpPlayerPulseDetection()
    {
        radarTool.onPulse.AddListener(OnPulse);
        void OnPulse()
        {
            if (!radarController.IsGrabbed())
                return;
            
            Assert.IsFalse(didPlayerPulse);
            didPlayerPulse = true;
            radarTool.onPulse.RemoveListener(OnPulse);
        }
    }

    public void StartTutorial()
    {
        if (isTutorialRunning)
            return;

        isTutorialRunning = true;
        StartCoroutine(TutorialCoroutine());
    }
    
    private void EnsureIsInitializedCorrectly()
    {
        Assert.IsNotNull(radarController);
        Assert.IsNotNull(rotateTransform);
        radarTool = radarController.GetComponent<RadarTool>();
        Assert.IsNotNull(radarTool);
        
        Assert.IsNotNull(engineAudioSource);
        Assert.IsNotNull(engineStartUpClip);
        Assert.IsNotNull(engineRunClip);
        Assert.IsNotNull(engineSlowDownClip);
    }

    IEnumerator TutorialCoroutine()
    {
        PulseSettings oldPulseSettings = radarTool.GetPulseSettings();
        radarTool.SetPulseSettings(MakeOverridePulseSettings(oldPulseSettings));

        yield return new WaitForSeconds(delayBeforeStart);

        // Turn Right and Play Sound
        yield return RotateMachine(Vector3.up * 90.0f, rotatingDuration)
            .WaitForCompletion();

        // Pulse
        radarController.StartUsing();
        yield return new WaitForSeconds(radarController.GetChargeupDuration() + 0.1f);
        radarController.StopUsing();
        yield return WaitUntilAllSpawnedWavespheresAreCaught();

        // Turn left
        yield return RotateMachine(Vector3.up * -90.0f, rotatingDuration)
            .WaitForCompletion();

        // Pulse
        radarController.StartUsing();
        yield return new WaitForSeconds(radarController.GetChargeupDuration() + 0.1f);
        radarController.StopUsing();
        yield return WaitUntilAllSpawnedWavespheresAreCaught();

        // Turn forward
        yield return RotateMachine(Vector3.forward, rotatingDuration)
            .WaitForCompletion();

        // Unlock the gun
        radarTool.SetPulseSettings(oldPulseSettings);
        radarController.transform.SetParent(null, true);
        radarController.isGrabbable = true;

        // Open
        yield return opening.Open().WaitForCompletion();
        
        StartCoroutine(HandTutorialCoroutine());
        yield return new WaitUntil(() => radarController.IsGrabbed());
        StartCoroutine(ControllerTutorialCoroutine());
        StartCoroutine(InfographicsCoroutine());
        
        yield return StartCoroutine(MachineMoveAwayCoroutine());

        gameObject.SetActive(false);
    }
    
    private IEnumerator HandTutorialCoroutine()
    {
        yield return new WaitForSeconds(handTutorialAppearDelay);

        if (radarController.IsGrabbed() || !handTutorial) 
            yield break;
        
        handTutorial.gameObject.SetActive(true);
            
        yield return new WaitUntil(() => radarController.IsGrabbed());
            
        if (handTutorial)
            handTutorial.Remove();
    }

    private IEnumerator ControllerTutorialCoroutine()
    {
        yield return new WaitForSeconds(controllerTutorialAppearDelay);
        if (didPlayerPulse)
            yield break;

        ControllerTutorial controllerTutorial = radarController.IsGrabbed(VRTK_DeviceFinder.GetControllerLeftHand()) ? leftController : rightController;
        if (!controllerTutorial)
            yield break;
        
        controllerTutorial.gameObject.SetActive(true);

        yield return new WaitUntil(() => didPlayerPulse);

        if (rightController)
            rightController.Remove();
        
        if (leftController)
            leftController.Remove();
    }

    private IEnumerator InfographicsCoroutine()
    {
        yield return new WaitUntil(() => radarController.IsGrabbed());
        yield return new WaitForSeconds(infographicsAppearDelay);

        if (infographics)
            infographics.Show();
    }

    private IEnumerator MachineMoveAwayCoroutine()
    {
        yield return new WaitUntil(() => radarController.IsGrabbed());
        yield return new WaitForSeconds(delayAfterGunIsGrabbed);
        
        float timeOfMovement = engineAudioSource.pitch * (engineStartUpClip.length + engineRunLoopClip.length + engineSlowDownClip.length);
        yield return MoveMachine(new Vector3(0, machineOffsetY, 0), timeOfMovement).WaitForCompletion();
        yield return MoveMachine(new Vector3(0, 0, machineOffsetZ), timeOfMovement).WaitForCompletion();
    }

    private Sequence RotateMachine(Vector3 rotateTo, float tweenDuration)
    {
        return DOTween.Sequence()
            .Join(EngineAudioSequence(engineRunClip))
            .Join(rotateTransform.DORotate(rotateTo, tweenDuration).SetEase(Ease.InOutQuad));
    }

    private Sequence MoveMachine(Vector3 moveTo, float tweenDuration)
    {
        return DOTween.Sequence()
            .Join(EngineAudioSequence(engineRunLoopClip))
            .Join(transform.DOMove(moveTo, tweenDuration).SetRelative().SetEase(Ease.InOutSine));
    }

    private Sequence EngineAudioSequence(AudioClip runClip)
    {
        engineAudioSource.DOKill();
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                engineAudioSource.clip = engineStartUpClip;
                engineAudioSource.Play();
            })
            .AppendInterval(engineStartUpClip.length)
            .AppendCallback(() =>
            {
                engineAudioSource.clip = runClip;
                engineAudioSource.Play();
            })
            .AppendInterval(runClip.length)
            .AppendCallback(() =>
            {
                engineAudioSource.clip = engineSlowDownClip;
                engineAudioSource.Play();
            })
            .SetTarget(engineAudioSource);
    }

    private PulseSettings MakeOverridePulseSettings(PulseSettings settings)
    {
        if (overrideWavespherePrefab)
            settings.wavespherePrefab = overrideWavespherePrefab;

        settings.wavePulseSpeed = overridePulseSpeed;
        settings.wavesphereSpeedMin = settings.wavesphereSpeedMax = overrideWavesphereSpeed;
        settings.maxNumWavespheresPerPulse = overrideMaxNumWavespheresPerPulse;

        return settings;
    }

    // TODO BUG: if all currently existing wavespheres are caught, even if the wave is still going and will spawn more, this ends.
    private CustomYieldInstruction WaitUntilAllSpawnedWavespheresAreCaught()
    {
        int numWavespheresLeftToCatch = 0;
        bool anyWavespheresSpawned = false;

        void WavesphereSpawnedHandler(RadarTool sender, Wavesphere flyingSphere)
        {
            anyWavespheresSpawned = true;
            numWavespheresLeftToCatch += 1;

            flyingSphere.onCaught.AddListener(() => numWavespheresLeftToCatch -= 1);
        }

        radarTool.onSpawnedWavesphere.AddListener(WavesphereSpawnedHandler);

        return new WaitUntil(() =>
        {
            Assert.IsTrue(numWavespheresLeftToCatch >= 0);
            if (!anyWavespheresSpawned || numWavespheresLeftToCatch > 0)
                return false;

            radarTool.onSpawnedWavesphere.RemoveListener(WavesphereSpawnedHandler);
            return true;
        });
    }
}