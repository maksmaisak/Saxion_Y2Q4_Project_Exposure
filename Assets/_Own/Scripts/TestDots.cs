using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class TestDots : MonoBehaviour
{
    [SerializeField] float sphereCastDistance = 200.0f;
    [SerializeField] float sphereCastRadius = 0.2f;

    [SerializeField] float dotEmissionConeAngle = 40.0f;
    [SerializeField] float maxDotDistanceFromSurfacePointAlongOriginalRayDirection = 1.0f;
    [SerializeField] GameObject flyingSpherePrefab;
    [SerializeField] LayerMask collisionLayer = 1 << 9;
    
    
    IEnumerator Start()
    {
        DisableAllRenderers();

        yield return new WaitUntil(() => Camera.main);

        Debug.Log(Camera.main.gameObject.name);

        ProbeFrom(Camera.main);

        //Probe(new Ray(transform.position, transform.forward));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            Probe(new Ray(transform.position, transform.forward), true);
        }
    }

    private void ProbeFrom(Camera cam)
    {
        Assert.IsNotNull(cam);
        
        const int numRaysPerAxis = 20;
        for (int i = 0; i < numRaysPerAxis; ++i)
        {
            for (int j = 0; j < numRaysPerAxis; ++j)
            {
                var viewportPos = new Vector3(i / (float)numRaysPerAxis, j / (float)numRaysPerAxis);
                Ray ray = cam.ViewportPointToRay(viewportPos);

                Debug.DrawRay(ray.origin, ray.direction, Color.white, 10.0f, true);
                Probe(ray);
            }
        }
    }

    private static void DisableAllRenderers()
    {
        foreach (var foundRenderer in FindObjectsOfType<Renderer>())
            if (!foundRenderer.GetComponent<ParticleSystem>())
                foundRenderer.enabled = false;
    }

    private void Probe(Ray ray, bool spawnFlyingSphere = false)
    {
        RaycastHit hit;
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out hit, sphereCastDistance, collisionLayer);
        if (!didHit)
            return;

        RadarHighlightLocation highlightLocation = new RadarHighlightLocation
        {
            originalRay = ray,
            pointOnSurface = hit.point,
            dotEmissionConeAngle = dotEmissionConeAngle,
            maxDotDistanceFromSurfacePointAlongOriginalRayDirection =
                maxDotDistanceFromSurfacePointAlongOriginalRayDirection
        };

        if (spawnFlyingSphere)
        {
            Assert.IsNotNull(flyingSpherePrefab);

            GameObject flyingSphere = Instantiate(
                flyingSpherePrefab,
                hit.point,
                Quaternion.identity);

            FlyingSphere flyingSphereComp = flyingSphere.GetComponent<FlyingSphere>();
            flyingSphereComp.highlightLocation = highlightLocation;
        }
        else {
            // TODO: For testing purposes move this out of this block to paint the dots when space is hit.
            DotsManager.instance.Highlight(highlightLocation);
        }
    }
}
