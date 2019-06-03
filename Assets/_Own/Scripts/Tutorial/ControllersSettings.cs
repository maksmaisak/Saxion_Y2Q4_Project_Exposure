using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ControllersSettings : Singleton<ControllersSettings>
{
    [SerializeField] private SkinnedMeshRenderer gripGameObject;
    [SerializeField] private SkinnedMeshRenderer triggerGameObject;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private Material highlightMaterial;

    public void ApplyHighlightToObject()
    {
        triggerGameObject.material = highlightMaterial;
        ChangeHighlightMaterial();
        ActivateAnimation();
    }

    public void DeleteGameObject() => transform.DOScale(0,2).OnComplete(() => Destroy(gameObject));

    private void ChangeHighlightMaterial() => gripGameObject.material = baseMaterial;

    private void ActivateAnimation() => GetComponent<Animator>().enabled = true;
    
    
}
