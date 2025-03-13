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
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Rendering;

using SkiaSharp;

namespace LightTextEditorPlus;

/// <summary>
/// 使用 Skia 渲染承载的文本编辑器
/// </summary>
/// 这是一个分部类，相关实现代码在：
/// - API 定义层： API\[Skia]TextEditor.*.cs
public partial class SkiaTextEditor : IRenderManager
{
    public SkiaTextEditor(SkiaTextEditorPlatformProvider? platformProvider = null)
    {
        SkiaTextEditorPlatformProvider? skiaTextEditorPlatformProvider;

        if (platformProvider is null)
        {
            skiaTextEditorPlatformProvider = new SkiaTextEditorPlatformProvider();
        }
        else
        {
            skiaTextEditorPlatformProvider = platformProvider;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (skiaTextEditorPlatformProvider.TextEditor != null)
            {
                throw new InvalidOperationException($"每个 SkiaTextEditorPlatformProvider 只能和一个 SkiaTextEditor 关联，禁止跨文本框使用。当前传入的 SkiaTextEditorPlatformProvider 关联的文本框的 DebugName={skiaTextEditorPlatformProvider.TextEditor.TextEditorCore.DebugName}");
            }
        }

        skiaTextEditorPlatformProvider.TextEditor = this;
        SkiaTextEditorPlatformProvider = skiaTextEditorPlatformProvider;
        TextEditorCore = new TextEditorCore(skiaTextEditorPlatformProvider);

        DebugConfiguration = new SkiaTextEditorDebugConfiguration(this);

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

    internal SkiaTextEditorPlatformProvider SkiaTextEditorPlatformProvider { get; }

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

    /// <summary>
    /// 调试下使用的重新渲染
    /// </summary>
    internal void DebugReRender()
    {
        if (!TextEditorCore.IsInDebugMode)
        {
            throw new InvalidOperationException($"此方法 {nameof(DebugReRender)} 只有调试模式下才能调用");
        }

        if (TextEditorCore.IsDirty)
        {
            // 如果是脏的，那就不需要在调试下重新渲染，等待自动进入渲染
            return;
        }

        RenderInfoProvider renderInfoProvider = TextEditorCore.GetRenderInfo();
        IRenderManager renderManager = this;
        renderManager.Render(renderInfoProvider);
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

    public ITextEditorContentSkiaRender GetCurrentTextRender()
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

    #region 调试

    public SkiaTextEditorDebugConfiguration DebugConfiguration { get; }

    #endregion
}

public class SkiaTextEditorPlatformProvider : PlatformProvider
{
    public SkiaTextEditor TextEditor { get; internal set; }
    // 框架确保赋值
        = null!;

    public override double GetFontLineSpacing(IReadOnlyRunProperty runProperty)
    {
        // 兼容获取的方法
        // 详细请参阅 cb389420514f0eb9cdb6ed79378e5e4508c2e2c4
        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.AsSkiaRunProperty().GetRenderingRunPropertyInfo();
        SKFont skFont = renderingRunPropertyInfo.Font;
        return (-skFont.Metrics.Ascent + skFont.Metrics.Descent) / skFont.Size;
    }

    private SkiaPlatformResourceManager? _skiaPlatformFontManager;

    private SkiaPlatformRunPropertyCreator? _skiaPlatformRunPropertyCreator;

    protected virtual SkiaPlatformResourceManager GetSkiaPlatformResourceManager()
    {
        return _skiaPlatformFontManager ??= new SkiaPlatformResourceManager(TextEditor);
    }

    public override IPlatformFontNameManager GetPlatformFontNameManager()
    {
        return GetSkiaPlatformResourceManager();
    }

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        return _skiaPlatformRunPropertyCreator ??= new SkiaPlatformRunPropertyCreator(GetSkiaPlatformResourceManager(), TextEditor);
    }

    public override IRenderManager GetRenderManager()
    {
        return TextEditor;
    }

    //public override ISingleCharInLineLayouter GetSingleRunLineLayouter()
    //{
    // // 原本以为 Skia 可以通过 BreakText 进行一行布局，然而过程发现其没有带语言文化，且即使带了估计也不符合预期。因此废弃此类型，换成 SkiaCharInfoMeasurer 只测量字符尺寸。但后续可能依然需要 HarfBuzz 辅助处理合写字的情况，到时候也许依然需要开放此类型。只不过这个过程中不需要再次测量字符尺寸而已
    //    return new SkiaSingleCharInLineLayouter(TextEditor);
    //}

    //public override IWholeLineCharsLayouter GetWholeLineCharsLayouter()
    //{
    //    return _skiaWholeLineLayouter ??= new SkiaWholeLineCharsLayouter();
    //}

    //private SkiaWholeLineCharsLayouter? _skiaWholeLineLayouter;

    public override ICharInfoMeasurer GetCharInfoMeasurer()
    {
        return _charInfoMeasurer ??= new SkiaCharInfoMeasurer();
    }

    private SkiaCharInfoMeasurer? _charInfoMeasurer;
}
