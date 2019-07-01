using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using VRTK;
using Random = UnityEngine.Random;

public class Wavesphere : MyBehaviour, IEventReceiver<OnRevealEvent> 
{
    [Header("Movement Settings")] 
    [SerializeField] float targetSphereRadius = 0.25f;
    [SerializeField] float attractionRadius = 1.5f;
    [SerializeField] float attractionAngularSpeed = 1.0f;
    [SerializeField] float forcedAttractionRadius = 1.0f;

    [Tooltip("If the wavesphere is spawned closer than this to the target, it will be slower.")] 
    [SerializeField] float slowdownRadius = 4.0f;

    [Header("Scaling Settings")] 
    [SerializeField] float scaleDuration = 0.7f;
    [SerializeField] float scaleMin = 0.0864f;
    [SerializeField] float scaleMax = 0.27f;

    [Header("Color Settings")] 
    [SerializeField] string albedoColorId = "_AlbedoColor_549AC39B";

    [SerializeField] string emissionColorId = "_EmissionColor_40E9251C";
    [SerializeField] List<Color> albedoColors = new List<Color>();
    [SerializeField] List<Color> emissionColors = new List<Color>();

    [Header("Vibration Settings")] 
    [SerializeField] float vibrationStrength = 0.5f;
    [SerializeField] float vibrationDuration = 0.1f;
    [SerializeField] float vibrationPulseInterval = 0.02f;

    [Header("Audio Settings")]
    [SerializeField] [Range(0, 1)] float grabAudioVolume;
    [SerializeField] AudioSource buzzAudioSource;
    
    [Header("Despawn settings")]
    [Tooltip("Spheres moving away from the player further than this distance will be despawned.")]
    [SerializeField] float minDespawnDistance = 2.0f;
    [SerializeField] float delayBetweenDespawnChecks = 0.1f;
    
    [Header("Other Settings")] 
    [Tooltip("If set, the sphere will move in a way that makes sure it gets caught no matter what.")]
    [SerializeField] bool mustGetCaught;
    [SerializeField] LayerMask handsCollisionLayer;

    public UnityEvent onCaught = new UnityEvent();

    private new Transform transform;
    private AudioSource audioSource;
    private float speed = 1.0f;
    private Vector3? targetCenter;
    private LightSection sourceLightSection;
    private bool didStart;
    private bool isFadingOut;

    private readonly List<Transform> targetTransforms = new List<Transform>();

    public RadarHighlightLocation highlightLocation { get; set; }
    public float speedMultiplier { get; set; } = 1.0f;
    public bool isVisibleToCamera { get; set; } = true;
    // The direction to rotate the wavesphere to
    public Vector3 targetDirection { get; set; }
    
    private static float lastTimeWasCaught;

    public void Initialize(Vector3 target, float movementSpeed, LightSection sourceSection)
    {
        Assert.IsFalse(didStart, "Can't Initialize a wavesphere after it started moving.");
        targetCenter = target;
        speed = movementSpeed;
        sourceLightSection = sourceSection;
    }

    protected override void Awake()
    {
        base.Awake();

        transform   = GetComponent<Transform>();
        audioSource = GetComponent<AudioSource>();

        if (VRTK_SDKManager.GetLoadedSDKSetup() != null)
        {
            targetTransforms.Add(VRTK_DeviceFinder.GetControllerLeftHand().transform);
            targetTransforms.Add(VRTK_DeviceFinder.GetControllerRightHand().transform);
        }
        else
        {
            var camera = Camera.main;
            if (camera)
                targetTransforms.Add(camera.transform);
        }
    }

    void Start()
    {
        didStart = true;

        RandomizeColor();
        RandomizeScale();
        RandomizeSpeedAndDirection();

        if (!mustGetCaught)
            StartCoroutine(CheckMissCoroutine());
    }

    void Update()
    {
        if (isFadingOut)
            return;

        AttractToHands();
        transform.position += speed * speedMultiplier * Time.deltaTime * transform.forward;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isFadingOut || !handsCollisionLayer.ContainsLayer(other.gameObject.layer))
            return;

        DotsManager.instance.Highlight(highlightLocation, transform.position);
        
        lastTimeWasCaught = Time.time;
        isFadingOut = true;

        VibrateController(other);
        float grabAudioDuration = PlayGrabSound();
        float attachAndFadeOutDuration = AttachAndDisappear(other.transform);
        FadeOutAudio();

