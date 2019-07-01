
public class OnWavesphereMissed : BroadcastEvent<OnWavesphereMissed>
{
    public readonly Wavesphere wavesphere;
    public OnWavesphereMissed(Wavesphere wavesphere) { this.wavesphere = wavesphere; }
}