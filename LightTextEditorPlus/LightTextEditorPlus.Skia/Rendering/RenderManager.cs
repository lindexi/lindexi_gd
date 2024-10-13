using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

class RenderManager: IRenderManager
{
    record SkiaTextRenderInfo();

    private List<SkiaTextRenderInfo>? RenderInfoList { set; get; }

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo lineInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                // 先不考虑缓存
                LineDrawingArgument argument = lineInfo.Argument;
            }
        }
    }

    public void Render(SKCanvas canvas)
    {
        if (RenderInfoList is null)
        {
            return;
        }

        foreach (SkiaTextRenderInfo skiaTextRenderInfo in RenderInfoList)
        {

        }
    }
}