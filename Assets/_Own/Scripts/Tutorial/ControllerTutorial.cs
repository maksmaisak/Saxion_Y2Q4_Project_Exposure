using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

// TODO don't make it a singleton jeez. Make it react to events instead. Or something.
public class ControllerTutorial : Singleton<ControllerTutorial>
{
    [SerializeField] SkinnedMeshRenderer grip;
    [SerializeField] SkinnedMeshRenderer trigger;
    [SerializeField] Material baseMaterial;
    [SerializeField] Material highlightMaterial;
    [SerializeField] float rotationDuration = 10.0f;

    void Start()
    {
        Assert.IsNotNull(baseMaterial);
        Assert.IsNotNull(highlightMaterial);
        
        transform
            .DOScale(0.2f, 1.0f)
            .SetEase(Ease.OutCirc);
        
        transform
            .DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

        var radarController = FindObjectOfType<RadarController>();
        if (radarController && radarController.IsGrabbed())
            HighlightTrigger();
    }

    public void HighlightTrigger()
    {
        if (trigger)
            trigger.sharedMaterial = highlightMaterial;
        
        if (grip) 
            grip.sharedMaterial = baseMaterial;
    }

    public void Remove() => transform
        .DOScale(0.0f, 1.0f)
        .SetEase(Ease.InBack)
        .OnComplete(() => Destroy(gameObject));
}
