using System;
using UnityEngine;

public class AdaptiveDifficulty : Singleton<AdaptiveDifficulty>, IEventReceiver<OnWavesphereDestroyed>
{
    private int numCaught;
    private int numMissed;
    
    [SerializeField] [Range(0.0f, 1.0f)] float _difficulty = 0.5f;

    [SerializeField] float difficultyIncreasePerCaughtWavesphere = 0.02f;
    [SerializeField] float difficultyDecreasePerMissedWavesphere = 0.05f;
    
    public float difficulty => _difficulty;

    public void On(OnWavesphereDestroyed message)
    {
        if (message.wasCaught)
        {
            numCaught += 1;
            _difficulty = Mathf.MoveTowards(_difficulty, 1.0f, difficultyIncreasePerCaughtWavesphere);
        }
        else
        {
            numMissed += 1;
            _difficulty = Mathf.MoveTowards(_difficulty, 0.0f, difficultyDecreasePerMissedWavesphere);
        }
    }
}