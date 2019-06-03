﻿using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

public class TutorialDirector : MonoBehaviour
{
    [SerializeField] RadarController radarController;
    [SerializeField] FlyingSphere overrideWavespherePrefab;
    [SerializeField] float overridePulseSpeed = 1.0f;
    [SerializeField] float overrideWavesphereSpeed = 1.0f;

    private RadarTool radarTool;

    IEnumerator Start()
    {
        EnsureIsInitializedCorrectly();

        radarController.isGrabbable = false;

        yield return new WaitForSeconds(2.0f);

        yield return radarController.transform
            .DORotate(Vector3.up * 90.0f, 5.0f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
        
        PulseSettings oldPulseSettings = radarTool.GetPulseSettings();
        radarTool.SetPulseSettings(MakeOverridePulseSettings(oldPulseSettings));
        
        radarController.StartUsing();
        yield return new WaitForSeconds(2.0f);
        radarController.StopUsing();
        
        yield return radarController.transform
            .DORotate(Vector3.up * -90.0f, 5.0f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
        
        radarController.StartUsing();
        yield return new WaitForSeconds(2.0f);
        radarController.StopUsing();

        yield return radarController.transform
            .DORotate(Vector3.zero, 5.0f)
            .WaitForCompletion();

        radarTool.SetPulseSettings(oldPulseSettings);
        radarController.isGrabbable = true;
    }

    private void EnsureIsInitializedCorrectly()
    {
        Assert.IsNotNull(radarController);
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
}
