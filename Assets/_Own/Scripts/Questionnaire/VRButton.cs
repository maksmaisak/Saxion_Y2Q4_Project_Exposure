using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using VRTK;

public class VRButton : MonoBehaviour
{
    public UnityEvent onActivate;

    private bool canBeTouched = true;
    private bool startHidden = true;

    private Transform lookAtTransform;

    IEnumerator Start()
    {
        if (startHidden)
        {
            transform.localScale = Vector3.zero;
            canBeTouched = false;
        }
        
        yield return new WaitUntil(() =>
        {
            var camera = Camera.main;
            if (camera) 
                lookAtTransform = camera.transform;

            return lookAtTransform;
        });
    }

    void LateUpdate()
    {
        if (lookAtTransform)
            transform.LookAt(lookAtTransform);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!canBeTouched)
            return;
        
        if (!VRTK_PlayerObject.IsPlayerObject(other.gameObject, VRTK_PlayerObject.ObjectTypes.Controller))
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
