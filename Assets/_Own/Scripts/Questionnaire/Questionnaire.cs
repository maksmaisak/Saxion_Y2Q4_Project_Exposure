using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Questionnaire : MyBehaviour, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] QuestionnaireButton[] buttons;

    [Header("Debug")] 
    [SerializeField] bool showOnStart;
    
    private Navpoint[] navpoints;
    private QuestionnairePanel[] questionPanels;
    
    private int currentAnswer = -1;

    protected override void Awake()
    {
        base.Awake();
        
        if (!showOnStart)
            gameObject.SetActive(false);
    }

    void Start()
    {
        navpoints = FindObjectsOfType<Navpoint>();
        questionPanels = GetComponentsInChildren<QuestionnairePanel>();
        
        for (int i = 0; i < buttons.Length; ++i)
        {
            int answer = i + 1;
            QuestionnaireButton button = buttons[i];
            button.onActivate.AddListener(() =>
            {
                currentAnswer = answer;
                this.Delay(1.0f, button.Show);
            });
        }
        
        Play();
    }
    
    public void On(OnTeleportEvent message)
    {
        if (!navpoints.All(n => n.isUsed)) 
            return;
        
        gameObject.SetActive(true);
    }

    public Coroutine Play()
    {
        return StartCoroutine(PlayCoroutine());
    }

    private IEnumerator PlayCoroutine()
    {
        int[] answers = new int[questionPanels.Length];

        bool areButtonsShown = false;
        
        for (int i = 0; i < questionPanels.Length; ++i)
        {
            yield return new WaitForSeconds(2.0f);

            questionPanels[i].Show();

            if (!areButtonsShown)
            {
                foreach (QuestionnaireButton button in buttons)
                {
                    yield return new WaitForSeconds(0.4f);
                    button.Show();
                }
                areButtonsShown = true;
            }

            currentAnswer = -1;
            yield return new WaitUntil(() => currentAnswer != -1);
            answers[i] = currentAnswer;

            questionPanels[i].Hide();
        }

        AddToFile(answers);
    }

    [ContextMenu("Find buttons")]
    private void FindButtons()
    {
        buttons = GetComponentsInChildren<QuestionnaireButton>();
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