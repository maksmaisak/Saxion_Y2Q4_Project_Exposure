using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OnReveal : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [Tooltip("This will happen once the section containing this object is revealed.")]
    public UnityEvent onReveal;
    [SerializeField] float delay = 1.0f;

    private ParticleSystem[] particleSystems;

    private void Start()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
            onReveal.AddListener(ps.Play);
        }
    }

    public void On(OnRevealEvent reveal)
    {
        // If there is nothing hooked up to this event then there is no reason to continue
        if (onReveal == null)
            return;

        if (!transform.IsChildOf(reveal.lightSection.transform) && !reveal.lightSection.GetGameObjects().Contains(gameObject))
            return;

        if (delay <= 0.0f)
            onReveal.Invoke();
        else
            this.Delay(delay, onReveal.Invoke);
    }
}