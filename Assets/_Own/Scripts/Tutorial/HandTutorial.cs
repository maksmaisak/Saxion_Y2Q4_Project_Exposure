using DG.Tweening;
using UnityEngine;

public class HandTutorial : MonoBehaviour
{
    void Start()
    {
        transform
            .DOScale(0.0f, 1.0f)
            .From()
            .SetEase(Ease.OutCirc);

        transform
            .DOPunchPosition(new Vector3(0, transform.localPosition.y, 0) * -0.05f, 1, 1)
            .SetLoops(-1, LoopType.Restart);

        var radarController = FindObjectOfType<RadarController>();
        if (radarController && radarController.IsGrabbed())
            Remove();
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
