using DG.Tweening;
using UnityEngine;

public class HandTutorial : Singleton<HandTutorial>
{
    void Start()
    {
        transform
            .DOScale(0.2f, 1.0f)
            .SetEase(Ease.OutCirc);

        transform
            .DOPunchPosition(new Vector3(0, transform.localPosition.y, 0) * -0.05f, 1, 1)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }


    public void Remove() => transform
     .DOScale(0.0f, 1.0f)
     .SetEase(Ease.InBack)
     .OnComplete(() => Destroy(gameObject));
}