        onCaught?.Invoke();
        new OnWavesphereCaught(this).SetDeliveryType(MessageDeliveryType.Immediate).PostEvent();
        Destroy(gameObject, Mathf.Max(grabAudioDuration, attachAndFadeOutDuration));
    }
    
    private IEnumerator CheckMissCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(delayBetweenDespawnChecks);
            
            Vector3 position = transform.position;
            Vector3 direction = transform.forward;
            float sqrMinDespawnDistance = minDespawnDistance * minDespawnDistance;
            bool didMiss = targetTransforms.Count > 0 && targetTransforms.All(t =>
            {
                Vector3 delta = t.position - position;
                return 
                    Vector3.Dot(direction, delta) < 0.0f && 
                    delta.sqrMagnitude > sqrMinDespawnDistance;
            });

            if (!didMiss) 
                continue;
            
            new OnWavesphereMissed(this).SetDeliveryType(MessageDeliveryType.Immediate).PostEvent();
            isFadingOut = false;
            Disappear().OnComplete(() => Destroy(gameObject));
            yield break;
        }
    }
    
    public void On(OnRevealEvent reveal)
    {
        if (isFadingOut)
            return;

        //if (!sourceLightSection || reveal.lightSection != sourceLightSection)
        //    return;
        
        isFadingOut = true;
        Disappear().OnComplete(() => Destroy(gameObject));
        FadeOutAudio();
    }

    private void RandomizeSpeedAndDirection()
    {
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
        targetDirection = (targetPosition - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(targetDirection);
    }

    private void RandomizeScale()
    {
        float scale = Random.Range(scaleMin, scaleMax);

        Transform tf = transform;
        tf.DOKill();
        tf.localScale = Vector3.zero;
        tf.DOScale(scale, scaleDuration).SetEase(Ease.OutQuart);
    }

    private void RandomizeColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (!renderer)
            return;

        if (albedoColors.Count > 0)
            renderer.material.SetColor(albedoColorId, albedoColors[Random.Range(0, albedoColors.Count)]);

        if (emissionColors.Count > 0)
            renderer.material.SetColor(emissionColorId, emissionColors[Random.Range(0, emissionColors.Count)]);
    }

    private void AttractToHands()
    {
        if (targetTransforms.Count == 0)
            return;

        Vector3 position = transform.position;
        Transform targetTransform = targetTransforms.ArgMin(t => (t.position - position).sqrMagnitude);
        Vector3 sphereToTargetDelta = targetTransform.position - position;
        float distanceToTargetSqr = sphereToTargetDelta.sqrMagnitude;
        // Allow to change target Direction from FlyingSphereTutorial class
        if (!mustGetCaught || (mustGetCaught && isVisibleToCamera) ||
            (mustGetCaught && !isVisibleToCamera && distanceToTargetSqr <= forcedAttractionRadius * forcedAttractionRadius))
            targetDirection = sphereToTargetDelta;

        if (!mustGetCaught && distanceToTargetSqr > attractionRadius * attractionRadius)
                return;

        Vector3 newDir = Vector3.RotateTowards(
            transform.forward, 
            targetDirection.normalized,
            attractionAngularSpeed * Time.deltaTime, 0.0f
        );
        transform.rotation = Quaternion.LookRotation(newDir);
    }

    private float PlayGrabSound()
    {
        AudioClip grabAudioClip = WavesphereAudio.instance.GetGrabAudioClip();
        Assert.IsNotNull(grabAudioClip);
        audioSource.clip = grabAudioClip;
        audioSource.volume = grabAudioVolume;
        audioSource.Play();

        return grabAudioClip.length / audioSource.pitch;
    }
    
    private void VibrateController(Collider other)
    {
        VRTK_PlayerObject controller = other.gameObject.GetComponentsInParent<VRTK_PlayerObject>()
            .FirstOrDefault(po => po.objectType == VRTK_PlayerObject.ObjectTypes.Controller);
        if (!controller)
            return;
        
        VRTK_ControllerReference controllerReference = VRTK_DeviceFinder.IsControllerLeftHand(controller.gameObject)
            ? VRTK_DeviceFinder.GetControllerReferenceLeftHand()
            : VRTK_DeviceFinder.GetControllerReferenceRightHand();
        if (controllerReference == null)
            return;
        
        VRTK_ControllerHaptics.TriggerHapticPulse(
            controllerReference, 
            vibrationStrength, vibrationDuration, vibrationPulseInterval
        );
    }

    private float AttachAndDisappear(Transform attach)
    {
        const float Duration = 0.2f;
        
        Transform scaleEffectParent = new GameObject("ScaleEffectParent").transform;
        scaleEffectParent.SetParent(attach, worldPositionStays: false);
        scaleEffectParent.DOScale(0.0f, Duration).SetEase(Ease.InQuad);

        transform.DOKill();
        transform.parent = scaleEffectParent;
        transform.DOLookAt(attach.position, Duration).SetEase(Ease.InQuad);

        return Duration;
    }

    private Tweener Disappear()
    {
        transform.DOKill();
        return transform
            .DOScale(0.0f, 0.5f)
            .SetEase(Ease.InBack);
    }
    
    private void FadeOutAudio()
    {
        if (buzzAudioSource)
        {
            buzzAudioSource.DOKill();
            buzzAudioSource.DOFade(0.0f, 0.1f);
        }
    }
}
        