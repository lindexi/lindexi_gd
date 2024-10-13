using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

using SkiaSharp;

namespace LightTextEditorPlus;

public partial class SkiaTextEditor : IRenderManager, ITextEditorSkiaRender
{
    public SkiaTextEditor(PlatformProvider? platformProvider = null)
    {
        var skiaTextEditorPlatformProvider = platformProvider ?? new SkiaTextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(skiaTextEditorPlatformProvider);
    }

    public TextEditorCore TextEditorCore { get; }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        if (renderInfoProvider.IsDirty)
        {
            return;
        }

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
        }

        RenderRequested?.Invoke(this, EventArgs.Empty);
    }

    public ITextEditorSkiaRender GetCurrentRender()
    {
        return this;
    }

    public event EventHandler? RenderRequested;

    public void Render(SKCanvas canvas)
    {

    }
}

public interface ITextEditorSkiaRender
{
    void Render(SKCanvas canvas);
}

public class SkiaTextEditorPlatformProvider : PlatformProvider
{
    public SkiaTextEditorPlatformProvider(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private SkiaTextEditor TextEditor { get; }

    public override IRenderManager? GetRenderManager()
    {
        return TextEditor;
    }
}