using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]
public class QuestionnaireButton : MonoBehaviour
{
    public UnityEvent onActivate;

    private bool canBeTouched = true;
    private bool startHidden = true;

    void Start()
    {
        if (startHidden)
        {
            transform.localScale = Vector3.zero;
            canBeTouched = false;
        }

        GetComponent<VRTK_InteractableObject>().InteractableObjectTouched += OnInteractableObjectTouched;
    }

    void OnInteractableObjectTouched(object sender, InteractableObjectEventArgs e)
    {
        if (!canBeTouched)
            return;
        
        canBeTouched = false;
        
        onActivate?.Invoke();

        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        
        transform.DOKill();
        transform
            .DOScale(1.0f, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => canBeTouched = true);
    }

    public void Hide()
    {
        canBeTouched = false;
        
        transform.DOKill();
        transform
            .DOScale(0.0f, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
