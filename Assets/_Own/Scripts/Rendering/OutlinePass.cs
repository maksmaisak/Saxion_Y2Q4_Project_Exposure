using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

[Serializable]
public class OutlinePass : ScriptableRenderPass
{    
    [SerializeField] Material outlineMaterial;
    
    private FilteringSettings outlineFilterSettings;
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");

    public OutlinePass(Color outlineColor)
    {
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        
        outlineMaterial = outlineMaterial ? outlineMaterial : CoreUtils.CreateEngineMaterial("Unlit/SimpleOutline");
        Assert.IsNotNull(outlineMaterial);
        outlineMaterial.SetColor(OutlineColorId, outlineColor);
        outlineMaterial.SetFloat(ThicknessId, 1.0f);

        outlineFilterSettings = new FilteringSettings(null) {
            renderQueueRange = RenderQueueRange.opaque
        };
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
        DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("LightweightForward"), ref renderingData, sortingCriteria);
        drawSettings.overrideMaterial = outlineMaterial;

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref outlineFilterSettings);
    }
}