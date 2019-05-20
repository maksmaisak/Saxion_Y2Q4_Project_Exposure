using System;
using UnityEditor;

[CustomEditor(typeof(DotsManager))]
public class DotsManagerEditor : Editor
{
    private void OnSceneGUI()
    {
        ((DotsManager)target).DrawDebugInfoInEditor();
    }
}