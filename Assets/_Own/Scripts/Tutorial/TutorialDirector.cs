using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

public class TutorialDirector : MonoBehaviour
{
    [SerializeField] RadarController radarController;
    [SerializeField] Transform rotateTransform;
    [SerializeField] FlyingSphere overrideWavespherePrefab;
    [SerializeField] float overridePulseSpeed = 1.0f;
    [SerializeField] float overrideWavesphereSpeed = 1.0f;
    [SerializeField] GameObject tutorialController;

    private RadarTool radarTool;
    private bool isTutorialRunning;

    public void StartTutorial()
    {
        if (isTutorialRunning) 
            return;
        
        isTutorialRunning = true;
        StartCoroutine(StartTutorialCoroutine());
    }

    IEnumerator StartTutorialCoroutine()
    {
        EnsureIsInitializedCorrectly();

        radarController.isGrabbable = false;

        yield return new WaitForSeconds(2.0f);

        yield return rotateTransform
            .DORotate(Vector3.up * 90.0f, 5.0f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
        
        PulseSettings oldPulseSettings = radarTool.GetPulseSettings();
        radarTool.SetPulseSettings(MakeOverridePulseSettings(oldPulseSettings));

        radarController.StartUsing();
        yield return new WaitForSeconds(radarController.GetChargeupDuration() + 0.1f);
        radarController.StopUsing();
        yield return WaitUntilAllSpawnedWavespheresAreCaught();

        yield return rotateTransform
            .DORotate(Vector3.up * -90.0f, 5.0f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
        
        radarController.StartUsing();
        yield return new WaitForSeconds(radarController.GetChargeupDuration() + 0.1f);
        radarController.StopUsing();
        yield return WaitUntilAllSpawnedWavespheresAreCaught();

        yield return rotateTransform
            .DORotate(Vector3.zero, 5.0f)
            .WaitForCompletion();

        radarTool.SetPulseSettings(oldPulseSettings);
        radarController.isGrabbable = true;
        radarController.transform.parent = null;
        
        tutorialController.SetActive(true);
        
        //Destroy(gameObject);
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
