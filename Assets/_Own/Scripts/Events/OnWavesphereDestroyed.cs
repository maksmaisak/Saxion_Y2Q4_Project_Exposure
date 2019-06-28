
public class OnWavesphereDestroyed : BroadcastEvent<OnWavesphereDestroyed>
{
    public readonly Wavesphere wavesphere;
    public readonly Wavesphere.ReasonForDestruction reason;

    public OnWavesphereDestroyed(Wavesphere wavesphere, Wavesphere.ReasonForDestruction reason)
    {
        this.wavesphere = wavesphere;
        this.reason = reason;
    }
}