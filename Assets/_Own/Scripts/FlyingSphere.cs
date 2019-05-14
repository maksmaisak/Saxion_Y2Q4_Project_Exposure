using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class FlyingSphere : MonoBehaviour
{
    [Header("Movement Settings")] 
    [SerializeField] float flyingSpeed = 1.5f;

    [Header("Scaling Settings")]
    [SerializeField] float scaleTarget = 0.8f;
    [SerializeField] float scaleDuration = 0.7f;
    [SerializeField] float scaleRandomMin = 0.4f;

    [Header("Other Settings")]
    [SerializeField] float delayToDespawn = 5.0f;
    [SerializeField] float targetSphereRadius = 0.25f;
    [SerializeField] LayerMask handsCollisionLayer;

    private bool didStart;
    private Vector3? targetCenter;

    public RadarHighlightLocation highlightLocation { get; set; }
    
    public void SetTarget(Vector3 position)
    {
        Assert.IsFalse(didStart, "Can't SetTarget of a wavesphere after it started moving.");
        
        targetCenter = position;
    }
    
    private void Start()
    {
        didStart = true;
        
        // Set the rotation to face the camera position + some kind of sphere offset.
        Vector3 targetPosition = targetCenter ?? Camera.main.transform.position;
        targetPosition += Random.insideUnitSphere * targetSphereRadius;
        
        // Rotate the along movement direction
        transform.rotation = Quaternion.LookRotation(targetPosition - transform.position);
        
        // Randomize scale over time
        float randomScale = scaleTarget * Mathf.Max(Random.value, scaleRandomMin);
        transform.DOScale(randomScale, scaleDuration);

        Destroy(gameObject, delayToDespawn);
    }

    private void Update() => transform.position += flyingSpeed * Time.deltaTime * transform.forward;

    private void OnTriggerEnter(Collider other)
    {
        if (!handsCollisionLayer.ContainsLayer(other.gameObject.layer)) 
            return;
        
        Debug.Log("Hand is hit");
        Destroy(gameObject);
        DotsManager.instance.Highlight(highlightLocation);
    }
}
