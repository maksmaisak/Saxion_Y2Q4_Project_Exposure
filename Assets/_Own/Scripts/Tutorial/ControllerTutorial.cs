using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

// TODO don't make it a singleton jeez. Make it react to events instead. Or something.
public class ControllerTutorial : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer trigger;
    [SerializeField] Material baseMaterial;
    [SerializeField] Material highlightMaterial;
    [SerializeField] float rotationDuration = 10.0f;

    void Start()
    {
        Assert.IsNotNull(baseMaterial);
        Assert.IsNotNull(highlightMaterial);
        Assert.IsNotNull(trigger);

        trigger.sharedMaterial = highlightMaterial;
        
        transform
            .DOScale(0.0f, 1.0f)
            .From()
            .SetEase(Ease.OutCirc);
        
        transform.rotation = Quaternion.identity;
        transform
            .DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    public void Remove()
    {
        transform.DOKill();
        transform
            .DOScale(0.0f, 1.0f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
