using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class RadarController : VRTK_InteractableObject
{
    [Header("Radar Controller")]
    [SerializeField] RadarTool radarTool;

    [Header("Audio Settings")]
    [SerializeField] AudioClip shootClip;
    [SerializeField] AudioClip chargeUpClip;
    [SerializeField] AudioClip interruptClip;
    [SerializeField] float shootVolume = 0.8f;
    [SerializeField] float chargeUpVolume = 0.6f;
    
    private AudioSource audioSource;

    private float lastChargeUpStartedTime;

    IEnumerator Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        Assert.IsNotNull(shootClip);
        Assert.IsNotNull(chargeUpClip);
        Assert.IsNotNull(interruptClip);

        yield return new WaitUntil(() =>
                radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        base.StartUsing(currentUsingObject);

        lastChargeUpStartedTime = Time.time;

        audioSource.clip = chargeUpClip;
        audioSource.volume = chargeUpVolume;
        audioSource.Play();
        
        // Maybe use DOTween
        this.Delay(chargeUpClip.length - 0.2f, () =>
        {
            audioSource.Stop();
            audioSource.clip = shootClip;
            audioSource.volume = shootVolume;
            audioSource.Play(); 
            
            radarTool.Probe();
        });
    }

    public override void StopUsing(VRTK_InteractUse previousUsingObject = null, bool resetUsingObjectState = true)
    {
        base.StopUsing(previousUsingObject, resetUsingObjectState);

        if (Time.time - lastChargeUpStartedTime < chargeUpClip.length - 0.2f)
        {
            StopAllCoroutines();
            
            audioSource.Stop();
            audioSource.clip = interruptClip;
            audioSource.volume = chargeUpVolume;
            audioSource.Play();
        }
    }
}

