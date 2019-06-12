using System.Collections;
using System.Collections.Generic;
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
    [Space]
    [SerializeField] TutorialMachineOpen opening;
    [SerializeField] float controllerTutorialAppearDelay = 0.7f;
    [SerializeField] GameObject controllerTutorial;
    [SerializeField] GameObject handTutorial;

    [Header("Audio Settings")] 
    [SerializeField] AudioClip engineStartUpClip;
    [SerializeField] AudioClip engineRunClip;
    [SerializeField] AudioClip engineSlowDownClip;
    [SerializeField] AudioSource rotatingPartAudioSource;

    private RadarTool radarTool;
    private bool isTutorialRunning;

    private void Start()
    {
        Assert.IsNotNull(engineStartUpClip);
        Assert.IsNotNull(engineRunClip);
        Assert.IsNotNull(engineSlowDownClip);
        Assert.IsNotNull(rotatingPartAudioSource);
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
        EnsureIsInitializedCorrectly();

        radarController.isGrabbable = false;
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

        yield return new WaitForSeconds(controllerTutorialAppearDelay);

        handTutorial.SetActive(true);
        controllerTutorial.SetActive(true);
        //Destroy(gameObject);
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

    private void EnsureIsInitializedCorrectly()
    {
        Assert.IsNotNull(radarController);
        Assert.IsNotNull(rotateTransform);
        radarTool = radarController.GetComponent<RadarTool>();
        Assert.IsNotNull(radarTool);
    }

    private PulseSettings MakeOverridePulseSettings(PulseSettings settings)
    {
        if (overrideWavespherePrefab)
            settings.wavespherePrefab = overrideWavespherePrefab;

        settings.wavePulseSpeed = overridePulseSpeed;
        settings.wavesphereSpeedMin = settings.wavesphereSpeedMax = overrideWavesphereSpeed;

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
