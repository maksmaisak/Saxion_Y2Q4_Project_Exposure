using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

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

    [Header("Audio Settings")] [SerializeField]
    AudioClip engineStartUpClip;

    [SerializeField] AudioClip engineRunClip;
    [SerializeField] AudioClip engineSlowDownClip;
    [SerializeField] AudioSource rotatingPartAudioSource;

    private AudioSource thisAudioSoruce;
    private RadarTool radarTool;
    private bool isTutorialRunning;

    void Start()
    {
        EnsureIsInitializedCorrectly();

        thisAudioSoruce = GetComponent<AudioSource>();

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
        yield return RotateAndPlaySoundSequence(Vector3.up * 90.0f, rotatingDuration)
            .WaitForCompletion();

        // Pulse
        radarController.StartUsing();
        yield return new WaitForSeconds(radarController.GetChargeupDuration() + 0.1f);
        radarController.StopUsing();
        yield return WaitUntilAllSpawnedWavespheresAreCaught();

        // Turn left
        yield return RotateAndPlaySoundSequence(Vector3.up * -90.0f, rotatingDuration)
            .WaitForCompletion();

        // Pulse
        radarController.StartUsing();
        yield return new WaitForSeconds(radarController.GetChargeupDuration() + 0.1f);
        radarController.StopUsing();
        yield return WaitUntilAllSpawnedWavespheresAreCaught();

        // Turn forward
        yield return RotateAndPlaySoundSequence(Vector3.forward, rotatingDuration)
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

        var timeOfMovement = engineRunClip.length + engineStartUpClip.length + engineSlowDownClip.length;

        yield return MoveAndPlaySoundSequence(new Vector3(0, -2.5f, 0), timeOfMovement)
            .WaitForCompletion();
        
        yield return MoveAndPlaySoundSequence(new Vector3(0, 0, -1.5f), timeOfMovement)
            .WaitForCompletion();

        gameObject.SetActive(false);
    }

    private Sequence RotateAndPlaySoundSequence(Vector3 rotateTo, float tweenDuration)
    {
        var audioSequence = DOTween.Sequence();
        audioSequence.AppendCallback(() =>
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

        return DOTween.Sequence().Join(audioSequence)
            .Join(rotateTransform.DORotate(rotateTo, tweenDuration).SetEase(Ease.InOutQuad));
    }

    private Sequence MoveAndPlaySoundSequence(Vector3 moveTo, float tweenDuration)
    {
        var moveSequence = DOTween.Sequence();
        moveSequence.AppendCallback(() =>
            {
                thisAudioSoruce.clip = engineStartUpClip;
                thisAudioSoruce.Play();
            })
            .AppendInterval(engineStartUpClip.length)
            .AppendCallback(() =>
            {
                thisAudioSoruce.clip = engineRunClip;
                thisAudioSoruce.Play();
            })
            .AppendInterval(engineRunClip.length)
            .AppendCallback(() =>
            {
                thisAudioSoruce.clip = engineSlowDownClip;
                thisAudioSoruce.Play();
            });

        return DOTween.Sequence().Join(moveSequence)
            .Join(transform.DOMove(moveTo, tweenDuration).SetRelative().SetEase(Ease.Linear));
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

    private void TutorialMachineDisappear()
    {
        transform.DOKill();
        
        thisAudioSoruce.clip = engineRunClip;

        this.Delay(2,
            () =>
            {
                thisAudioSoruce.Play();
                transform.DOMoveY(-2.5f, engineRunClip.length).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        thisAudioSoruce.Play();
                        transform.DOMoveZ(2.5f, engineRunClip.length).SetEase(Ease.Linear).OnComplete(() => gameObject.SetActive(false));
                    });
            });
    }
}