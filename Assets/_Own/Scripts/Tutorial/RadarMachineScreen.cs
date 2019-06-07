using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class RadarMachineScreen : MyBehaviour, IEventReceiver<OnHighlightEvent>
{
    [SerializeField] Transform dotsTransform;
    [SerializeField] new ParticleSystem particleSystem;
    [Space] 
    [SerializeField] Rect cullRect = new Rect(-0.23f, -0.23f, 0.46f, 0.46f);

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
            .ToArray();
        
        particleSystem.AddParticles(positions);
    }

    private Vector3 WorldToRadarScreenPosition(Vector3 worldspacePosition)
    {
        Vector3 radarScreenPosition = dotsTransform.position;
        Vector3 relativePosition = worldspacePosition - radarScreenPosition;

        Vector3 right   = Vector3.right;
        Vector3 forward = Vector3.forward;

        return new Vector3(
            Vector3.Dot(relativePosition, right  ) * 0.05f,
            Vector3.Dot(relativePosition, forward) * 0.05f,
            0.0f
        );
    }
    
    private bool IsWithinRadarScreen(Vector3 position)
    {
        return cullRect.Contains(position);
    }
}