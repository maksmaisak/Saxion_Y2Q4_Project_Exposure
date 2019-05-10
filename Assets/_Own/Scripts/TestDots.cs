using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class TestDots : MonoBehaviour
{
    [SerializeField] new ParticleSystem particleSystem;
    [SerializeField] float sphereCastRadius = 0.2f;

    void Start()
    {
        DisableAllRenderers();

        Assert.IsNotNull(particleSystem);

        var cam = Camera.main;
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

        //StartCoroutine(ProbeCoroutine());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private IEnumerator ProbeCoroutine()
    {
        while (true)
        {
            Probe(new Ray(transform.position, transform.forward), 2);
            yield return null;
        }
    }

    private static void DisableAllRenderers()
    {
        foreach (var foundRenderer in FindObjectsOfType<Renderer>())
            if (!foundRenderer.GetComponent<ParticleSystem>())
                foundRenderer.enabled = false;
    }

    private void Probe(Ray ray, int maxNumDots = 100)
    {
        RaycastHit hit;
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out hit);
        if (!didHit)
            return;

        //Vector3 direction = -hit.normal;
        Vector3 direction = ray.direction;
        DotsManager.instance.Highlight(new RadarHighlightLocation
        {
            originalRay = ray,
            pointOnSurface = hit.point,
            distributionDirection = ray.direction //-hit.normal
        });
    }
}
