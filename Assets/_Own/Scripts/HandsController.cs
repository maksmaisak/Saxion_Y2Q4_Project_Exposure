using System;
using VRTK;
using VRTK.Examples;

public class HandsController : ToggleCustomHands, IEventReceiver<OnRevealEvent>, IEventReceiver<OnTeleportEvent>
{
    private VRTK_Pointer leftPointer;
    private VRTK_Pointer rightPointer;
    
    private VRTK_StraightPointerRenderer leftPointerRenderer;
    private VRTK_StraightPointerRenderer rightPointerRenderer;
    
    protected void Start() => EventsManager.instance.Add(this);

    protected void OnDestroy()
    {
        var manager = EventsManager.instance;
        if (manager) manager.Remove(this);
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
        leftPointer = leftController.GetComponent<VRTK_Pointer>();
        rightPointer = rightController.GetComponent<VRTK_Pointer>();
        leftPointerRenderer = leftController.GetComponent<VRTK_StraightPointerRenderer>();
        rightPointerRenderer = rightController.GetComponent<VRTK_StraightPointerRenderer>();

        ToggleRenderer(false);
    }

    private void ToggleRenderer(bool newState, bool bothHands = false)
    {
        rightPointer.enableTeleport = newState;

        VRTK_BasePointerRenderer.VisibilityStates visibilityState = newState
            ? VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn
            : VRTK_BasePointerRenderer.VisibilityStates.AlwaysOff;
        
        rightPointerRenderer.tracerVisibility = visibilityState;
        rightPointerRenderer.cursorVisibility = visibilityState;

        if (!bothHands && newState) return;
        
        leftPointer.enableTeleport = newState;
        
        leftPointerRenderer.tracerVisibility = visibilityState;
        leftPointerRenderer.cursorVisibility = visibilityState;
    }

    public void On(OnRevealEvent reveal) => ToggleRenderer(true);

    public void On(OnTeleportEvent teleport)
    {
        LightSection lightSection = teleport.navpoint.GetComponentInParent<LightSection>();
        
        ToggleRenderer(lightSection && lightSection.isRevealed);
    }
}
