using System.Collections;
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
    
    [Header("Audio")]
    [SerializeField] AudioClip panelAppearSound;
    [SerializeField] AudioClip buttonAppearSound;
    
    [Header("Debug")] 
    [SerializeField] bool showOnStart;
    [SerializeField] bool showOnReveal;

    private MyPanel creditsPanel;
    private AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        
        audioSource = GetComponent<AudioSource>();
        Assert.IsNotNull(audioSource);

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
        audioSource.clip = panelAppearSound;
        audioSource.Play();
        
        yield return new WaitForSeconds(showButtonDelay);
        
        restartButton.Show();
        audioSource.clip = buttonAppearSound;
        audioSource.Play();
    }

    private void RestartScene()
    {
        restartButton.Hide();

        this.Delay(timeToRestartScene, () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }
}
