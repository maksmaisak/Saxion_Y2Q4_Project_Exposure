using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class Questionnaire : MyBehaviour, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] GameObject objectsParent;
    [SerializeField] QuestionnaireButton[] buttons;
    [SerializeField] float startDelay = 4.0f;
    [SerializeField] float delayBetweenQuestionPanels = 2.0f;
    [SerializeField] float showButtonsDelay = 1.0f;
    [SerializeField] float showButtonsPerButtonDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] AudioClip changePanelSound;
    [SerializeField] AudioClip buttonUseSound;
    [SerializeField] AudioClip buttonAppearSound;

    [Header("Debug")] 
    [SerializeField] bool showOnStart;

    private Navpoint[] navpoints;
    private QuestionnairePanel[] questionPanels;
    
    private int lastPressedButtonIndex = -1;

    private AudioSource audioSource;
    
    protected override void Awake()
    {
        base.Awake();

        audioSource = GetComponent<AudioSource>();
        Assert.IsNotNull(audioSource);

        navpoints = FindObjectsOfType<Navpoint>();
        questionPanels = GetComponentsInChildren<QuestionnairePanel>();

        for (int i = 0; i < buttons.Length; ++i)
        {
            int index = i;
            QuestionnaireButton button = buttons[i];
            button.onActivate.AddListener(() => lastPressedButtonIndex = index);
        }

        Assert.IsNotNull(objectsParent);
        if (!showOnStart)
            objectsParent.SetActive(false);
    }

    public void On(OnTeleportEvent message)
    {
        if (!navpoints.All(n => n.isUsed)) 
            return;
        
        objectsParent.SetActive(true);
        Play();
    }

    public Coroutine Play()
    {
        return StartCoroutine(PlayCoroutine());
    }

    private IEnumerator PlayCoroutine()
    {
        yield return new WaitForSeconds(startDelay);
        
        int[] answers = new int[questionPanels.Length];
        for (int i = 0; i < questionPanels.Length; ++i)
        {
            questionPanels[i].Show();

            yield return new WaitForSeconds(showButtonsDelay);
            yield return ShowButtons().WaitForCompletion();
            
            lastPressedButtonIndex = -1;
            yield return new WaitUntil(() => lastPressedButtonIndex != -1);
            answers[i] = lastPressedButtonIndex + 1;
            
            yield return HideButtons().WaitForCompletion();

            questionPanels[i].Hide();
            
            yield return new WaitForSeconds(delayBetweenQuestionPanels);
        }

        HideButtons();
        
        AddToFile(answers);
    }

    [ContextMenu("Find buttons")]
    private void FindButtons()
    {
        buttons = GetComponentsInChildren<QuestionnaireButton>();
    }

    private Sequence ShowButtons()
    {
        this.DOKill();
        var sequence = DOTween
            .Sequence()
            .SetTarget(this);

        for (int i = 0; i < buttons.Length; ++i)
        {
            float delay = showButtonsPerButtonDelay * Mathf.Abs(buttons.Length * 0.5f - 0.5f - i);
            sequence.Join(DOTween.Sequence()
                .AppendInterval(delay)
                .AppendCallback(buttons[i].Show)
                .AppendCallback(() => { audioSource.clip = buttonAppearSound; audioSource.Play(); })
            );
        }

        return sequence;
    }

    private Sequence HideButtons()
    {
        this.DOKill();
        var sequence = DOTween.Sequence().SetTarget(this);

        foreach (QuestionnaireButton button in buttons)
            sequence.AppendCallback(button.Hide);

        return sequence;
    }
    
    private void AddToFile(int[] answers)
    {
        const string filepath = "questionnaire.csv";
        bool isFirstTime = !File.Exists(filepath);
        using (var sw = File.AppendText(filepath))
        {
            if (isFirstTime)
            {
                sw.Write("Date,");
                for (int i = 0; i < questionPanels.Length; ++i)
                {
                    sw.Write(questionPanels[i].GetText());
                    if (i + 1 < questionPanels.Length)
                        sw.Write(",");
                }
                sw.WriteLine();
            }
            
            sw.Write(DateTime.Now);
            sw.Write(",");
            for (int i = 0; i < answers.Length; ++i)
            {
                sw.Write(answers[i]);
                if (i + 1 < answers.Length)
                    sw.Write(",");
            }
            sw.WriteLine();
        }
    }
}