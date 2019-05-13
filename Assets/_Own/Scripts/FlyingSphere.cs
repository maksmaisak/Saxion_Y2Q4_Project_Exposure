using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

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
    [SerializeField] float delayToRevealDots = 2.0f;
    [SerializeField] float targetSphereRadius = 0.25f;

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
        
        // Randomize scale over time, also adjust the collider radius.
        float randomScale = scaleTarget * Mathf.Max(Random.value, scaleRandomMin);
        transform.DOScale(new Vector3(randomScale, randomScale, randomScale), scaleDuration);

        DOTween.To( 
            () => sphereCollider.radius, 
            x => sphereCollider.radius = x, 
            randomScale, 
            scaleDuration);
        
        // Despawn it after some time and reveal the position spawned from. (TODO: this will be moved to when it hits the actual hands)
        this.Delay(delayToDespawn, () => { Destroy(gameObject); });
        this.Delay(delayToRevealDots, () => { DotsManager.instance.Highlight(highlightLocation); });
    }

    private void Update() => transform.position += flyingSpeed * Time.deltaTime * transform.forward;
}
