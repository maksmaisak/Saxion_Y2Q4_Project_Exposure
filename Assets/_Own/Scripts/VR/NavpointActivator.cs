using UnityEngine;
using VRTK;
using VRTK.UnityEventHelper;

public class NavpointActivator : MonoBehaviour
{
    [SerializeField] VRTK_DestinationMarker pointer;
    
    void OnEnable()
    {
        pointer = (pointer == null ? GetComponent<VRTK_DestinationMarker>() : pointer);

        if (pointer != null)
        {
            pointer.DestinationMarkerEnter += DestinationMarkerEnter;
            pointer.DestinationMarkerExit  += DestinationMarkerExit;
        }
        else
        {
            VRTK_Logger.Error(VRTK_Logger.GetCommonMessage(VRTK_Logger.CommonMessageKeys.REQUIRED_COMPONENT_MISSING_FROM_GAMEOBJECT, "PointerEventListener", "VRTK_DestinationMarker", "the Controller Alias"));
        }
    }

    void OnDisable()
    {
        if (pointer != null)
        {
            pointer.DestinationMarkerEnter -= DestinationMarkerEnter;
            pointer.DestinationMarkerExit  -= DestinationMarkerExit;
        }
    }

    private void DestinationMarkerEnter(object sender, DestinationMarkerEventArgs e)
    {
        foreach (Navpoint navpoint in e.target.GetComponentsInParent<Navpoint>())
        {
            navpoint.SetFilling(true);
        }
    }

    private void DestinationMarkerExit(object sender, DestinationMarkerEventArgs e)
    {
        foreach (Navpoint navpoint in e.target.GetComponentsInParent<Navpoint>())
        {
            navpoint.SetFilling(false);
        }
    }
}