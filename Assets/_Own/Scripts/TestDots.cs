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
        //bool didHit = Physics.Raycast(ray, out hit);
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out hit);
        if (!didHit)
            return;
        
        //Vector3 direction = -hit.normal;
        Vector3 direction = ray.direction;
        Vector3 center = hit.point - direction * 1.0f;
        Reveal(ray.origin, center, direction, maxNumDots);
    }

    private void Reveal(Vector3 rayOrigin, Vector3 position, Vector3 direction, int maxNumDots = 100)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);
        for (int i = 0; i < maxNumDots; ++i)
        {
            Vector3 origin = position + rotation * Random.insideUnitCircle * 0.5f;            
            RaycastHit dotHit;
            bool didHitDot = Physics.Raycast(new Ray(origin, direction), out dotHit);
            if (!didHitDot)
                continue;

            /*if (Vector3.SqrMagnitude(dotHit.point - rayOrigin) > 5.0f * 5.0f)
                continue;*/
            
            AddDot(dotHit.point);
        }
    }

    private void AddDot(Vector3 position)
    {
        var emitParams = new ParticleSystem.EmitParams {position = position};
        particleSystem.Emit(emitParams, 1);
        
        // To make it recalculate the bounds used for culling
        particleSystem.Simulate(0.01f, false, false, false);
    }
}
