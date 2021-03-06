﻿using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using VRTK.Controllables;

[RequireComponent(typeof(TutorialDirector))]
public class TutorialButtonController : MonoBehaviour
{
    [SerializeField] VRTK_BaseControllable controllable;
    [SerializeField] Transform handTransform;
    [SerializeField] Transform buttonTransform;

    [FormerlySerializedAs("timeToDisapear")] [SerializeField] float timeToDisappear = 0.5f;
    
    private TutorialDirector tutorialDirector;
    
    private bool wasPressed;

    private void Start()
    {
        tutorialDirector = GetComponent<TutorialDirector>();
        handTransform
            .DOPunchPosition(new Vector3(0, handTransform.localScale.y, 0) * -0.05f, 1, 1)
            .SetLoops(-1, LoopType.Restart);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Press();
        }
    }

    private void OnEnable()
    {
        controllable = (controllable == null ? GetComponent<VRTK_BaseControllable>() : controllable);
        controllable.MaxLimitReached += (sender, args) => Press();
    }

    private void Press()
    {
        if (wasPressed)
            return;

        wasPressed = true;
        
        handTransform.DOKill(true);
        handTransform.DOScale(0, timeToDisappear).SetEase(Ease.InBack).OnComplete(() => Destroy(handTransform.gameObject));

        this.Delay(1.0f, () => buttonTransform.DOScale(0, timeToDisappear).SetEase(Ease.InBack).OnComplete(() => Destroy(buttonTransform.gameObject)));
        
        tutorialDirector.StartTutorial();
        
        controllable.GetComponent<AudioSource>()?.Play();
    }
}