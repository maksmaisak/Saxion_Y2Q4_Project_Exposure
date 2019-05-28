 public class OnTeleportEvent : BroadcastEvent<OnTeleportEvent>
 {
     public readonly NavpointUIElement navpoint;

     public OnTeleportEvent(NavpointUIElement navpoint) => this.navpoint = navpoint;
 }