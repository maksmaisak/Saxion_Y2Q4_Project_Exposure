using System;
using VRTK;
using VRTK.Examples;

public class HandsController : ToggleCustomHands
{
    protected void Start() => EventsManager.instance.Add(this);

    protected void OnDestroy()
    {
        var manager = EventsManager.instance;
        if (manager) 
            manager.Remove(this);
    }

    protected override void OnEnable()
    {
        state = false;
        ToggleVisibility();

        // we toggle the visibility again with a small delay because the SDK_Setup is not loaded yet
        this.Delay(0.1f, () =>
        {
            state = true;
            ToggleVisibility();
        });
    }
}
