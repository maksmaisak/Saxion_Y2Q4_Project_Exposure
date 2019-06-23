using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RenderToImage))]
public class RenderToImageEditor : Editor
{
    public override void OnInspectorGUI()
    {    
        DrawDefaultInspector();

        var renderToImage = (RenderToImage)target;
        if (GUILayout.Button("Save to file"))
            renderToImage.SaveToFile();
    }
}