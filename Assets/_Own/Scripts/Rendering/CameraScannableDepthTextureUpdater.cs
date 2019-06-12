using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

public class CameraScannableDepthTextureUpdater : MonoBehaviour
{
    private new Camera camera;
    private RenderTexture renderTexture;
    
    [SerializeField] Texture2D texture2D;
    [SerializeField] Shader depthOnlyShader;

    void Start()
    {
        camera = camera ? camera : GetComponentInChildren<Camera>();
        camera.depthTextureMode |= DepthTextureMode.Depth;
        renderTexture = camera.targetTexture;
        Assert.IsNotNull(renderTexture);

        if (!texture2D)
            texture2D = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGBA32, false);
    }

    void Update()
    {
        Assert.IsNotNull(camera);
        
        camera.SetTargetBuffers(renderTexture.colorBuffer, renderTexture.depthBuffer);
        camera.RenderWithShader(depthOnlyShader, null);
    }
}
