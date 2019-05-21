using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VRTK;

public class RadarController : VRTK_InteractableObject
{
    [Header("Radar Controller")]
    [SerializeField] RadarTool radarTool;
    [SerializeField] float fireCooldown = 1.0f;

    AudioSource audioSource;

    [Header("Sound Settings")]
    [SerializeField] AudioClip shootClip;
    [SerializeField] float volume = 1.0f;
    
    private bool canShoot = true;

    IEnumerator Start()
    {
        audioSource = GetComponent<AudioSource>();

        yield return new WaitUntil(() =>
                radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        audioSource.clip = shootClip;
        audioSource.volume = volume;
        audioSource.Play();

        base.StartUsing(currentUsingObject);

        if (!canShoot)
            return;
        
        canShoot = false;
        
        radarTool.Probe();

        this.Delay(fireCooldown, () => canShoot = true);
    }
}

