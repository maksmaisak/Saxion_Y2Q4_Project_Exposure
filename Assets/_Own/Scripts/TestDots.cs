using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TestDots : MonoBehaviour
{
    [SerializeField] new ParticleSystem particleSystem;
    
    void Start()
    {        
        Assert.IsNotNull(particleSystem);
        particleSystem.Pause();
        
        RaycastHit hit;
        bool didHit = Physics.Raycast(new Ray(transform.position, transform.forward), out hit);
        if (!didHit)
            return;
        
        //Vector3 direction = -hit.normal;
        Vector3 direction = transform.forward;
        Vector3 center = hit.point - direction * 1.0f;
        Quaternion rotation = Quaternion.LookRotation(direction);
        for (int i = 0; i < 100; ++i)
        {
            Vector3 origin = center + rotation * Random.insideUnitCircle * 0.5f;
            RaycastHit dotHit;
            bool didHitDot = Physics.Raycast(new Ray(origin, direction), out dotHit);
            if (!didHitDot)
                continue;

            AddDot(dotHit.point);
        }
    }

    private void AddDot(Vector3 position)
    {
        var emitParams = new ParticleSystem.EmitParams {position = position};
        particleSystem.Emit(emitParams, 1);
    }
}
