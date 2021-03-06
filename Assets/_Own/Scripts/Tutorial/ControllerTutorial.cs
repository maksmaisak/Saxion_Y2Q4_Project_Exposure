﻿using UnityEngine.Assertions;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using VRTK;

public class ControllerTutorial : MonoBehaviour
{
    [SerializeField] float rotationDuration = 1.5f;
    [SerializeField] float degreesToRotate = 90.0f;
    [SerializeField] Transform controllerToRotate;
    [SerializeField] SkinnedMeshRenderer trigger;
    [SerializeField] Material baseMaterial;
    [SerializeField] Material highlightMaterial;

    private Transform cameraTransform;
    private bool isRemoving = false;

    private RadarTool radarTool;

    IEnumerator Start()
    {
        Assert.IsNotNull(controllerToRotate);
        Assert.IsNotNull(baseMaterial);
        Assert.IsNotNull(highlightMaterial);
        Assert.IsNotNull(trigger);
        
        trigger.sharedMaterial = highlightMaterial;

        yield return new WaitUntil(() => cameraTransform = VRTK_DeviceFinder.HeadsetCamera());
        if (isRemoving)
        {
            Debug.Log("ControllerTutorial was removed before starting the appear tweens.");
            yield break;
        }
        transform.rotation = Quaternion.LookRotation(cameraTransform.position - transform.position);

        controllerToRotate
            .DOScale(0.0f, duration: 1.0f)
            .From()
            .SetEase(Ease.OutCirc);

        controllerToRotate.localRotation = Quaternion.Euler(0, degreesToRotate * 0.5f, 0);
        controllerToRotate
            .DOLocalRotate(new Vector3(0, -degreesToRotate, 0), rotationDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void RemoveWhenRadarPulses()
    {
        radarTool = GetRadarTool();
        if (!radarTool)
            return;
        
        radarTool.onPulse.AddListener(OnPulse);

        void OnPulse()
        {
            radarTool.onPulse.RemoveListener(OnPulse);
            Remove();
        }
    }

    private RadarTool GetRadarTool()
    {
        RadarTool tool = radarTool;
        
        if (tool)
            return tool;

        if (tool = GetComponentInParent<RadarTool>())
            return tool;
        
        if (tool = GetComponentInChildren<RadarTool>())
            return tool;
        
        if (tool = FindObjectOfType<RadarTool>())
            return tool;

        return null;
    }

    public void Remove()
    {
        isRemoving = true;
        
        controllerToRotate.DOKill();
        controllerToRotate
            .DOScale(0.0f, 1.0f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
