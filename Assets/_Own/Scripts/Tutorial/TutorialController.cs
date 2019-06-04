using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TutorialController : Singleton<TutorialController>
{
    [SerializeField] private SkinnedMeshRenderer gripGameObject;
    [SerializeField] private SkinnedMeshRenderer triggerGameObject;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private Material highlightMaterial;

    private void Start()
    {
        transform.DOScale(0.3f, 1).SetEase(Ease.OutCirc);
    }

    public void ApplyHighlightToObject()
    {
        if (triggerGameObject == null || gripGameObject == null)
            return;

        triggerGameObject.material = highlightMaterial;
        ChangeHighlightMaterial();
    }

    public void DeleteGameObject() => transform.DOScale(0,1).SetEase(Ease.InCirc).OnComplete(() => Destroy(gameObject));

    private void ChangeHighlightMaterial() => gripGameObject.material = baseMaterial;

}
