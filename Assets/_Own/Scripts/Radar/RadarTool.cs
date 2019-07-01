using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.Events;
using VRTK;

[Serializable]
public struct PulseSettings
{
    public static readonly PulseSettings Default = new PulseSettings
    {
        wavePulseAngleHorizontal = 40.0f,
        wavePulseAngleVertical = 40.0f,
        wavePulseSpeed = 10.0f,
        wavePulseMaxRange = 35.0f,
        sphereCastRadius = 0.05f,
        
        maxNumWavespheresPerPulse = -1,
        maxNumWavespheresPerSecond = new RangeFloat(3.0f, 3.0f),
        minDistanceBetweenSpawnedWavespheres = 2.0f,
        wavesphereSpeedMin = 2.5f,
        wavesphereSpeedMax = 4.5f,
        
        baseDotConeAngle = 40.0f,
        dotConeAngleFalloff = 0.1f,
        dotConeAngleFalloffPower = 1.0f,
        maxDotDistanceFromSurfacePointAlongOriginalRay = 1.0f
    };
    
    [Header("Wave Pulse Settings")]
    public GameObject wavePulsePrefab;
    [Range(0.0f, 360.0f)] public float wavePulseAngleHorizontal;
    [Range(0.0f, 360.0f)] public float wavePulseAngleVertical;
    public float wavePulseSpeed   ;
    public float wavePulseMaxRange;
    public float sphereCastRadius ;

    [Header("Wavesphere Settings")] 
    public RangeFloat maxNumWavespheresPerSecond;
    public int maxNumWavespheresPerPulse;
    public float minDistanceBetweenSpawnedWavespheres;
    public float wavesphereSpeedMin;
    public float wavesphereSpeedMax;
    public Wavesphere wavespherePrefab;
    public Transform  wavesphereTarget;

    [Header("Dots Settings")]
    [Range(0.0f, 360.0f)] public float baseDotConeAngle;
    [Range(0.01f, 1.0f)]  public float dotConeAngleFalloff;
    [Range(0.1f , 5.0f)]  public float dotConeAngleFalloffPower;
    public float maxDotDistanceFromSurfacePointAlongOriginalRay;
}

public class RadarTool : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [Serializable]
    public class OnSpawnedWavesphereHandler : UnityEvent<RadarTool, Wavesphere> {}
    
    [SerializeField] PulseSettings pulseSettings = PulseSettings.Default;
    public OnSpawnedWavesphereHandler onSpawnedWavesphere;
    public UnityEvent onPulse;

    [Header("Debug settings")] 
    [SerializeField] bool highlightWithoutWavespheres = false;
    [SerializeField] bool drawSpherecastRays          = false;
    
    private new Transform transform;
    private RadarPulse pulse;

    protected override void Awake()
    {
        base.Awake();
        transform = base.transform;
        pulse = new RadarPulse();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (pulse != null)
            pulse.Dispose();
    }

    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.R)) 
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        if (Input.GetKeyDown(KeyCode.Space)) 
            Pulse();
    }

    public void Pulse()
    {
        onPulse?.Invoke();
        
        if (pulse.isWorkingJob)
        {
            pulse.Complete().Each(SpawnWavesphere);
            Assert.IsFalse(pulse.isWorkingJob);
        }
        
        StartCoroutine(PulseCoroutine());
    }
    
    public PulseSettings GetPulseSettings() => pulseSettings;
    public void SetPulseSettings(PulseSettings newPulseSettings) => pulseSettings = newPulseSettings;

    public void On(OnRevealEvent message) => StopAllCoroutines();
    
    private IEnumerator PulseCoroutine()
    {
        Assert.IsFalse(pulse.isWorkingJob);
        
        CreatePulseWaveVisual();

        pulse.drawSpherecastRays = drawSpherecastRays;
        pulse.pulseSettings = pulseSettings;
        pulse.DoPulse(
            transform.position,
            transform.rotation,
            DotsManager.instance.GetDotsSurfaceLayerMask()
        );
        
        yield return new WaitUntil(() => !pulse.isWorkingJob || pulse.isJobCompleted);
        if (!pulse.isWorkingJob) 
            yield break;
        
        Assert.IsTrue(pulse.isJobCompleted);
        pulse.Complete().Each(SpawnWavesphere);
    }
    
    private static readonly int CosHalfVerticalAngle   = Shader.PropertyToID("_CosHalfVerticalAngle");
    private static readonly int CosHalfHorizontalAngle = Shader.PropertyToID("_CosHalfHorizontalAngle");

    private void CreatePulseWaveVisual()
    {
        Assert.IsNotNull(pulseSettings.wavePulsePrefab);
        
        GameObject pulse = Instantiate(pulseSettings.wavePulsePrefab, transform.position, transform.rotation);
        
        Transform tf = pulse.transform;
        tf.localScale = Vector3.zero;
        tf.DOScale(pulseSettings.wavePulseMaxRange * 2.0f, pulseSettings.wavePulseMaxRange / pulseSettings.wavePulseSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(pulse));
        
        var material = pulse.GetComponent<Renderer>().material;
        material.SetFloat(CosHalfHorizontalAngle, Mathf.Cos(Mathf.Deg2Rad * pulseSettings.wavePulseAngleHorizontal * 0.5f));
        material.SetFloat(CosHalfVerticalAngle  , Mathf.Cos(Mathf.Deg2Rad * pulseSettings.wavePulseAngleVertical   * 0.5f));
    }

    private void SpawnWavesphere(RadarHighlightLocation highlightLocation)
    {
        Wavesphere prefab = pulseSettings.wavespherePrefab;
        Assert.IsNotNull(prefab);
        
        Vector3 targetPosition = GetWavesphereTargetTransform().position;
        
        this.Delay(highlightLocation.distanceFromOrigin / pulseSettings.wavePulseSpeed, () =>
        {
            if (highlightLocation.lightSection && highlightLocation.lightSection.isRevealed)
                return;
            
            if (highlightWithoutWavespheres)
            {
                DotsManager.instance.Highlight(highlightLocation, highlightLocation.originalRay.origin);
                return;
            }
            
            Wavesphere wavesphere = Instantiate(prefab, highlightLocation.pointOnSurface, Quaternion.identity);
            wavesphere.Initialize(targetPosition, highlightLocation.wavesphereSpeed, highlightLocation.lightSection);
            wavesphere.highlightLocation = highlightLocation;
            
            onSpawnedWavesphere?.Invoke(this, wavesphere);
        });
    }

    private Transform GetWavesphereTargetTransform()
    {
        Transform tf = pulseSettings.wavesphereTarget;
        if (tf)
            return tf;

        if (tf = VRTK_DeviceFinder.HeadsetCamera())
            return tf;

        if (tf = Camera.main.transform)
            return tf;

        return transform;
    }
}
