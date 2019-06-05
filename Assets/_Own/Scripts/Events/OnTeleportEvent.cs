 public class OnTeleportEvent : BroadcastEvent<OnTeleportEvent>
 {
     public readonly Navpoint navpoint;

     public OnTeleportEvent(Navpoint navpoint) => this.navpoint = navpoint;
 }