using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRTK;

public class RadarController : VRTK_InteractableObject
{
    [Header("Radar Controller")]
    [SerializeField] RadarTool radarTool;
    [SerializeField] float fireCooldown = 1.0f;
    
    private bool canShoot = true;
    private Rigidbody rb;
    private Collider[] attachedColliders;

    IEnumerator Start()
    {
        rb = rb ? rb : GetComponent<Rigidbody>();
        attachedColliders = GetComponentsInChildren<Collider>();
        
        yield return new WaitUntil(() =>
                radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        StopAllCoroutines();

        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        rb.useGravity = false;
        
        this.Delay(0.5f, () =>
        {
            if(attachedColliders != null)
                foreach (Collider col in attachedColliders)
                    col.enabled = false;
        });
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        StopAllCoroutines();
        
        if(attachedColliders != null)
            foreach (Collider col in attachedColliders)
                col.enabled = true;
        
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        rb.useGravity = true;
    }

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public override void StartUsing(VRTK_InteractUse currentUsingObject = null)
    {
        base.StartUsing(currentUsingObject);

        if (!canShoot)
            return;
        
        canShoot = false;
        
        radarTool.Probe();

        this.Delay(fireCooldown, () => canShoot = true);
    }
}

