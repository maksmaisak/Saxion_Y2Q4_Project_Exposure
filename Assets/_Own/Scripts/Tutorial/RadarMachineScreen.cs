using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class RadarMachineScreen : MyBehaviour, IEventReceiver<OnHighlightEvent>
{
    [SerializeField] Transform dotsTransform;
    [SerializeField] new ParticleSystem particleSystem;
    [Space] 
    [SerializeField] Rect cullRect = new Rect(-0.23f, -0.23f, 0.46f, 0.46f);
    [SerializeField] RangeFloat cullRangeVertical = new RangeFloat(-1.0f, 1.0f);

    void Start()
    {
        dotsTransform = dotsTransform ? dotsTransform : transform;
        particleSystem = particleSystem ? particleSystem : GetComponentInChildren<ParticleSystem>();
        Assert.IsNotNull(particleSystem);
    }
    
    public void On(OnHighlightEvent message)
    {
        Vector3[] positions = message.dotPositions
            .Select(WorldToRadarScreenPosition)
            .Where(IsWithinRadarScreen)
            .Select(v => new Vector3(v.x, v.y, 0.0f))
            .ToArray();
        
        particleSystem.AddParticles(positions);
    }

    private Vector3 WorldToRadarScreenPosition(Vector3 worldspacePosition)
    {
        Vector3 radarScreenPosition = dotsTransform.position;
        Vector3 relativePosition = worldspacePosition - radarScreenPosition;

        Vector3 right   = Vector3.right;
        Vector3 forward = Vector3.forward;
        Vector3 up = Vector3.up;

        return new Vector3(
            Vector3.Dot(relativePosition, right  ) * 0.05f,
            Vector3.Dot(relativePosition, forward) * 0.05f,
            Vector3.Dot(relativePosition, up     )
        );
    }
    
    private bool IsWithinRadarScreen(Vector3 position)
    {
        return cullRect.Contains(position) && cullRangeVertical.Contains(position.z);
    }
}