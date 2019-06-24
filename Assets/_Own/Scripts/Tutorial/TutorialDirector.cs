using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class TutorialDirector : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [SerializeField] float delayBeforeStart = 2.0f;
    [SerializeField] float rotatingDuration = 4.235f;
    [Space] 
    [SerializeField] RadarController radarController;
    [SerializeField] Transform rotateTransform;
    [Space] 
    [SerializeField] Wavesphere overrideWavespherePrefab;
    [SerializeField] float overridePulseSpeed = 1.0f;
    [SerializeField] float overrideWavesphereSpeed = 1.0f;
    [SerializeField] int overrideMaxNumWavespheresPerPulse = -1;
    [Space] 
    [SerializeField] TutorialMachineOpen opening;
    [SerializeField] float handTutorialAppearDelay = 0.01f;
    [SerializeField] float controllerTutorialAppearDelay = 0.7f;
    [SerializeField] ControllerTutorial controllerTutorial;
    [SerializeField] HandTutorial handTutorial;

    [Header("Audio Settings")]
    [SerializeField] AudioSource engineAudioSource;
    [SerializeField] AudioClip engineStartUpClip;
    [SerializeField] AudioClip engineRunClip;
    [SerializeField] AudioClip engineRunLoopClip;
    [SerializeField] AudioClip engineSlowDownClip;
    
    [FormerlySerializedAs("timeForMachineToDisappear")]
    [Space]
    [SerializeField] float delayAfterGunIsGrabbed = 0.5f;
    [SerializeField] private float machineOffsetY = -2.5f;
    [SerializeField] private float machineOffsetZ = -1.5f;
    
    [Space]
    [SerializeField] private Transform infographic;

    private RadarTool radarTool;
    private bool isTutorialRunning;

    void Start()
    {
        EnsureIsInitializedCorrectly();
        
        radarController.isGrabbable = false;
    }

    public void StartTutorial()
    {
        if (isTutorialRunning)
            return;

        isTutorialRunning = true;
        StartCoroutine(TutorialCoroutine());
    }

    public void ShowInfoGraphic()
    {
        infographic.gameObject.SetActive(true);
        infographic.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
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

        // Open
        yield return opening.Open().WaitForCompletion();
        
        // Unlock the gun
        radarTool.SetPulseSettings(oldPulseSettings);
        radarController.isGrabbable = true;
        radarController.transform.SetParent(null, true);
        
        StartCoroutine(HandTutorialCoroutine());
        StartCoroutine(ControllerTutorialCoroutine());
        
        yield return StartCoroutine(MachineMoveAwayCoroutine());
        Destroy(gameObject);
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
        bool didPulse = false;
        radarTool.onPulse.AddListener(OnPulse);
        void OnPulse()
        {
            radarTool.onPulse.RemoveListener(OnPulse);
            didPulse = true;
        }

        yield return new WaitForSeconds(controllerTutorialAppearDelay);

        if (didPulse || !controllerTutorial)
            yield break;

        controllerTutorial.gameObject.SetActive(true);
        
        yield return new WaitUntil(() => didPulse);

        if (controllerTutorial)
            controllerTutorial.Remove();
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

    public void On(OnRevealEvent revealEvent) => infographic.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => Destroy(infographic.gameObject));
}