using System.Collections;
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

    IEnumerator Start()
    {
        yield return new WaitUntil(() => radarTool = radarTool ? radarTool : GetComponentInChildren<RadarTool>());
    }

    private void OnEnable()
    {
        base.OnEnable();

        return;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void OnDisable()
    {
        base.OnDisable();

        return;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (Input.GetKeyDown(KeyCode.Space))
            radarTool.Probe();
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

