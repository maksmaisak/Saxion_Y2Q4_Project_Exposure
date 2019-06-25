using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine;

public class Infographics : MyBehaviour, IEventReceiver<OnTeleportEvent>, IEventReceiver<OnRevealEvent>
{
    [SerializeField] Image[] images;
    [SerializeField] float crossFadeDuration = 1.0f;
    [SerializeField] float imageOnScreenDuration = 2.0f;
    [SerializeField] float showDuration = 0.5f;
    [SerializeField] float hideDuration = 0.5f;
    [Space]
    [SerializeField] bool startActive = false;
    
    [SerializeField] List<Navpoint> appearAt;
    [SerializeField] float showDelayAfterTeleport = 2.0f;

    private Vector3 originalScale = Vector3.one;

    protected override void Awake()
    {
        base.Awake();

        originalScale = transform.localScale;

        if (!startActive)
            transform.localScale = Vector3.zero;
        else
            Show();
    }
    
    public void On(OnTeleportEvent message)
    {
        if (appearAt.Contains(message.navpoint))
            this.Delay(showDelayAfterTeleport, Show);
    }

    public void On(OnRevealEvent message)
    {
        Hide();
    }

    public void Show()
    {
        Transform tf = transform;
        tf.DOKill();
        tf.localScale = Vector3.zero;
        tf
            .DOScale(originalScale, showDuration)
            .SetEase(Ease.OutBack);

        this.DOKill();
        DoFadeSequence().SetLoops(-1);
    }

    public void Hide()
    {
        Transform tf = transform;
        tf.DOKill();
        tf
            .DOScale(Vector3.zero, hideDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => this.DOKill());
    }

    private Sequence DoFadeSequence()
    {
        Sequence sequence = DOTween.Sequence().SetTarget(this);

        for (int i = 0; i < images.Length; ++i)
        {
            sequence
                .AppendInterval(imageOnScreenDuration)
                .Append(DoCrossFade(images[i], images[(i + 1) % images.Length]));
        }

        return sequence;
    }

    private Sequence DoCrossFade(Image fadeOut, Image fadeIn)
    {
        return DOTween.Sequence()
            .Join(fadeOut.DOFade(0.0f, crossFadeDuration))
            .Join(fadeIn .DOFade(1.0f, crossFadeDuration));
    }
}
