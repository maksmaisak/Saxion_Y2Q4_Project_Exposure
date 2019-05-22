using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [SerializeField] private UnityEvent OnReveal;
    
    public void On(OnRevealEvent reveal) => OnReveal?.Invoke();
}
