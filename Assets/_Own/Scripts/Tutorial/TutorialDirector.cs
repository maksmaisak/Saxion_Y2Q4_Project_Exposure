using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class TutorialDirector : MonoBehaviour
{
    [SerializeField] float delayBeforeStart = 2.0f;
    [SerializeField] float rotatingDuration = 4.235f;
    [Space] 
    [SerializeField] RadarController radarController;
    [SerializeField] Transform rotateTransform;
    [Space] 
    [SerializeField] FlyingSphere overrideWavespherePrefab;
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
    [SerializeField] AudioClip engineStartUpClip;
    [SerializeField] AudioClip engineRunClip;
    [SerializeField] private AudioClip engineRunLoopClip;
    [SerializeField] AudioClip engineSlowDownClip;
    [SerializeField] AudioSource rotatingPartAudioSource;
    
    [FormerlySerializedAs("timeForMachineToDisappear")]
    [Space]
    [SerializeField] float delayAfterGunIsGrabbed = 0.5f;
    [SerializeField] private float machineOffsetY = -2.5f;
    [SerializeField] private float machineOffsetZ = -1.5f;

    private AudioSource audioSource;
    private RadarTool radarTool;
    private bool isTutorialRunning;

    void Start()
    {
        EnsureIsInitializedCorrectly();

        audioSource = GetComponent<AudioSource>();

        radarController.isGrabbable = false;

        radarController.InteractableObjectGrabbed += OnInteractableObjectGrabbed;

        void OnInteractableObjectGrabbed(object sender, VRTK.InteractableObjectEventArgs e)
        {
            radarController.InteractableObjectGrabbed -= OnInteractableObjectGrabbed;

            if (handTutorial)
                handTutorial.Remove();

            this.Delay(controllerTutorialAppearDelay, () =>
            {
                if (controllerTutorial)
                    controllerTutorial.gameObject.SetActive(true);
            });
        }
    }

    public void StartTutorial()
    {
        if (isTutorialRunning)
            return;

        isTutorialRunning = true;
        StartCoroutine(TutorialCoroutine());
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

        // Make controller disappear on pulse
        radarTool.onPulse.AddListener(OnPulse);

        void OnPulse()
        {
            radarTool.onPulse.RemoveListener(OnPulse);
            if (controllerTutorial)
                controllerTutorial.Remove();
        }

        yield return new WaitForSeconds(handTutorialAppearDelay);

        if (handTutorial)
            handTutorial.gameObject.SetActive(true);

        yield return new WaitUntil(() => radarController.IsGrabbed());
        
        yield return new WaitForSeconds(delayAfterGunIsGrabbed);
        
        float timeOfMovement = audioSource.pitch * (engineStartUpClip.length + engineRunLoopClip.length + engineSlowDownClip.length);
        
        yield return MoveMachine(new Vector3(0, machineOffsetY, 0), timeOfMovement)
            .WaitForCompletion();
        
        yield return MoveMachine(new Vector3(0, 0, machineOffsetZ), timeOfMovement)
            .WaitForCompletion();

        Destroy(gameObject);
    }

    private Sequence RotateMachine(Vector3 rotateTo, float tweenDuration)
    {
        return DOTween.Sequence()
            .Join(RotationAudioSequence())
            .Join(rotateTransform.DORotate(rotateTo, tweenDuration).SetEase(Ease.InOutQuad));
    }

    private Sequence MoveMachine(Vector3 moveTo, float tweenDuration)
    {
        return DOTween.Sequence()
            .Join(MovementAudioSequence())
            .Join(transform.DOMove(moveTo, tweenDuration).SetRelative().SetEase(Ease.InOutSine));
    }

    private Sequence MovementAudioSequence()
    {
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                audioSource.clip = engineStartUpClip;
                audioSource.Play();
            })
            .AppendInterval(engineStartUpClip.length)
            .AppendCallback(() =>
            {
                audioSource.clip = engineRunLoopClip;
                audioSource.Play();
            })
            .AppendInterval(engineRunLoopClip.length)
            .AppendCallback(() =>
            {
                audioSource.clip = engineSlowDownClip;
                audioSource.Play();
            });
    }

    private Sequence RotationAudioSequence()
    {        
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                rotatingPartAudioSource.clip = engineStartUpClip;
                rotatingPartAudioSource.Play();
            })
            .AppendInterval(engineStartUpClip.length)
            .AppendCallback(() =>
            {
                rotatingPartAudioSource.clip = engineRunClip;
                rotatingPartAudioSource.Play();
            })
            .AppendInterval(engineRunClip.length)
            .AppendCallback(() =>
            {
                rotatingPartAudioSource.clip = engineSlowDownClip;
                rotatingPartAudioSource.Play();
            });
    }

    private void EnsureIsInitializedCorrectly()
    {
        Assert.IsNotNull(radarController);
        Assert.IsNotNull(rotateTransform);
        radarTool = radarController.GetComponent<RadarTool>();
        Assert.IsNotNull(radarTool);

        Assert.IsNotNull(engineStartUpClip);
        Assert.IsNotNull(engineRunClip);
        Assert.IsNotNull(engineSlowDownClip);
        Assert.IsNotNull(rotatingPartAudioSource);
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

        void WavesphereSpawnedHandler(RadarTool sender, FlyingSphere flyingSphere)
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