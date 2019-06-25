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
    [SerializeField] Transform radarController;
    [SerializeField] float openCabinetDuration = 1.5f;
    [SerializeField] float slideOutDuration    = 1.5f;
    [SerializeField] float slideOutZOffset     = 0.3f;

    public Sequence Open()
    {
        Assert.IsNotNull(radarController);
        Assert.IsNotNull(leftCabinet);
        Assert.IsNotNull(rightCabinet);

        return DOTween.Sequence().SetTarget(radarController)
            .Join(leftCabinet.DORotate(new Vector3(0, -90.0f, 0), openCabinetDuration).SetEase(Ease.InOutQuart))
            .Join(rightCabinet.DORotate(new Vector3(0, 90.0f, 0), openCabinetDuration).SetEase(Ease.InOutQuart))
            .Append(radarController.DOLocalMoveZ(-slideOutZOffset, slideOutDuration).SetRelative()
                .SetEase(Ease.InOutQuad).SetUpdate(UpdateType.Late));
    }
}
