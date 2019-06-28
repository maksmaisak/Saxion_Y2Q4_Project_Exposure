
public class OnWavesphereDestroyed : BroadcastEvent<OnWavesphereDestroyed>
{
    public readonly Wavesphere wavesphere;
    public readonly bool wasCaught;

    public OnWavesphereDestroyed(Wavesphere wavesphere, bool wasCaught)
    {
        this.wavesphere = wavesphere;
        this.wasCaught = wasCaught;
    }
}