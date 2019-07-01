using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

public class NavpointIndicator : MyBehaviour, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] Transform navpointTransform;
    [SerializeField] Transform arrowTransform;
    [SerializeField] float delayTillDisplay = 10.0f;
    [SerializeField] float appearDuration = 1.0f;
    [SerializeField] List<Transform> indicatorLocationTransforms = new List<Transform>();
    
    private Renderer[] renderers;
    private LightSection lightSection;
    private Transform currentLocationTransform;
    private Vector3 directionToNavpoint;
    
    private bool canDisplay = false;
    private Transform cameraTransform;
    
    IEnumerator Start()
    {
        arrowTransform = arrowTransform ? arrowTransform : transform;
        
        lightSection = GetComponentInParent<LightSection>();
        Assert.IsNotNull(lightSection, "Navpoint Indicator should be parented under a Light Section!");
        Assert.IsNotNull(navpointTransform, "Please set the next navpoint this indicator should point to!");

        renderers = GetComponentsInChildren<Renderer>();
        renderers.Each(r => r.enabled = false);

        yield return new WaitUntil(() => Camera.main != null);
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (!canDisplay)
            return;
        
        directionToNavpoint = (navpointTransform.position - arrowTransform.position).normalized;
        arrowTransform.rotation = Quaternion.LookRotation(directionToNavpoint);
    }
    
    public void On(OnRevealEvent onReveal)
    {
        if (!navpointTransform || lightSection != onReveal.lightSection) 
            return;
        
        canDisplay = true;
        this.Delay(delayTillDisplay, () => 
        { 
            renderers.Each(r => r.enabled = true);

            Transform newTransform = FindNearestLocationInCameraView();
            if (newTransform)
            {
                currentLocationTransform = newTransform;
                arrowTransform.position = newTransform.position;
            }

            StartCoroutine(AppearAndUpdateCoroutine());
        });
    }

    public void On(OnTeleportEvent message)
    {
        if (!canDisplay) 
            return;
        
        canDisplay = false;
        
        renderers.Each(r => r.enabled = false);
        arrowTransform.DOKill();
        StopAllCoroutines();
    }

    private IEnumerator AppearAndUpdateCoroutine()
    {
        yield return arrowTransform
            .DOScale(Vector3.zero, appearDuration)
            .From()
            .SetEase(Ease.OutCirc)
            .WaitForCompletion();

        ArrowPunchPositionToNavpoint();

        if (indicatorLocationTransforms.Count <= 0)
            yield break;

        while (true)
        {
            Transform newTransform = FindNearestLocationInCameraView();
            if (newTransform && currentLocationTransform != newTransform)
            {
                arrowTransform.DOKill();
                currentLocationTransform = newTransform;
                yield return arrowTransform
                    .DOMove(currentLocationTransform.position, 1.5f)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() => ArrowPunchPositionToNavpoint())
                    .WaitForCompletion();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    
    Transform FindNearestLocationInCameraView()
    {
        if (indicatorLocationTransforms.Count <= 0)
            return null;

        Vector3 cameraPosition = cameraTransform.position;
        Vector3 cameraForward  = cameraTransform.forward;
        return indicatorLocationTransforms.ArgMax(t => 
            Vector3.Dot((t.position - cameraPosition).normalized, cameraForward)
        );
    }

    Tween ArrowPunchPositionToNavpoint()
    {
        arrowTransform.DOKill();
        return arrowTransform
            .DOPunchPosition(directionToNavpoint, 1.5f, 1, 0.1f)
            .SetEase(Ease.OutQuart)
            .SetLoops(-1);
    }
}
