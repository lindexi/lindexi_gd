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

namespace LightTextEditorPlus;

public partial class SkiaTextEditor : IRenderManager
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
        return RenderManager;
    }

    public event EventHandler? RenderRequested;
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