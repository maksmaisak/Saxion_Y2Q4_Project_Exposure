
public class OnWavesphereCaught : BroadcastEvent<OnWavesphereCaught>
{
    public readonly Wavesphere wavesphere;
    public OnWavesphereCaught(Wavesphere wavesphere) { this.wavesphere = wavesphere; }
}