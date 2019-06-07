using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VRTK;

public class Navpoint : VRTK_DestinationMarker
{
    [Header("Navpoint Settings")]
    [SerializeField] VRTK_BasicTeleport teleporter;
    [SerializeField] Transform rotateToFacePlayerTransform;
    [SerializeField] Transform teleportToTransform;
    [Space]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image outerCircle;
    [SerializeField] Image innerCircle;
    [Space] 
    [SerializeField] float fillDuration = 1.0f;
    [SerializeField] float unFillDuration = 10.0f;
    [Space]
    [FormerlySerializedAs("onComplete")] public UnityEvent onTeleport;

    [Header("Appear settings")]
    [Tooltip("The navpoint will only show up after this section is revealed. Defaults to the section it's parented to. If none, the navpoint is always visible.")]
    [SerializeField] LightSection lightSectionToRevealWith;
    [SerializeField] float appearDelay = 5.0f;

    [Header("Pulse Settings")] 
    [FormerlySerializedAs("navPointAudio")]
    [SerializeField] AudioClip pulseSound;
    [SerializeField] float pulseDuration = 0.8f;
    [SerializeField] float pulseInterval = 2.0f;
    [SerializeField] float pulsePunchScale = 0.4f;

    [Header("Teleport Audio")]
    [Tooltip("The audio source is situated inside the VR_Setup")]
    [SerializeField] AudioSource teleportAudioSource;
    [FormerlySerializedAs("teleportAudio")] [SerializeField] AudioClip   teleportClip;

    [Header("Debug")] 
    [SerializeField] bool teleportOnStart = false;

    private new Camera camera;
    private AudioSource audioSource;

    enum State
    {
        Unfilling,
        Filling,
        PlayingEffect
    }

    private State state = State.Unfilling;

    IEnumerator Start()
    {
        audioSource = GetComponent<AudioSource>();

        rotateToFacePlayerTransform = rotateToFacePlayerTransform ? rotateToFacePlayerTransform : transform;
        teleportToTransform = teleportToTransform ? teleportToTransform : transform;
        
        canvasGroup = canvasGroup ? canvasGroup : GetComponentInChildren<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);
        
        Assert.IsNotNull(outerCircle);
        Assert.IsNotNull(innerCircle);
        outerCircle.fillAmount = 0.0f;
        
        lightSectionToRevealWith = lightSectionToRevealWith ? lightSectionToRevealWith : GetComponentInParent<LightSection>();
        if (lightSectionToRevealWith && !lightSectionToRevealWith.isRevealed)
        {
            Hide();
            lightSectionToRevealWith.onReveal.AddListener(() => this.Delay(appearDelay, Show));
        }

        yield return new WaitUntil(() => camera = Camera.main);

        if (teleportOnStart)
            PlayDisappearEffect().OnComplete(Teleport);
    }
    
    void Update()
    {
        switch (state)
        {
            case State.Unfilling:
                outerCircle.fillAmount = Mathf.Clamp01(outerCircle.fillAmount - Time.deltaTime / unFillDuration);
                break;
            case State.Filling:
                float fill = Mathf.Clamp01(outerCircle.fillAmount + Time.deltaTime / fillDuration);
                outerCircle.fillAmount = fill;
                if (fill >= 1.0f)
                    PlayDisappearEffect().OnComplete(Teleport);
                break;
        }
    }

    void LateUpdate()
    {
        if (!camera)
            return;
        
        rotateToFacePlayerTransform.rotation = Quaternion.LookRotation(rotateToFacePlayerTransform.position - camera.transform.position);
    }

    public void SetFilling(bool isFilling)
    {
        if (state != State.Filling && state != State.Unfilling)
            return;

        state = isFilling ? State.Filling : State.Unfilling;
    }

    private void Hide()
    {
        GetComponentsInChildren<Collider>().Each(c => c.enabled = false);
        canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.0f;
    }
    
    private void Show()
    {
        GetComponentsInChildren<Collider>().Each(c => c.enabled = true);
        canvasGroup.interactable = true;

        canvasGroup.DOKill();
        canvasGroup.DOFade(1.0f, 1.0f);

        transform.DOKill();
        DOTween.Sequence()
            .SetTarget(this)
            .AppendCallback(() => {
                audioSource.clip = pulseSound;
                audioSource.loop = false;
                audioSource.Play();
            })
            .Append(transform.DOPunchScale(Vector3.one * pulsePunchScale, pulseDuration, 2))
            .AppendInterval(pulseInterval)
            .SetLoops(-1, LoopType.Restart);
    }

    private Sequence PlayDisappearEffect()
    {
        state = State.PlayingEffect;

        outerCircle.DOKill();
        innerCircle.DOKill();
        this.DOKill();

        const float PartDuration = 0.4f;

        return DOTween
            .Sequence()

            .Append(outerCircle.rectTransform.DOScale(1.2f, PartDuration))
            .Join(outerCircle.DOFade(0.0f, PartDuration).SetEase(Ease.OutQuart))

            .AppendCallback(() => outerCircle.transform.localScale = Vector3.one * 0.8f)

            .Join(outerCircle.transform.DOScale(1.0f, PartDuration))
            .Join(outerCircle.DOFade(1.0f, PartDuration))
            .Join(innerCircle.DOFade(0.0f, PartDuration))

            .Append(outerCircle.rectTransform.DOScale(1.2f, PartDuration))
            .Join(outerCircle.DOFade(0.0f, PartDuration).SetEase(Ease.OutQuart));
    }

    [ContextMenu("Teleport")]
    private void Teleport()
    {
        teleportAudioSource.PlayOneShot(teleportClip);

        this.Delay(0.1f, () =>
        {
            Assert.IsTrue(EnsureTeleporter());
            teleporter.Teleport(transform, teleportToTransform.position, teleportToTransform.rotation);

            new OnTeleportEvent(this).SetDeliveryType(MessageDeliveryType.Immediate).PostEvent();
            onTeleport?.Invoke();

            Destroy(gameObject);
        });
    }

    private bool EnsureTeleporter()
    {
        if (teleporter)
            return true;

        if (VRTK_ObjectCache.registeredTeleporters.Count == 0)
            return false;

        teleporter = VRTK_ObjectCache.registeredTeleporters[0];
        return true;
    }
}