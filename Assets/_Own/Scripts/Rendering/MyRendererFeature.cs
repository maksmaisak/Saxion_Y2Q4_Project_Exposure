using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

[CreateAssetMenu(fileName = "MyRendererFeature", menuName = "Rendering/MyRendererFeature", order = CoreUtils.assetCreateMenuPriority1)]
public class MyRendererFeature : ScriptableRendererFeature
{
    [SerializeField] OutlinePass pass;
    
    public override void Create()
    {
        pass = new OutlinePass(Color.yellow);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}
