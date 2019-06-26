using System.Collections;
using UnityEngine;
using VRTK;

public class QuestionnaireCameraFollow : MonoBehaviour
{
    [SerializeField] Transform followTransform;
    [SerializeField] float minDistanceFromCamera = 0.5f;
    [SerializeField] float maxDistanceFromCamera = 2.0f;
    
    private Transform cameraTransform;

    IEnumerator Start()
    {
        followTransform = followTransform ? followTransform : transform;
        yield return new WaitUntil(() => cameraTransform = VRTK_DeviceFinder.HeadsetCamera());
    }

    void LateUpdate()
    {
        if (!cameraTransform)
            return;

        Vector3 cameraPosition = cameraTransform.position;
        Vector3 fromCamera = followTransform.position - cameraPosition;
        Vector3 flatFromCamera = new Vector3(fromCamera.x, 0.0f, fromCamera.z);
        Vector3 desiredFlatFromCamera = GetDesiredFlatFromCamera(flatFromCamera);

        Vector3 newFlatFromCamera = Vector3.MoveTowards(flatFromCamera, desiredFlatFromCamera, 1.0f * Time.deltaTime);
        Vector3 newFromCamera = new Vector3(newFlatFromCamera.x, fromCamera.y, newFlatFromCamera.z);
        followTransform.position = cameraPosition + newFromCamera;
    }

    private Vector3 GetDesiredFlatFromCamera(Vector3 flatFromCamera)
    {
        float distance = flatFromCamera.magnitude;
        
        if (distance < minDistanceFromCamera)
            return minDistanceFromCamera * (flatFromCamera / distance);
        
        if (distance > maxDistanceFromCamera)
            return maxDistanceFromCamera * (flatFromCamera / distance);

        return flatFromCamera;
    }
}