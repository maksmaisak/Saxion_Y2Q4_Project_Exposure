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
    [SerializeField] float chargeUpDuration = 1.6f;
    
    [Header("Audio Settings")]
    [SerializeField] AudioClip shootClip;
    [SerializeField] AudioClip chargeUpClip;
    [SerializeField] AudioClip interruptClip;
    [SerializeField] float shootVolume = 0.8f;
    [SerializeField] float chargeUpVolume = 0.6f;

    private bool canShoot = true;
    
    private AudioSource audioSource;

    private float lastChargeUpStartedTime;

    IEnumerator Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        Assert.IsNotNull(shootClip);
        Assert.IsNotNull(chargeUpClip);
        Assert.IsNotNull(interruptClip);

        yield return new WaitUntil(() => radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    public void SetIsUsable(bool isUsable)
    {
        if (canShoot && !isUsable)
        {
            StopAllCoroutines();

            if (audioSource)
            {
                audioSource.Stop();
                PlayInterruptIfNeeded();
            }
            lastChargeUpStartedTime = 0.0f;
        }
        
        canShoot = isUsable;
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        base.StartUsing(currentUsingObject);

        if (!canShoot)
            return;

        lastChargeUpStartedTime = Time.time;

        Assert.IsNotNull(audioSource);
        audioSource.clip = chargeUpClip;
        audioSource.volume = chargeUpVolume;
        audioSource.Play();
        
        // Maybe use DOTween and instead of chargeUpDuration use clip.length (however this is easier to change)
        this.Delay(chargeUpDuration, () =>
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

        if (!canShoot)
            return;

        PlayInterruptIfNeeded();
    }

    private void PlayInterruptIfNeeded()
    {
        float timeSinceChargeupStarted = Time.time - lastChargeUpStartedTime;
        if (timeSinceChargeupStarted >= chargeUpDuration) 
            return;
        
        StopAllCoroutines();
            
        Assert.IsNotNull(audioSource);
        audioSource.Stop();
        audioSource.clip = interruptClip;
        audioSource.volume = chargeUpVolume;
        audioSource.Play();
    }
}

