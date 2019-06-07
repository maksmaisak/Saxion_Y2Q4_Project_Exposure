using System.Collections.Generic;
using UnityEngine;

public class OnHighlightEvent : BroadcastEvent<OnHighlightEvent>
{
    public readonly IReadOnlyList<Vector3> dotPositions;

    public OnHighlightEvent(IReadOnlyList<Vector3> dotPositions)
    {
        this.dotPositions = dotPositions;
    }
}