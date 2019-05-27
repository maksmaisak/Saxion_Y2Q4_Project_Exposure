using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

/// Autoparents itself to the play area.
public class PlayArea : MonoBehaviour
{
    IEnumerator Start()
    {
        Transform playAreaTransform = null;
        yield return new WaitUntil(() => playAreaTransform = VRTK_DeviceFinder.PlayAreaTransform());
        
        Assert.IsNotNull(playAreaTransform, "Play area transform not found!");
        transform.parent = playAreaTransform;
    }
}
