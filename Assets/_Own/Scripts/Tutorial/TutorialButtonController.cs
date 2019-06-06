using DG.Tweening;
using UnityEngine;
using VRTK.Controllables;

[RequireComponent(typeof(TutorialDirector))]
public class TutorialButtonController : MonoBehaviour
{
    [SerializeField] VRTK_BaseControllable controllable;
    [SerializeField] Transform handTransform;
    [SerializeField] Transform buttonTransform;

    [SerializeField] float timeToDisapear = 0.5f;
    
    private TutorialDirector tutorialDirector;
    
    private void Start()
    {
        tutorialDirector = GetComponent<TutorialDirector>();
        handTransform
            .DOPunchPosition(new Vector3(0, handTransform.localScale.y, 0) * -0.05f, 1, 1)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    protected virtual void OnEnable()
    {
        controllable = (controllable == null ? GetComponent<VRTK_BaseControllable>() : controllable);
        controllable.MaxLimitReached += MaxLimitReached;
    }

    protected virtual void MaxLimitReached(object sender, ControllableEventArgs e)
    {
        handTransform.DOKill(true);
        handTransform.DOScale(0, timeToDisapear).SetEase(Ease.InBack).OnComplete(() => Destroy(handTransform.gameObject));

        this.Delay(1.0f, () => buttonTransform.DOScale(0, timeToDisapear).SetEase(Ease.InBack).OnComplete(() => Destroy(buttonTransform.gameObject)));
        
        tutorialDirector.StartTutorial();
    }
}