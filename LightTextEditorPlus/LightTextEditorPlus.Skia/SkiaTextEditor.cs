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
    /// 日志
    /// </summary>
    public ITextLogger Logger => TextEditorCore.Logger;

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

        InternalRenderCompleted?.Invoke(this, EventArgs.Empty);

        InvalidateVisualRequested?.Invoke(this, EventArgs.Empty);

        _renderCompletionSource.TrySetResult();
    }

    public ITextEditorSkiaRender GetCurrentTextRender()
    {
        return RenderManager.GetCurrentTextRender();
    }

    public ITextEditorCaretAndSelectionRenderSkiaRender GetCurrentCaretAndSelectionRender()
    {
        return RenderManager.GetCurrentCaretAndSelectionRender();
    }

    public event EventHandler? InvalidateVisualRequested;

    internal event EventHandler? InternalRenderCompleted;

    /// <summary>
    /// 等待渲染完成
    /// </summary>
    /// <returns></returns>
    public Task WaitForRenderCompletedAsync()
    {
        if (_renderCompletionSource.Task.IsCompleted && TextEditorCore.IsDirty)
        {
            // 已经完成渲染，但是当前的文档又是脏的。那就是需要重新等待渲染
            _renderCompletionSource = new TaskCompletionSource();
        }

        return _renderCompletionSource.Task;
    }
    private TaskCompletionSource _renderCompletionSource = new TaskCompletionSource();
}

public class SkiaTextEditorPlatformProvider : PlatformProvider
{
    public SkiaTextEditorPlatformProvider(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
        _skiaPlatformFontManager = new SkiaPlatformResourceManager(TextEditor);
    }

    private SkiaTextEditor TextEditor { get; }

    private readonly SkiaPlatformResourceManager _skiaPlatformFontManager;

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        return new SkiaPlatformRunPropertyCreator(_skiaPlatformFontManager, TextEditor.Logger);
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