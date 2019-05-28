using System;
using VRTK;
using VRTK.Examples;

public class HandsController : ToggleCustomHands, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    private VRTK_Pointer pointer;
    
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
            AssignControllerPointers();
        });
    }

    private void AssignControllerPointers()
    {
        pointer = rightController.GetComponent<VRTK_Pointer>();
        pointer.Toggle(false);
    }
    
    // TODO only toggle if it's the current section getting revealed
    public void On(OnRevealEvent reveal) => pointer.Toggle(true);

    public void On(OnTeleportEvent teleport)
    {
        LightSection lightSection = teleport.navpoint.GetComponentInParent<LightSection>();
        pointer.Toggle(lightSection && lightSection.isRevealed);
    }
}
