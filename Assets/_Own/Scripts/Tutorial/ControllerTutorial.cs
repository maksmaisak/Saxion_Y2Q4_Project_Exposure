using UnityEngine.Assertions;
using UnityEngine;
using DG.Tweening;

public class ControllerTutorial : MonoBehaviour
{
    [SerializeField] float rotationDuration = 10.0f;
    [SerializeField] SkinnedMeshRenderer trigger;
    [SerializeField] Material baseMaterial;
    [SerializeField] Material highlightMaterial;

    private void Start()
    {
        Assert.IsNotNull(baseMaterial);
        Assert.IsNotNull(highlightMaterial);
        Assert.IsNotNull(trigger);
        
        trigger.sharedMaterial = highlightMaterial;
        
        transform
            .DOScale(0.0f, 1.0f)
            .From()
            .SetEase(Ease.OutCirc);
        
        transform.rotation = Quaternion.identity;
        transform
            .DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }
    
    public void Remove()
    { 
        transform.DOKill();
        transform
            .DOScale(0.0f, 1.0f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
        
    }
}
