using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    [SerializeField] GameObject objectsParent;
    [SerializeField] VRButton restartButton;
    [SerializeField] float startDelay = 4.0f;
    [SerializeField] float showButtonsDelay = 1.0f;
    
    [Header("Debug")] 
    [SerializeField] bool showOnStart;

    private MyPanel creditsPanel;


    private void Awake()
    {
        creditsPanel = GetComponentInChildren<MyPanel>();
        
        objectsParent.SetActive(true);
        restartButton.onActivate.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        
        Assert.IsNotNull(objectsParent);
        
        if(!showOnStart)
            objectsParent.SetActive(false);
    }
    
    private void Play() => StartCoroutine(PlayCoroutine());

    private IEnumerator PlayCoroutine()
    {
        yield return new WaitForSeconds(startDelay);
        
        creditsPanel.Show();
        
        yield return new WaitForSeconds(showButtonsDelay);
        ShowButton();

    }

    public void Activate()
    {
        objectsParent.SetActive(true);
        Play();
    }
    
    private void ShowButton() => restartButton.Show();
}
