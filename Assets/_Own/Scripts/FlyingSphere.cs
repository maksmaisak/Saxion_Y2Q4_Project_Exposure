using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;
using Random = UnityEngine.Random;

public class FlyingSphere : MonoBehaviour
{
    [Header("Movement Settings")] 
    [SerializeField] float angularSpeed = 1.0f;
    [SerializeField] float attractionRadius = 1.5f;
    [SerializeField] float randomMinSpeed = 0.8f;
    [SerializeField] float randomMaxSpeed = 2.5f;
    [SerializeField] float targetSphereRadius = 0.25f;
    [Tooltip("If the wavesphere is spawned closer than this to the target, it will be slower.")]
    [SerializeField] float slowdownRadius = 4.0f;

    [Header("Scaling Settings")]
    [SerializeField] float scaleTarget = 0.8f;
    [SerializeField] float scaleDuration = 0.7f;
    [SerializeField] float scaleRandomMin = 0.4f;

    [Header("Color Settings")]
    [SerializeField] string albedoColorId = "_AlbedoColor_549AC39B";
    [SerializeField] string emissionColorId = "_EmissionColor_40E9251C";
    [SerializeField] List<Color> albedoColors = new List<Color>();
    [SerializeField] List<Color> emissionColors = new List<Color>();

    [Header("Vibration Settings")]
    [SerializeField] int vibrationDuration = 40;
    [SerializeField] int frequency = 2;
    [SerializeField] int strength = 100;

    [Header("Other Settings")]
    [SerializeField] AudioClip grabAudio;
    [SerializeField] AudioClip movingSound;
    [SerializeField] AudioClip spawnAudio;
    [SerializeField] float delayToDespawn = 20.0f;
    [SerializeField] LayerMask handsCollisionLayer;

    private new Transform transform;
    private float speed;

    private bool didStart;
    private bool canMove;

    private AudioSource audioSource;
    
    private Vector3? targetCenter;

    private List<Transform> targetTransforms = new List<Transform>();

    public RadarHighlightLocation highlightLocation { get; set; }
    
    public void SetTarget(Vector3 position)
    {
        Assert.IsFalse(didStart, "Can't SetTarget of a wavesphere after it started moving.");
        
        targetCenter = position;
    }

    void Awake()
    {
        transform = GetComponent<Transform>();
        audioSource = GetComponent<AudioSource>();
        
        targetTransforms.Add(Camera.main.gameObject.transform);

        if (VRTK_SDKManager.GetLoadedSDKSetup() == null)
            return;
            
        targetTransforms.Add(VRTK_DeviceFinder.GetControllerLeftHand().transform);
        targetTransforms.Add(VRTK_DeviceFinder.GetControllerRightHand().transform);
    }
    
    void Start()
    {
        didStart = true;
        canMove = true;

        RandomizeColor();
        RandomizeScale();
        RandomizeSpeedAndDirection();

        audioSource.PlayOneShot(spawnAudio);

        audioSource.clip = movingSound;
        audioSource.loop = true;
        audioSource.Play();

        Destroy(gameObject, delayToDespawn);
    }

    void Update()
    {
        if (!canMove)
            return;

        transform.position += speed * Time.deltaTime * transform.forward;

        if (targetTransforms.Count == 0)
            return;
        
        Transform targetTransform = targetTransforms.ArgMin(x => (x.transform.position - transform.position).sqrMagnitude);
        
        Vector3 targetDir = targetTransform.position - transform.position;

        if (targetDir.sqrMagnitude > attractionRadius * attractionRadius)
            return;
        
        Vector3 newDir =
            Vector3.RotateTowards(transform.forward, targetDir, 
                angularSpeed * Time.deltaTime, 0.0f);

        transform.rotation = Quaternion.LookRotation(newDir);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!handsCollisionLayer.ContainsLayer(other.gameObject.layer)) 
            return;

        GameObject otherController = other.gameObject.GetComponentInParent<VRTK_Pointer>()?.gameObject;

        if (otherController)
        {
            bool isLeftHand = VRTK_DeviceFinder.IsControllerLeftHand(otherController);

            VRTK_ControllerReference controllerReference = isLeftHand
                ? VRTK_DeviceFinder.GetControllerReferenceLeftHand()
                : VRTK_DeviceFinder.GetControllerReferenceRightHand();

            VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, grabAudio);
        }

        audioSource.PlayOneShot(grabAudio);

        canMove = false;

        const float Duration = 0.2f;
        
        transform.DOKill();

        transform.DOScale(0.0f, Duration)
            .SetEase(Ease.OutQuart);
        
        Destroy(gameObject, Mathf.Max(grabAudio.length, Duration));

        Vector3 otherPosition = other.transform.position;
        transform.DOLookAt(otherPosition - transform.position, Duration)
            .SetEase(Ease.OutQuart);

        transform.parent = other.transform;
        
        DotsManager.instance.Highlight(highlightLocation);
    }

    void RandomizeSpeedAndDirection()
    {
        speed = Random.Range(randomMinSpeed, randomMaxSpeed);
        float targetPositionRandomizationRadius = targetSphereRadius;

        Vector3 targetPosition = targetCenter ?? Camera.main.transform.position;
        float distance = Vector3.Distance(targetPosition, transform.position);
        if (distance < slowdownRadius)
        {
            float multiplier = Mathf.Max(distance / slowdownRadius, 0.01f);
            
            speed *= multiplier;
            targetPositionRandomizationRadius *= multiplier;
        }

        // Set the rotation to face a position within a sphere around the camera position
        targetPosition += Random.insideUnitSphere * targetPositionRandomizationRadius;

        // Rotate the along movement direction
        transform.rotation = Quaternion.LookRotation(targetPosition - transform.position);
    }

    void RandomizeScale()
    {
        // Randomize scale over time
        float randomScale = scaleTarget * Mathf.Max(Random.value, scaleRandomMin);
        
        Transform tf = transform;
        tf.localScale = Vector3.zero;
        tf.DOScale(randomScale, scaleDuration).SetEase(Ease.OutQuart);
    }

    void RandomizeColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (!renderer)
            return;

        if (albedoColors.Count > 0)
            renderer.material.SetColor(albedoColorId, albedoColors[Random.Range(0, albedoColors.Count)]);

        if (emissionColors.Count > 0)
            renderer.material.SetColor(emissionColorId, emissionColors[Random.Range(0, emissionColors.Count)]);
    }
}
        