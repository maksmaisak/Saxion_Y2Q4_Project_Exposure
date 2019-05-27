using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnReveal : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [Tooltip("This will happen once the section containing this object is revealed.")]
    public UnityEvent onReveal;

    public void On(OnRevealEvent reveal)
    {
        // If there is nothing hooked up to this event then there is no reason to continue
        if (onReveal == null)
            return;
        
        if (reveal.lightSection.GetGameObjects().Contains(gameObject) || transform.IsChildOf(reveal.lightSection.transform))
            onReveal.Invoke();
    }
}
