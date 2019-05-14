using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class TestDots : MonoBehaviour
{
    [SerializeField] float sphereCastDistance = 200.0f;
    [SerializeField] float sphereCastRadius = 0.2f;

    [SerializeField] float dotEmissionConeAngle = 10.0f;
    [SerializeField] float maxDotDistanceFromSurfacePointAlongOriginalRayDirection = 1.0f;
    
    [SerializeField] FlyingSphere flyingSpherePrefab;
    [SerializeField] GameObject wavePulsePrefab;

    [SerializeField] float wavePulseSpeed = 10.0f;
    [SerializeField] float wavePulseMaxRange = 20.0f;

    private new Camera camera;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => camera = Camera.main);
        
        ProbeFrom(camera);
        CreateWavePulse();

        //Probe(new Ray(transform.position, transform.forward));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (Input.GetKeyDown(KeyCode.Space) && camera)
            ProbeFrom(camera);
        
        if (Input.GetKeyDown(KeyCode.P))
            CreateWavePulse();
    }

    private void ProbeFrom(Camera cam)
    {
        Assert.IsNotNull(cam);
        
        const int numRaysPerAxis = 4;
        for (int i = 0; i < numRaysPerAxis; ++i)
        {
            for (int j = 0; j < numRaysPerAxis; ++j)
            {
                var viewportPos = new Vector3(i / (float)numRaysPerAxis, j / (float)numRaysPerAxis);
                Ray ray = cam.ViewportPointToRay(viewportPos);

                Debug.DrawRay(ray.origin, ray.direction, Color.white, 10.0f, true);
                Probe(ray, true);
            }
        }

        CreateWavePulse();
    }

    private void CreateWavePulse()
    {
        Assert.IsNotNull(wavePulsePrefab);
        
        GameObject pulse = Instantiate(wavePulsePrefab, transform.position, Quaternion.identity);
        
        Transform tf = pulse.transform;
        tf.localScale = Vector3.zero;
        tf.DOScale(wavePulseMaxRange * 2.0f, wavePulseMaxRange / wavePulseSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(pulse));
    }
    
    private void Probe(Ray ray, bool spawnFlyingSphere = false)
    {
        LayerMask layerMask = DotsManager.instance.GetDotsSurfaceLayerMask();
        RaycastHit hit;
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out hit, sphereCastDistance, layerMask, QueryTriggerInteraction.Ignore);
        if (!didHit)
            return;

        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = ray,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotEmissionConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRayDirection = maxDotDistanceFromSurfacePointAlongOriginalRayDirection
        };

        if (spawnFlyingSphere)
        {
            Assert.IsNotNull(flyingSpherePrefab);

            // Use this instead of ray.origin because ray.origin is on not at the camera's position, but on its near plane.
            Vector3 position = transform.position;
            
            this.Delay(Vector3.Distance(hit.point,position) / wavePulseSpeed, () =>
            {
                FlyingSphere flyingSphere = Instantiate(flyingSpherePrefab, hit.point, Quaternion.identity);
                flyingSphere.SetTarget(position);
                flyingSphere.highlightLocation = highlightLocation;
            });
        }
        else 
        {
            // TODO: For testing purposes move this out of this block to paint the dots when space is hit.
            DotsManager.instance.Highlight(highlightLocation);
        }
    }
}
