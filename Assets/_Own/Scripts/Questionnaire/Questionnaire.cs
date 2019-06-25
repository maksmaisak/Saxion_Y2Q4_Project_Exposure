using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

public class Questionnaire : MyBehaviour, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] GameObject objectsParent;
    [SerializeField] QuestionnaireButton[] buttons;
    [SerializeField] float showButtonsDelay = 1.0f;
    [SerializeField] float showButtonsPerButtonDelay = 0.5f;

    [Header("Debug")] 
    [SerializeField] bool showOnStart;

    private Navpoint[] navpoints;
    private QuestionnairePanel[] questionPanels;
    
    private int lastPressedButtonIndex = -1;

    protected override void Awake()
    {
        base.Awake();

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
        yield return new WaitForSeconds(2.0f);
        
        int[] answers = new int[questionPanels.Length];
        bool areButtonsShown = false;
        for (int i = 0; i < questionPanels.Length; ++i)
        {
            yield return new WaitForSeconds(2.0f);

            questionPanels[i].Show();

            if (!areButtonsShown)
            {
                yield return ShowButtons().WaitForCompletion();
                areButtonsShown = true;
            }

            lastPressedButtonIndex = -1;
            yield return new WaitUntil(() => lastPressedButtonIndex != -1);
            answers[i] = lastPressedButtonIndex + 1;

            if (i + 1 < questionPanels.Length)
                this.Delay(2.0f, buttons[lastPressedButtonIndex].Show);

            questionPanels[i].Hide();
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
            .SetTarget(this)
            .AppendInterval(showButtonsDelay);

        for (int i = 0; i < buttons.Length; ++i)
        {
            float delay = showButtonsPerButtonDelay * Mathf.Abs(buttons.Length * 0.5f - 0.5f - i);
            sequence.Join(DOTween.Sequence()
                .AppendInterval(delay)
                .AppendCallback(buttons[i].Show)
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