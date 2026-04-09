using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// <summary>
    /// 创建使用 Skia 渲染承载的文本编辑器
    /// </summary>
    /// <param name="platformProvider"></param>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// 文本编辑器内核
    /// </summary>
    public TextEditorCore TextEditorCore { get; }

    internal SkiaTextEditorPlatformProvider SkiaTextEditorPlatformProvider { get; }

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger => TextEditorCore.Logger;

    ///// <summary>
    ///// 获取文档的布局尺寸，实际布局尺寸
    ///// </summary>
    //public TextRect CurrentLayoutBounds { get; private set; } = TextRect.Zero;

    #region 渲染
    internal RenderManager RenderManager { get; }

    /// <summary>
    /// 禁用自动刷新光标和选择区域的渲染
    /// </summary>
    public void DisableAutoFlushCaretAndSelectionRender()
    {
        Logger.LogDebug("[SkiaTextEditor][DisableAutoFlushCaretAndSelectionRender] 禁用自动刷新光标和选择区域的渲染。过程不可逆");
        TextEditorCore.CurrentSelectionChanged -= TextEditorCore_CurrentSelectionChanged;
    }

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

        var renderManager = TextEditorCore.PlatformProvider.GetRenderManager();
        renderManager ??= this;
        renderManager.Render(renderInfoProvider);
    }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        if (renderInfoProvider.IsDirty)
        {
            return;
        }

        //CurrentLayoutBounds = TextEditorCore.GetDocumentLayoutBounds().DocumentOutlineBounds;

        RenderManager.Render(renderInfoProvider);

        InternalRenderCompleted?.Invoke(this, EventArgs.Empty);

        OnInvalidateVisualRequested();

        _renderCompletionSource.TrySetResult();
    }

    /// <summary>
    /// 获取当前的文本渲染内容
    /// </summary>
    /// <returns></returns>
    public ITextEditorContentSkiaRenderer GetCurrentTextRender()
    {
        return RenderManager.GetCurrentTextRender();
    }

    /// <summary>
    /// 构建渲染内容，适合自定义渲染
    /// </summary>
    /// <param name="renderContext"></param>
    /// <returns></returns>
    /// 正常情况下，可以使用 <see cref="IRenderManager.Render"/> 配合 <see cref="GetCurrentTextRender"/> 方法获取渲染内容
    /// 文本库提供两个渲染方式，第一个就是直接将 <see cref="IRenderManager.Render"/> 注入给到文本核心，直接用 <see cref="SkiaTextEditor"/> 承载渲染。第二个就是在 UI 框架里面，自己实现渲染调度，调用 <see cref="BuildTextEditorSkiaRender"/> 方法进行渲染
    /// 第一个方法无需 UI 框架做额外的处理，第二个方法可以让 UI 框架决定什么时候应该渲染，比如 UI 框架的渲染次数小于布局次数，很多次布局都只是为了输入层做的强行布局而已，此时就能够通过减少渲染次数提升性能
    /// 第二个方法还能支持范围渲染，提升长文本的性能
    public ITextEditorContentSkiaRenderer BuildTextEditorSkiaRender(in TextEditorSkiaRenderContext renderContext)
    {
        var textEditorSkiaRender = RenderManager.BuildTextEditorSkiaRender(in renderContext);

        if (!renderContext.RenderInfoProvider.IsDirty)
        {
            // 此时就算是完成渲染了
            _renderCompletionSource.TrySetResult();
        }

        return textEditorSkiaRender;
    }

    /// <summary>
    /// 获取当前的光标和选择的渲染内容
    /// </summary>
    /// <param name="renderContext"></param>
    /// <returns></returns>
    public ITextEditorCaretAndSelectionRenderSkiaRenderer GetCurrentCaretAndSelectionRender(in CaretAndSelectionRenderContext renderContext)
    {
        return RenderManager.GetCurrentCaretAndSelectionRender(renderContext);
    }

    /// <summary>
    /// 构建光标和选择的渲染内容，适合自定义渲染
    /// </summary>
    /// <param name="renderInfoProvider"></param>
    /// <param name="selection"></param>
    /// <param name="renderContext"></param>
    /// <returns></returns>
    public ITextEditorCaretAndSelectionRenderSkiaRenderer BuildCaretAndSelectionRender
        (RenderInfoProvider renderInfoProvider, in Selection selection, in CaretAndSelectionRenderContext renderContext)
    {
        return RenderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);
    }

    /// <summary>
    /// 请求重新渲染当前的文本编辑器内容
    /// </summary>
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

    /// <summary>
    /// 调试配置
    /// </summary>
    public SkiaTextEditorDebugConfiguration DebugConfiguration { get; }

    #endregion

    /// <summary>
    /// 存放到图片文件
    /// </summary>
    /// <param name="filePath"></param>
    public void SaveAsImageFile(string filePath)
    {
        ITextEditorContentSkiaRenderer textRenderer = GetCurrentTextRender();
        TextRect bounds = textRenderer.RenderBounds;
        using var skBitmap = new SKBitmap((int) bounds.Width, (int) bounds.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKCanvas skCanvas = new SKCanvas(skBitmap);

        textRenderer.Render(skCanvas);

        using FileStream fileStream = File.OpenWrite(filePath);
        skBitmap.Encode(fileStream, SKEncodedImageFormat.Png, 100);
    }
}