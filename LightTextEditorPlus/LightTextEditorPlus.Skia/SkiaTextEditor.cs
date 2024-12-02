using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Rendering;

namespace LightTextEditorPlus;

/// <summary>
/// 使用 Skia 渲染承载的文本编辑器
/// </summary>
/// 这是一个分部类，相关实现代码在：
/// - API 定义层： API\[Skia]TextEditor.*.cs
public partial class SkiaTextEditor : IRenderManager
{
    public SkiaTextEditor(PlatformProvider? platformProvider = null)
    {
        var skiaTextEditorPlatformProvider = platformProvider ?? new SkiaTextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(skiaTextEditorPlatformProvider);

        RenderManager = new RenderManager(this);
        TextEditorCore.CurrentSelectionChanged += TextEditorCore_CurrentSelectionChanged;

//#if DEBUG
//        DocumentManager.SetDefaultTextRunProperty<SkiaTextRunProperty>(property => property with
//        {
//            FontName = new FontName("微软雅黑"),
//            FontSize = 50,
//        });
//#endif
    }

    private DocumentManager DocumentManager => TextEditorCore.DocumentManager;

    public TextEditorCore TextEditorCore { get; }

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger => TextEditorCore.Logger;

    /// <summary>
    /// 获取文档的布局尺寸，实际布局尺寸
    /// </summary>
    public TextRect CurrentLayoutBounds { get; private set; } = TextRect.Zero;

    #region 渲染
    internal RenderManager RenderManager { get; }

    private void TextEditorCore_CurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        if (TextEditorCore.IsDirty)
        {
            // 如果是脏的，那就不需要更新光标和选择区域的渲染，等待后续自动进入渲染
            return;
        }

        RenderInfoProvider renderInfoProvider = TextEditorCore.GetRenderInfo();
        RenderManager.UpdateCaretAndSelectionRender(renderInfoProvider, e.NewValue);
        OnInvalidateVisualRequested();
    }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        if (renderInfoProvider.IsDirty)
        {
            return;
        }

        CurrentLayoutBounds = TextEditorCore.GetDocumentLayoutBounds();

        RenderManager.Render(renderInfoProvider);

        InternalRenderCompleted?.Invoke(this, EventArgs.Empty);

        OnInvalidateVisualRequested();

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

    private void OnInvalidateVisualRequested()
    {
        InvalidateVisualRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
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

    private SkiaPlatformRunPropertyCreator? _skiaPlatformRunPropertyCreator;

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        return _skiaPlatformRunPropertyCreator ??= new SkiaPlatformRunPropertyCreator(_skiaPlatformFontManager, TextEditor);
    }

    public override IRenderManager GetRenderManager()
    {
        return TextEditor;
    }

    public override ISingleCharInLineLayouter GetSingleRunLineLayouter()
    {
        return new SkiaSingleCharInLineLayouter(TextEditor);
    }

    public override ICharInfoMeasurer GetCharInfoMeasurer()
    {
        Debug.Fail($"已重写 GetSingleRunLineLayouter 方法，不应再进入 GetCharInfoMeasurer 方法");
        return new SkiaCharInfoMeasurer(TextEditor);
    }
}