using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

public class NavpointIndicator : MyBehaviour, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] Transform navpointTransform;
    [SerializeField] float delayTillDisplay = 8.0f;
    [SerializeField] List<Transform> indicatorLocationsTransform = new List<Transform>();
    
    private bool canDisplay = false;

    private MeshRenderer[] meshRenderers;

    private Transform currentLocationTransform;

    private Vector3 directionToNavpoint;
    
    Transform cameraTransform;
    
    private IEnumerator Start()
    {
        Assert.IsNotNull(transform.parent, "Navpoint Indicator should be parented under a Light Section!");
        Assert.IsNotNull(navpointTransform, "Please set the next navpoint this indicator should point to!");

        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        
        Assert.IsNotNull(meshRenderers);
        
        foreach (MeshRenderer meshRenderer in meshRenderers)
            meshRenderer.enabled = false;

        yield return new WaitUntil(() => Camera.main != null);

        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (!canDisplay)
            return;
        
        directionToNavpoint = (navpointTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToNavpoint);
    }
    
    public void On(OnRevealEvent onReveal)
    {
        if (navpointTransform && transform.IsChildOf(onReveal.lightSection.transform))
        {
            canDisplay = true;
            
            this.Delay(delayTillDisplay, ()=> 
            { 
                foreach (MeshRenderer meshRenderer in meshRenderers)
                    meshRenderer.enabled = true;

                StartCoroutine(MoveToClosestLocationInCameraView());
            });
        }
    }

    public void On(OnTeleportEvent message)
    {
        // If the next navpoint is not in the current lightsection stop displaying the arrow
        if (canDisplay && !message.navpoint.transform.IsChildOf(transform.parent))
        {
            canDisplay = false;
            
            foreach (MeshRenderer meshRenderer in meshRenderers)
                meshRenderer.enabled = false;

            transform.DOKill();
            
            StopAllCoroutines();
        }
    }

    private IEnumerator MoveToClosestLocationInCameraView()
    {
        if (indicatorLocationsTransform.Count <= 0)
            yield break;

        while (true)
        {
            Vector3 cameraPos = cameraTransform.position;
            Vector3 cameraForward = cameraTransform.forward;

            Transform newTransform = indicatorLocationsTransform.ArgMax(t =>
                Vector3.Dot((t.position - cameraPos).normalized, cameraForward));

            if (newTransform && currentLocationTransform != newTransform)
            {
                transform.DOKill();

                currentLocationTransform = newTransform;

                yield return transform
                    .DOMove(currentLocationTransform.position, 1.5f)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() =>
                    {
                        transform
                            .DOPunchPosition(directionToNavpoint, 1.5f, 1, 0.1f)
                            .SetEase(Ease.OutQuart)
                            .SetLoops(-1);
                    })
                    .WaitForCompletion();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
