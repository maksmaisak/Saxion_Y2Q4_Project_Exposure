using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

[Serializable]
public class OutlinePass : ScriptableRenderPass
{    
    [SerializeField] Material outlineMaterial;
    
    private FilteringSettings outlineFilterSettings;
    private readonly int OutlineColorId;

    public OutlinePass(Color outlineColor)
    {
        outlineMaterial = CoreUtils.CreateEngineMaterial("Unlit/SimpleOutline");

        OutlineColorId = Shader.PropertyToID("_OutlineColor");
        outlineMaterial.SetColor(OutlineColorId, outlineColor);

        outlineFilterSettings = new FilteringSettings {
            renderQueueRange = RenderQueueRange.opaque
        };

        this.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
        DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("LightweightForward"), ref renderingData, sortingCriteria);
        drawSettings.overrideMaterial = outlineMaterial;

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref outlineFilterSettings);
    }
}