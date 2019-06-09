using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class TutorialMachineOpen
{
    [SerializeField] Transform leftCabinet;
    [SerializeField] Transform rightCabinet;
    [SerializeField] Transform radarGun;
    [SerializeField] float openCabinetDuration = 1.5f;
    [SerializeField] float slideOutDuration    = 1.5f;
    [SerializeField] float slideOutZOffset     = 0.3f;

    public Sequence Open()
    {
        Assert.IsNotNull(radarGun);
        Assert.IsNotNull(leftCabinet);
        Assert.IsNotNull(rightCabinet);
        
        return DOTween.Sequence()
            .Join(leftCabinet .DORotate(new Vector3(0, -90.0f, 0), openCabinetDuration).SetEase(Ease.InOutQuart))
            .Join(rightCabinet.DORotate(new Vector3(0,  90.0f, 0), openCabinetDuration).SetEase(Ease.InOutQuart))
            .Append(radarGun.DOMoveZ(-slideOutZOffset, slideOutDuration).SetRelative().SetEase(Ease.InOutQuad));
    }
}
