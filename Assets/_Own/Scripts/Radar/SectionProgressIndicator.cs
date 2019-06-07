using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SectionProgressIndicator : MyBehaviour, IEventReceiver<OnTeleportEvent>
{
    [SerializeField] Image indicator;

    private LightSection currentSection;

    void Start()
    {
        Assert.IsNotNull(indicator);
        indicator.fillAmount = 0.0f;

        currentSection = GetFirstSection();
        Assert.IsNotNull(currentSection);
    }

    void Update()
    {
        indicator.fillAmount = currentSection.GetRevealProgress();
    }

    public void On(OnTeleportEvent message)
    {
        var section = message.navpoint.GetComponentInParent<LightSection>();
        Assert.IsNotNull(section);
        
        currentSection = section;
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