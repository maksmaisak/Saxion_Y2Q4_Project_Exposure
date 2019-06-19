using UnityEditor;
using UnityEngine;
using System.IO;

public class RenderToImage : MonoBehaviour
{
    [SerializeField] Vector2Int resolution = new Vector2Int(5120, 2160);
    [SerializeField] string filepath = "image.png";

    void OnValidate()
    {
        string path = filepath.Split('.')[0];
        if (string.IsNullOrEmpty(path))
            path = "image";        
        filepath = path + ".png";
    }

    [ContextMenu("SaveToFile")]
    public void SaveToFile()
    {
        var targetCamera = GetCamera();
        if (!targetCamera)
            return;

        RenderTexture renderTexture = RenderToTexture(targetCamera);
        Texture2D texture = ToTexture2D(renderTexture);
        WriteToFile(texture);
    }

    private Camera GetCamera()
    {
        var cam = GetComponent<Camera>();
        if (cam) 
            return cam;

        return Camera.main;
    }

    private RenderTexture RenderToTexture(Camera camera)
    {   
        var renderTexture = new RenderTexture(resolution.x, resolution.y, 24);
        
        RenderTexture previousTarget = camera.targetTexture;
        camera.targetTexture = renderTexture;
        camera.Render();
        camera.targetTexture = previousTarget;
        return renderTexture;
    }

    private Texture2D ToTexture2D(RenderTexture renderTexture)
    {
        var previouslyActive = RenderTexture.active;
        RenderTexture.active = renderTexture;
        
        var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0, false);
        
        RenderTexture.active = previouslyActive;

        return texture;
    }

    private void WriteToFile(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(filepath, bytes);
    }
}