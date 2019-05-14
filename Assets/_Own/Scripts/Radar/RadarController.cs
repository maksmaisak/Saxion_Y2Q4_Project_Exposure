using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

public class RadarController : VRTK.VRTK_InteractableObject
{
    [Header("Radar Settings")]
    [SerializeField] GameObject wavePulsePrefab;

    public override void StartUsing(VRTK.VRTK_InteractUse usingObject)
    {
        base.StartUsing(usingObject);
        Debug.Log("Radar fired!");

        CreateWavePulse();
    }

    private void CreateWavePulse()
    {
        Assert.IsNotNull(wavePulsePrefab);

        const float duration = 2.0f;

        GameObject pulse = Instantiate(wavePulsePrefab, transform.position, Quaternion.identity);
        Transform tf = pulse.transform;

        tf.localScale = Vector3.zero;
        tf.DOScale(20, duration).SetEase(Ease.Linear);

        Destroy(pulse, duration);
    }
}

