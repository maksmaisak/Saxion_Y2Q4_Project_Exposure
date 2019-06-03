using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.Events;
using VRTK;

public class NavpointUIElement : VRTK_DestinationMarker
{
    [Header("Navpoint Settings")]
    [SerializeField] VRTK_BasicTeleport teleporter;
    public UnityEvent onComplete;

    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image outerCircle;
    [SerializeField] Image innerCircle;
    [Space] 
    [SerializeField] float fillDuration = 1.0f;

    [Tooltip("The navpoint will only show up after this section is revealed. Defaults to the section it's parented to. If none, the navpoint is always visible.")]
    [SerializeField] LightSection lightSectionToRevealWith;

    [Header("Debug")] 
    [SerializeField] bool teleportOnStart = false;
    
    private new Camera camera;

    enum State
    {
        Unfilling,
        Filling,
        PlayingEffect
    }

    private State state = State.Unfilling;

    IEnumerator Start()
    {
        canvasGroup = canvasGroup ? canvasGroup : GetComponentInChildren<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);
        
        Assert.IsNotNull(outerCircle);
        Assert.IsNotNull(innerCircle);
        outerCircle.fillAmount = 0.0f;
        
        lightSectionToRevealWith = lightSectionToRevealWith ? lightSectionToRevealWith : GetComponentInParent<LightSection>();
        if (lightSectionToRevealWith && !lightSectionToRevealWith.isRevealed)
        {
            Hide();
            lightSectionToRevealWith.onReveal.AddListener(() => this.Delay(5.0f, Show));
        }

        yield return new WaitUntil(() => camera = Camera.main);

        if (teleportOnStart)
            PlayDisappearEffect().OnComplete(Teleport);
    }
    
    void Update()
    {
        if (!camera)
            return;
        
        transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        
        switch (state)
        {
            case State.Unfilling:
                outerCircle.fillAmount = Mathf.Clamp01(outerCircle.fillAmount - Time.deltaTime / fillDuration);
                break;
            case State.Filling:
                float fill = Mathf.Clamp01(outerCircle.fillAmount + Time.deltaTime / fillDuration);
                outerCircle.fillAmount = fill;
                if (fill >= 1.0f)
                    PlayDisappearEffect().OnComplete(Teleport);
                break;
        }
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
        transform.DOPunchScale(Vector3.one * 1.1f, 1.0f, 2);
    }

    private Sequence PlayDisappearEffect()
    {
        state = State.PlayingEffect;

        outerCircle.DOKill();
        innerCircle.DOKill();

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
        Assert.IsTrue(EnsureTeleporter());
        Transform tf = transform;
        teleporter.Teleport(tf, tf.position);

        new OnTeleportEvent(this).SetDeliveryType(MessageDeliveryType.Immediate).PostEvent();
            
        onComplete?.Invoke();
        Destroy(gameObject);
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