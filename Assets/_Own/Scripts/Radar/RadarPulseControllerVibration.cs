using UnityEngine;
using UnityEngine.Assertions;
using VRTK;

public class RadarPulseControllerVibration : MonoBehaviour
{
    [SerializeField] VRTK_InteractableObject interactableObject;
    [SerializeField] RadarTool radarTool;
    [Space] 
    [SerializeField] float vibrationStrength = 1.0f;
    [SerializeField] float vibrationDuration = 0.5f;
    [SerializeField] float vibrationPulseInterval = 0.05f;
    
    void Start()
    {
        interactableObject = interactableObject ? interactableObject : GetComponent<RadarController>();
        radarTool = radarTool ? radarTool : GetComponent<RadarTool>();
        
        Assert.IsNotNull(interactableObject);
        Assert.IsNotNull(radarTool);
        
        radarTool.onPulse.AddListener(OnPulse);
    }

    private void OnPulse()
    { 
        GameObject grabbingObject = interactableObject.GetGrabbingObject();
        if (!grabbingObject)
            return;
        
        VRTK_ControllerReference controllerReference = VRTK_DeviceFinder.IsControllerLeftHand(grabbingObject)
            ? VRTK_DeviceFinder.GetControllerReferenceLeftHand()
            : VRTK_DeviceFinder.GetControllerReferenceRightHand();
        if (controllerReference == null)
            return;
        
        VRTK_ControllerHaptics.TriggerHapticPulse(
            controllerReference, 
            vibrationStrength, vibrationDuration, vibrationPulseInterval
        );
    }
}