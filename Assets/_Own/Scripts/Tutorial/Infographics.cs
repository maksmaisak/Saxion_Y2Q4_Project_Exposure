using DG.Tweening;
using UnityEngine.UI;
using UnityEngine;

public class Infographics : MonoBehaviour
{
    [SerializeField] Image[] images;
    [SerializeField] float crossFadeDuration = 1.0f;
    [SerializeField] float imageOnScreenDuration = 2.0f;
    
    void Start()
    {
        DoFadeSequence().SetLoops(-1);
    }

    private Sequence DoFadeSequence()
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < images.Length; ++i)
        {
            sequence
                .AppendInterval(imageOnScreenDuration)
                .Append(DoCrossFade(images[i], images[(i + 1) % images.Length]));
        }

        return sequence;
    }

    private Sequence DoCrossFade(Image fadeOut, Image fadeIn)
    {
        return DOTween.Sequence()
            .Join(fadeOut.DOFade(0, crossFadeDuration))
            .Join(fadeIn.DOFade(1, crossFadeDuration));
    }
}
