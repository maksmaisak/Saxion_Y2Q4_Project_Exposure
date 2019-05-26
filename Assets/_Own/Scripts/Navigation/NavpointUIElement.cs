using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class NavpointUIElement : MonoBehaviour
{
    [SerializeField] Image outerCircle;
    [SerializeField] Image innerCircle;
    [Space] 
    [SerializeField] float fillDuration = 1.0f;

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
        Assert.IsNotNull(outerCircle);
        Assert.IsNotNull(innerCircle);

        outerCircle.fillAmount = 0.0f;
        
        yield return new WaitUntil(() => camera = Camera.main);
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
                    PlayEffect();
                break;
        }
    }

    private void PlayEffect()
    {
        state = State.PlayingEffect;

        outerCircle.DOKill();
        innerCircle.DOKill();

        const float PartDuration = 0.4f;

        DOTween
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

    public void SetFilling(bool isFilling)
    {
        if (state != State.Filling && state != State.Unfilling)
            return;

        state = isFilling ? State.Filling : State.Unfilling;
    }
}