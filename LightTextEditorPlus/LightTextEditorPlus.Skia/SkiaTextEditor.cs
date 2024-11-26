using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Rendering;

namespace LightTextEditorPlus;

public partial class SkiaTextEditor : IRenderManager
{
    public SkiaTextEditor(PlatformProvider? platformProvider = null)
    {
        var skiaTextEditorPlatformProvider = platformProvider ?? new SkiaTextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(skiaTextEditorPlatformProvider);

        RenderManager = new RenderManager(this);

#if DEBUG
        DocumentManager.SetDefaultTextRunProperty<SkiaTextRunProperty>(property => property with
        {
            FontName = new FontName("微软雅黑"),
            FontSize = 50,
        });
#endif
    }

    internal RenderManager RenderManager { get; }
    private DocumentManager DocumentManager => TextEditorCore.DocumentManager;

    public TextEditorCore TextEditorCore { get; }

    /// <summary>
    /// 获取文档的布局尺寸，实际布局尺寸
    /// </summary>
    public Rect CurrentLayoutBounds { get; private set; } = Rect.Zero;

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        if (renderInfoProvider.IsDirty)
        {
            return;
        }

        CurrentLayoutBounds = TextEditorCore.GetDocumentLayoutBounds();
        
        RenderManager.Render(renderInfoProvider);

        RenderRequested?.Invoke(this, EventArgs.Empty);
    }

    public ITextEditorSkiaRender GetCurrentTextRender()
    {
        return RenderManager.GetCurrentRender();
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

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        var skiaPlatformFontManager = new SkiaPlatformFontManager();
        return new SkiaPlatformRunPropertyCreator(skiaPlatformFontManager);
    }

    public override IRenderManager GetRenderManager()
    {
        return TextEditor;
    }

    public override ISingleCharInLineLayouter? GetSingleRunLineLayouter()
    {
        return new SkiaSingleCharInLineLayouter(TextEditor);
    }

    public override ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return new SkiaCharInfoMeasurer(TextEditor);
    }
}