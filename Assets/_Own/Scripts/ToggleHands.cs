using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK.Examples;

public class ToggleHands : ToggleCustomHands 
{
    protected override void OnEnable()
    {
        state = false;
        ToggleVisibility();

        // we toggle the visibility again with a small delay because the SDK_Setup is not loaded yet
        this.Delay(0.5f, () =>
        {
            state = true;
            ToggleVisibility();
        });
    }

    protected  override void OnDisable() {}
}
