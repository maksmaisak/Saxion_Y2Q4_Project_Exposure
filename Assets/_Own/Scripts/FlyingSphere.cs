using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;
using Random = UnityEngine.Random;

public class FlyingSphere : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [Header("Movement Settings")] [SerializeField]
    float angularSpeed = 1.0f;

    [SerializeField] float attractionRadius = 1.5f;
    [SerializeField] float targetSphereRadius = 0.25f;

    [Tooltip("If the wavesphere is spawned closer than this to the target, it will be slower.")] [SerializeField]
    float slowdownRadius = 4.0f;

    [Header("Scaling Settings")] [SerializeField]
    float scaleTarget = 0.8f;

    [SerializeField] float scaleDuration = 0.7f;
    [SerializeField] float scaleRandomMin = 0.4f;

    [Header("Color Settings")] [SerializeField]
    string albedoColorId = "_AlbedoColor_549AC39B";

    [SerializeField] string emissionColorId = "_EmissionColor_40E9251C";
    [SerializeField] List<Color> albedoColors = new List<Color>();
    [SerializeField] List<Color> emissionColors = new List<Color>();

    [Header("Vibration Settings")] [SerializeField]
    int vibrationDuration = 40;

    [SerializeField] int frequency = 2;
    [SerializeField] int strength = 100;

    [Header("Audio Settings")]
    [SerializeField] AudioClip spawnAudio;
    [SerializeField] [Range(0, 1)] float grabAudioVolume;
    [SerializeField] bool playOnAwake = false;

    [Header("Other Settings")] 
    [SerializeField] float delayToDespawn = 20.0f;
    [SerializeField] LayerMask handsCollisionLayer;

    private new Transform transform;
    private AudioSource audioSource;

    private float speed = 1.0f;
    private Vector3? targetCenter;
    private LightSection sourceLightSection;

    private bool didStart;
    private bool isFadingOut;

    private readonly List<Transform> targetTransforms = new List<Transform>();

    public RadarHighlightLocation highlightLocation { get; set; }

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
        
        Destroy(gameObject, delayToDespawn);

    }

    void Update()
    {
        if (isFadingOut)
            return;

        transform.position += speed * Time.deltaTime * transform.forward;

        AttractToHands();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFadingOut || !handsCollisionLayer.ContainsLayer(other.gameObject.layer))
            return;

        DotsManager.instance.Highlight(highlightLocation, transform.position);

        isFadingOut = true;
        //Debug.Log("Seconds since previous wavesphere caught:" + Time.time - lastTimeWasCaught);
        lastTimeWasCaught = Time.time;

        AudioClip grabAudioClip = FlyingSphereAudio.instance.GetGrabAudioClip();
        Assert.IsNotNull(grabAudioClip);
        audioSource.clip = grabAudioClip;
        audioSource.volume = grabAudioVolume;
        audioSource.Play();

        VibrateController(other);

        transform.parent = other.transform;

        transform.DOKill();

        const float duration = 0.2f;

        transform.DOScale(0.0f, duration)
            .SetEase(Ease.OutQuart);

        var otherPosition = other.transform.position;
        transform.DOLookAt(otherPosition - transform.position, duration)
            .SetEase(Ease.OutQuart);

        Destroy(gameObject, Mathf.Max(grabAudioClip.length / audioSource.pitch, duration));
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

    private void RandomizeScale()
    {
        // Randomize scale over time
        float randomScale = scaleTarget * Mathf.Max(Random.value, scaleRandomMin);

        Transform tf = transform;
        tf.localScale = Vector3.zero;
        tf.DOScale(randomScale, scaleDuration).SetEase(Ease.OutQuart);
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

        Transform targetTransform =
            targetTransforms.ArgMin(x => (x.transform.position - transform.position).sqrMagnitude);
        Vector3 targetDir = targetTransform.position - transform.position;
        if (targetDir.sqrMagnitude > attractionRadius * attractionRadius)
            return;

        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, angularSpeed * Time.deltaTime, 0.0f);
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
        