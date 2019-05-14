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

    private SphereCollider sphereCollider;
    public RadarHighlightLocation highlightLocation { get; set; }

    private void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
        Assert.IsNotNull(sphereCollider);

        // Make sure the collider matches the scale as this script will be attached to a sphere.
        sphereCollider.radius = transform.localScale.x;
        
        // Set the rotation to face the camera position + some kind of sphere offset.
        var targetPosition = Camera.main.transform.position;
        targetPosition += Random.insideUnitSphere * targetSphereRadius;
        
        transform.rotation = Quaternion.LookRotation(targetPosition - transform.position);
        
        // Randomize scale over time
        float randomScale = scaleTarget * Mathf.Max(Random.value, scaleRandomMin);
        transform.DOScale(new Vector3(randomScale, randomScale, randomScale), scaleDuration);

        Destroy(gameObject, delayToDespawn);
    }

    private void Update() => transform.position += flyingSpeed * Time.deltaTime * transform.forward;

    private void OnTriggerEnter(Collider other)
    {
        if (handsCollisionLayer.ContainsLayer(other.gameObject.layer))
        {
            Debug.Log("Hand is hit");
            Destroy(gameObject);
            DotsManager.instance.Highlight(highlightLocation);
        }
    }
}
