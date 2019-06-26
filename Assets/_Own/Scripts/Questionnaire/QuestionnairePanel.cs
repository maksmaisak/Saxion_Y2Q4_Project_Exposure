using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class QuestionnairePanel : MonoBehaviour
{
    [SerializeField] FadeZoom fadeZoom;
    [SerializeField] CanvasGroup canvasGroup;
    
    public void Show()
    {
        fadeZoom.FadeIn(canvasGroup, transform);
        canvasGroup.interactable = canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        fadeZoom.FadeOut(canvasGroup, transform);
        canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
    }

    public string GetText()
    {
        var text = GetComponentInChildren<TMP_Text>();
        Assert.IsNotNull(text);
        return text.text;
    }
}