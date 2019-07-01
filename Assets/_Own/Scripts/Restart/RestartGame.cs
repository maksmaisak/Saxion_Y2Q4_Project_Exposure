﻿using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class RestartGame : MyBehaviour, IEventReceiver<OnRevealEvent>
{
    [SerializeField] GameObject objectsParent;
    [SerializeField] VRButton restartButton;
    [SerializeField] float startDelay = 4.0f;
    [SerializeField] float showButtonDelay = 1.0f;
    [SerializeField] float timeToRestartScene = 1.5f;
    
    [Header("Debug")] 
    [SerializeField] bool showOnStart;
    [SerializeField] bool showOnReveal;

    private MyPanel creditsPanel;

    protected override void Awake()
    {
        base.Awake();

        creditsPanel = GetComponentInChildren<MyPanel>();
        
        objectsParent.SetActive(true);
        restartButton.onActivate.AddListener(RestartScene);
        
        Assert.IsNotNull(objectsParent);
        
        if(!showOnStart)
            objectsParent.SetActive(false);
    }

    public void On(OnRevealEvent message)
    {
        if (showOnReveal)
            Activate();
    }

    public void Activate()
    {
        objectsParent.SetActive(true);
        Play();
    }

    private void Play() => StartCoroutine(PlayCoroutine());

    private IEnumerator PlayCoroutine()
    {
        yield return new WaitForSeconds(startDelay);
        
        creditsPanel.Show();
        
        yield return new WaitForSeconds(showButtonDelay);
        restartButton.Show();
    }

    private void RestartScene()
    {
        restartButton.Hide();

        this.Delay(timeToRestartScene, () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }
}
