using DG.Tweening;
using UnityEngine.UI;
using UnityEngine;

public class InfographicsFadeSequence : MonoBehaviour
{
    [SerializeField] Image image1;
    [SerializeField] Image image2;
    [SerializeField] float crossFadeDuration = 1f;
    [SerializeField] float imageOnScreenDuration = 10;

    private void Start()
    {
        DoFadeSequence().SetLoops(-1);
    }

    private Sequence DoFadeSequence()
    {
        return DOTween.Sequence()
            .AppendInterval(imageOnScreenDuration)
            .Append(DoCrossFade(image1, image2))
            .AppendInterval(imageOnScreenDuration)
            .Append(DoCrossFade(image2, image1));
    }

    private Sequence DoCrossFade(Image fadeOut, Image fadeIn)
    {
        return DOTween.Sequence()
            .Join(fadeOut.DOFade(0, crossFadeDuration))
            .Join(fadeIn.DOFade(1, crossFadeDuration));
    }
    
    
}
