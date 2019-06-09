using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

public class OnTutorialEnd : MyBehaviour, IEventReceiver<OnTutorialEndEvent>
{
    [SerializeField] GameObject leftCabinet;
    [SerializeField] GameObject rightCabinet;
    [SerializeField] GameObject radarGun;
    [SerializeField] float openCabinetDuration = 1.5f;
    [SerializeField] float slideOutDuration    = 1.5f;
    [SerializeField] float slideOutZOffset     = 0.3f;

    private void Start()
    {
        Assert.IsNotNull(radarGun);
        Assert.IsNotNull(leftCabinet);
        Assert.IsNotNull(rightCabinet);
    }
    
    public void On(OnTutorialEndEvent tutorialEndEvent)
    {
        DOTween.Sequence()
            .Join(leftCabinet .transform.DORotate(new Vector3(0, -90.0f, 0), openCabinetDuration).SetEase(Ease.InOutQuart))
            .Join(rightCabinet.transform.DORotate(new Vector3(0,  90.0f, 0), openCabinetDuration).SetEase(Ease.InOutQuart))
            .Append(radarGun.transform.DOMoveZ(-slideOutZOffset, slideOutDuration).SetRelative().SetEase(Ease.InOutQuad));
    }
}
