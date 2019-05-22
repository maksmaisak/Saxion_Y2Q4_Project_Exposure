using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnRevealEvent : BroadcastEvent<OnRevealEvent>
{
    public readonly LightSection lightSection;

    public OnRevealEvent(LightSection lightSection) => this.lightSection = lightSection;
}