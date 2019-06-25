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
    [SerializeField] private List<Navpoint> appearAt;

    private Vector3 originalScale = Vector3.one;

    protected override void Awake()
    {
        base.Awake();
        
        if (!startActive) 
            this.DoNextFrame(() => gameObject.SetActive(false));

        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        this.DOKill();
        DoFadeSequence().SetLoops(-1);
    }
    
    public void On(OnTeleportEvent message)
    {
        if (appearAt.Contains(message.navpoint))
            Show();
    }

    public void On(OnRevealEvent message)
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        Transform tf = transform;
        tf.DOKill();
        tf.localScale = Vector3.zero;
        tf
            .DOScale(originalScale, showDuration)
            .SetEase(Ease.OutBack);
    }

    public void Hide()
    {
        Transform tf = transform;
        tf.DOKill();
        tf
            .DOScale(Vector3.zero, hideDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
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
