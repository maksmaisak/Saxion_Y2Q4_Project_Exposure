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

public class FlyingSphere : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [Header("Movement Settings")] 
    [SerializeField] float targetSphereRadius = 0.25f;
    [SerializeField] float attractionRadius = 1.5f;
    [SerializeField] float attractionAngularSpeed = 1.0f;

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
    [SerializeField] int vibrationDuration = 40;
    [SerializeField] int frequency = 2;
    [SerializeField] int strength = 100;

    [Header("Audio Settings")]
    [SerializeField] AudioClip spawnAudio;
    [SerializeField] [Range(0, 1)] float grabAudioVolume;
    [SerializeField] bool playOnAwake = false;
    [SerializeField] AudioSource buzzAudioSource;

    [Header("Other Settings")] 
    [Tooltip("If set, the sphere will move in a way that makes sure it gets caught no matter what.")]
    [SerializeField] bool mustGetCaught;
    [Tooltip("If not caught within this many seconds, the sphere despawns. If `mustGetCaught` is set, it never despawns.")]
    [SerializeField] float delayToDespawn = 20.0f;
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

        RandomizeColor();
        RandomizeScale();
        RandomizeSpeedAndDirection();

        audioSource.playOnAwake = playOnAwake;

        audioSource.PlayOneShot(spawnAudio);
        
        if (!mustGetCaught)
            Destroy(gameObject, delayToDespawn);
    }

    void Update()
    {
        if (isFadingOut)
            return;

        transform.position += speed * speedMultiplier * Time.deltaTime * transform.forward;

        AttractToHands();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFadingOut || !handsCollisionLayer.ContainsLayer(other.gameObject.layer))
            return;

        DotsManager.instance.Highlight(highlightLocation, transform.position);
        
        isFadingOut = true;

        lastTimeWasCaught = Time.time;

        AudioClip grabAudioClip = FlyingSphereAudio.instance.GetGrabAudioClip();
        Assert.IsNotNull(grabAudioClip);
        audioSource.clip = grabAudioClip;
        audioSource.volume = grabAudioVolume;
        audioSource.Play();

        VibrateController(other);

        transform.parent = other.transform;
        
        onCaught?.Invoke();

        transform.DOKill();
        const float Duration = 0.2f;
        transform.DOScale(0.0f, Duration)
            .SetEase(Ease.OutQuart);
        var otherPosition = other.transform.position;
        transform.DOLookAt(otherPosition - transform.position, Duration)
            .SetEase(Ease.OutQuart);

        buzzAudioSource?.DOFade(0.0f, 0.1f);
        
        Destroy(gameObject, Mathf.Max(grabAudioClip.length / audioSource.pitch, Duration));
    }

    public void On(OnRevealEvent reveal)
    {
        if (isFadingOut)
            return;

        //if (!sourceLightSection || reveal.lightSection != sourceLightSection)
        //    return;

        isFadingOut = true;
        transform.DOKill();
        transform
            .DOScale(0.0f, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void RandomizeSpeedAndDirection()
    {
        var targetPositionRandomizationRadius = targetSphereRadius;

        var targetPosition = targetCenter ?? Camera.main.transform.position;
        var distance = Vector3.Distance(targetPosition, transform.position);
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

    // Randomize scale over time
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
        Vector3 targetDelta = targetTransform.position - position;
        
        if (!mustGetCaught)
            if (targetDelta.sqrMagnitude > attractionRadius * attractionRadius)
                return;

        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDelta.normalized, attractionAngularSpeed * Time.deltaTime, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDir);
    }

    private void VibrateController(Collider other)
    {
        GameObject otherController = other.gameObject.GetComponentInParent<VRTK_Pointer>()?.gameObject;
        if (!otherController)
            return;

        bool isLeftHand = VRTK_DeviceFinder.IsControllerLeftHand(otherController);

        VRTK_ControllerReference controllerReference = isLeftHand
            ? VRTK_DeviceFinder.GetControllerReferenceLeftHand()
            : VRTK_DeviceFinder.GetControllerReferenceRightHand();

        VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, 1, 0.5f, pulseInterval: 0.02f);
    }
}
        