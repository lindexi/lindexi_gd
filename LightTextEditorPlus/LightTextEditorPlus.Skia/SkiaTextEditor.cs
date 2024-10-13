using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Rendering;
using SkiaSharp;

namespace LightTextEditorPlus;

public partial class SkiaTextEditor : IRenderManager, ITextEditorSkiaRender
{
    public SkiaTextEditor(PlatformProvider? platformProvider = null)
    {
        var skiaTextEditorPlatformProvider = platformProvider ?? new SkiaTextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(skiaTextEditorPlatformProvider);
    }

    internal RenderManager RenderManager { get; } = new RenderManager();

    public TextEditorCore TextEditorCore { get; }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        if (renderInfoProvider.IsDirty)
        {
            return;
        }

        RenderManager.Render(renderInfoProvider);

        RenderRequested?.Invoke(this, EventArgs.Empty);
    }

    public ITextEditorSkiaRender GetCurrentRender()
    {
        return this;
    }

    public event EventHandler? RenderRequested;

    public void Render(SKCanvas canvas)
    {
        RenderManager.Render(canvas);
    }
}

/// <summary>
/// 文本的 Skia 渲染器
/// </summary>
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