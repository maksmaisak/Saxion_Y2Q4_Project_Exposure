using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using VRTK.Controllables;
using VRTK.Controllables.PhysicsBased;

public class Questionnaire : MyBehaviour, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] VRTK_PhysicsPusher[] buttons;
    
    private Navpoint[] navpoints;
    private QuestionnairePanel[] questionPanels;
    
    private int currentAnswer = -1;

    void Start()
    {
        navpoints = FindObjectsOfType<Navpoint>();
        questionPanels = GetComponentsInChildren<QuestionnairePanel>();
        
        for (int i = 0; i < buttons.Length; ++i)
        {
            int answer = i + 1;
            var button = buttons[i];
            button.stayPressed = false;
            button.MaxLimitReached += (sender, args) =>
            {
                currentAnswer = answer;
                button.stayPressed = true;
                this.Delay(0.5f, () => button.stayPressed = false);
            };
        }

        // TEMP
        Play();
    }
    
    public void On(OnTeleportEvent message)
    {
        if (navpoints.All(n => n.isUsed))
            Play();
    }

    public Coroutine Play()
    {
        return StartCoroutine(PlayCoroutine());
    }

    private IEnumerator PlayCoroutine()
    {
        int[] answers = new int[questionPanels.Length];
        
        for (int i = 0; i < questionPanels.Length; ++i)
        {
            questionPanels[i].Show();
            
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
        buttons = GetComponentsInChildren<VRTK_PhysicsPusher>();
    }
    
    private void AddToFile(int[] answers)
    {
        const string filepath = "questionnaire.csv";
        bool isFirstTime = !File.Exists(filepath);
        using (var sw = new StreamWriter(filepath))
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