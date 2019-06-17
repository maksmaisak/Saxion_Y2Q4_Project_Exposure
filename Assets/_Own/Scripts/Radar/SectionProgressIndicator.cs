using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SectionProgressIndicator : MyBehaviour, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] Image indicator;
    [SerializeField] float maxProgressPerSecond = 1.0f;

    private LightSection currentSection;

    private Sequence updateFillSequence;
    private float targetFillAmount;

    void Start()
    {
        Assert.IsNotNull(indicator);
        indicator.fillAmount = targetFillAmount = 0.0f;

        currentSection = GetFirstSection();
        Assert.IsNotNull(currentSection);
    }

    void Update()
    {
        if (!currentSection)
        {
            indicator.fillAmount = targetFillAmount = 0.0f;
            return;
        }

        if (currentSection.isRevealed)
        {
            indicator.fillAmount = targetFillAmount = 1.0f;
            return;
        }

        float currentRevealProgress = currentSection.GetRevealProgress();
        if (Mathf.Approximately(targetFillAmount, currentRevealProgress)) 
            return;

        targetFillAmount = currentRevealProgress;
        
        updateFillSequence?.Kill();
        updateFillSequence = DOTween.Sequence()
            .Join(indicator.DOFillAmount(targetFillAmount, Mathf.Abs(targetFillAmount - indicator.fillAmount) / maxProgressPerSecond))
            .Join(indicator.transform.DOPunchScale(-0.1f * indicator.transform.localScale, 0.5f, vibrato: 1))
            .OnComplete(() => updateFillSequence = null);
    }

    public void On(OnTeleportEvent message)
    {
        updateFillSequence?.Kill();
        updateFillSequence = null;

        var section = message.navpoint.GetComponentInParent<LightSection>();
        Assert.IsNotNull(section);
        
        currentSection = section;
        indicator.fillAmount = section.GetRevealProgress();
    }

    private LightSection GetFirstSection()
    {
        var lightSections = FindObjectsOfType<LightSection>()
            .Select(ls => (section: ls, navpoints: ls.GetComponentsInChildren<Navpoint>()))
            .ToArray();
        Assert.AreNotEqual(0, lightSections.Length);

        var section = lightSections.FirstOrDefault(tuple => !tuple.navpoints.Any()).section;
        if (section)
            return section;
        
        Vector3 position = transform.position;
        return lightSections.ArgMin(tuple => (position - tuple.section.transform.position).sqrMagnitude).section;
    }
}