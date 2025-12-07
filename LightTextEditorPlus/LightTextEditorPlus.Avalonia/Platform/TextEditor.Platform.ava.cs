using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Diagnostics.LogInfos;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Platform;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus;

// 这里存放和 Avalonia 相关的代码
partial class TextEditor : Control
{
    /// <summary>
    /// 创建文本编辑器
    /// </summary>
    public TextEditor() : this(builder: null)
    {
    }

    /// <summary>
    /// 创建文本编辑器
    /// </summary>
    /// <param name="builder">平台构建器的提供器</param>
    public TextEditor(IAvaloniaSkiaTextEditorPlatformProviderBuilder? builder)
    {
        // 属性初始化
        Focusable = true;

        TextEditorPlatformProvider = builder?.Build(this) ??
                                     new AvaloniaSkiaTextEditorPlatformProvider(this);
        SkiaTextEditor = new SkiaTextEditor(TextEditorPlatformProvider);

        _renderEngine = new AvaloniaTextEditorRenderEngine(this);

        // 禁用自动刷新光标和选择渲染。因为 Avalonia 框架会统一调用渲染，将光标渲染交给 Avalonia 来调度
        SkiaTextEditor.DisableAutoFlushCaretAndSelectionRender();
        TextEditorCore.CurrentSelectionChanged += (sender, args) =>
        {
            if (_renderEngine.CanShowCaret && !_isRendering)
            {
                // 能够显示光标，且不在渲染过程中，才触发重绘
                InvalidateVisual();
            }
        };

        SkiaTextEditor.InvalidateVisualRequested += SkiaTextEditor_InvalidateVisualRequested;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        TextEditorCore.LayoutCompleted += TextEditorCore_LayoutCompleted;
        TextEditorCore.TextChanged += TextEditorCore_TextChanged;

        // 设计上会导致 Avalonia 总会调用二级的 SkiaTextEditor 接口实现功能。有开发资源可以做一层代理

        //#if DEBUG
        //        WidthProperty.Changed.AddClassHandler((TextEditor o, AvaloniaPropertyChangedEventArgs args) =>
        //        {

        //        });
        //#endif

        IMESupporter.AddIMESupport(this);
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        UpdateInScrollViewer();

        base.OnLoaded(e);
    }

    internal AvaloniaSkiaTextEditorPlatformProvider TextEditorPlatformProvider { get; }

    /// <summary>
    /// 是否是当前正在调试的文本控件。一个界面里面包含多个控件的时候，就不太适合只用 IsInDebugMode 属性了，需要再有一个用来做业务上的区分
    /// </summary>
    private bool IsDebugging
#if DEBUG
        =>
            //DebugName?.Contains("江南莲花开，莲花惹人采") ?? 
            false;
#else
        => false;
#endif

    #region 交互

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        TextEditorHandler.OnPointerPressed(e);
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        TextEditorHandler.OnPointerMoved(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        TextEditorHandler.OnPointerReleased(e);
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        TextEditorHandler.OnTextInput(e);

        base.OnTextInput(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        TextEditorHandler.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        TextEditorHandler.OnKeyUp(e);
        base.OnKeyUp(e);
    }

    #endregion

    #region 状态同步

    /// <inheritdoc />
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        if (IsAutoEditingModeByFocus)
        {
            // 获取焦点时，允许用户编辑，才能设置为编辑模式
            IsInEditingInputMode = IsEditable && true;
        }

        base.OnGotFocus(e);
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (IsAutoEditingModeByFocus)
        {
            IsInEditingInputMode = false;
        }

        base.OnLostFocus(e);
    }

    #endregion

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // 决策： 不再跟随 Avalonia 的 ForegroundProperty 属性变化
        // 由于 Avalonia 的 ForegroundProperty 属性进入不符合预期，如页面切换的时候，都会触发 ForegroundProperty 属性的变化。于是就会出现文本的前景色被错误覆盖的情况
        //if (e.Property == TextElement.ForegroundProperty)
        //{
        //    Logger.LogDebug($"TextEditor Platform OnPropertyChanged Foreground changed.");
        //    //if (e.NewValue is BindingValue<IBrush> bindingBrush)
        //    //{
        //    //    SetForegroundInternal(bindingBrush.Value);
        //    //}
        //    //else
        //    if (e.NewValue is IBrush brush && brush.ToSkiaTextBrush() is { } skiaTextBrush)
        //    {
        //        if (!this.IsInitialized)
        //        {
        //            // 还没初始化，则需要额外判断文本是否有内容
        //            // 存在这样的情况，文本先设置内容，然后再加入到界面中。但按照 Avalonia 离谱的设计，加入到界面里面将会被刷一次 TextElement.ForegroundProperty 属性。如果此时走默认的 SetForeground 方法，会导致文本库的颜色被覆盖
        //            // 为了避免这种情况，需要在初始化之前先判断一下文本库是否有内容
        //            if (TextEditorCore.DocumentManager.IsInitializingTextEditor())
        //            {
        //                Logger.LogDebug("TextEditor Platform OnPropertyChanged Foreground changed. IsInitializingTextEditor=true. SetStyleTextRunProperty.");

        //                SetStyleTextRunProperty(property => property with
        //                {
        //                    Foreground = skiaTextBrush
        //                });
        //            }
        //            else
        //            {
        //                // 文本已经初始化过了，那就不能再设置颜色了，否则将会覆盖文本现有的属性配置
        //                // 对应测试用例：“文本加入界面之前被设置颜色，颜色不会在加入界面之后被覆盖”

        //                Logger.LogDebug("TextEditor Platform OnPropertyChanged Foreground changed. IsInitializingTextEditor=False. Ignore.");
        //            }
        //        }
        //        else
        //        {
        //            Logger.LogDebug("TextEditor Platform OnPropertyChanged Foreground changed. IsInitialized=False. SetForeground");

        //            SetForeground(skiaTextBrush);
        //        }
        //    }
        //}
        //else
        if (e.Property == WidthProperty)
        {
            if (e.NewValue is double width)
            {
                TextEditorCore.DocumentManager.DocumentWidth = width;
            }
        }
        else if (e.Property == HeightProperty)
        {
            if (e.NewValue is double height)
            {
                TextEditorCore.DocumentManager.DocumentHeight = height;
            }
        }

        base.OnPropertyChanged(e);
    }

    #region 布局

    /// <summary>
    /// 立刻布局，获取布局结果信息
    /// </summary>
    internal partial RenderInfoProvider ForceLayout()
    {
        _isInForceLayout = true;

        try
        {
            // 当前实现的 ForceLayout 是不亏的，因为只有文本存在变更的时候，才会执行实际逻辑
            // 而不是让文本必定需要重新布局
            RenderInfoProvider? renderInfoProvider;
            while (!TextEditorCore.TryGetRenderInfo(out renderInfoProvider))
            {
                // 什么时候这个循环会进入两次？当文本刚刚布局完成之后，就被其他业务弄脏了。如有业务监听 LayoutCompleted 事件，在此事件里面修改文本
                var hasLayout = TextEditorPlatformProvider.EnsureLayoutUpdated();
                if (!hasLayout)
                {
                    // 如果是存在上次异常的情况，可能这次也不能成功
                    bool isFinishUpdateLayoutWithException = TextEditorCore.IsFinishUpdateLayoutWithException;

                    // 继续循环也是不行的，需要强行压入布局内容
                    Logger.Log(new ForceLayoutNotFoundUpdateActionLogInfo(isFinishUpdateLayoutWithException));

                    // 如果没有压入的话，继续循环多少次也没用
                    TextEditorCore
                        .RequireReUpdateAllDocumentWhenFinishWithException();

                    // 压入之后，可以强行跑一次试试看
                    try
                    {
                        var hasLayout2 = TextEditorPlatformProvider.EnsureLayoutUpdated();
                        Debug.Assert(hasLayout2); // 由于前面强行压入了，现在必定是有得处理的
                    }
                    catch (Exception e)
                    {
                        if (isFinishUpdateLayoutWithException)
                        {
                            // 如果上次异常，这次也异常，那就基本没救了，继续靠异常炸掉吧
                            Logger.Log(new ForceLayoutContinuousExceptionLogInfo(e));
                        }

                        throw;
                    }
                }
            }

            return renderInfoProvider;
        }
        finally
        {
            _isInForceLayout = false;
        }
    }

    /// <summary>
    /// 是否进入强制布局状态
    /// </summary>
    private bool _isInForceLayout;

    private void ForceRedraw()
    {
        // 现在只需立刻布局即可，在布局完成之后会自动触发重绘
        _ = ForceLayout();
    }

    private void TextEditorCore_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        if (IsDebugging)
        {
        }

        InvalidateMeasureAfterLayoutCompleted();
        OnLayoutCompleted(e);
    }

    /// <summary>
    /// 布局完成之后，判断是否需要重新测量
    /// </summary>
    private void InvalidateMeasureAfterLayoutCompleted()
    {
        if (IsDebugging)
        {
        }

        if (_isMeasuring)
        {
            // 正在布局测量中，不需要再次触发布局~~。预计是触发 ForceLayout 或空文本布局。由于前面已经经过了 _isInForceLayout 判断了，所以能进入这里的只有空文本布局~~
            return;
        }

        Debug.Assert(!TextEditorCore.IsDirty, "布局完成时，文本一定可用");

        DocumentLayoutBounds layoutBounds = TextEditorCore.GetDocumentLayoutBounds();
        TextRect documentContentBounds = layoutBounds.DocumentContentBounds;
        TextRect documentOutlineBounds = layoutBounds.DocumentOutlineBounds;

        bool widthChanged = !NearlyEquals(documentOutlineBounds.Width, DesiredSize.Width);
        bool heightChanged = !NearlyEquals(documentOutlineBounds.Height, DesiredSize.Height);

        if (!widthChanged)
        {
            if (documentOutlineBounds.Width < documentContentBounds.Width)
            {
                // 如果内容已经超过了外接大小，则说明宽度应该发生变化
                widthChanged = true;
            }
        }

        if (!heightChanged)
        {
            if (documentOutlineBounds.Height < documentContentBounds.Height)
            {
                // 如果内容已经超过了外接大小，则说明高度应该发生变化
                heightChanged = true;
            }
        }

        bool shouldInvalidateMeasure = false;
        if (TextEditorCore.SizeToContent is TextSizeToContent.Width && widthChanged)
        {
            // 宽度自适应的情况下
            shouldInvalidateMeasure = true;
        }
        else if (TextEditorCore.SizeToContent is TextSizeToContent.Height && heightChanged)
        {
            // 高度自适应的情况下
            shouldInvalidateMeasure = true;
        }
        else if (TextEditorCore.SizeToContent is TextSizeToContent.WidthAndHeight && (widthChanged || heightChanged))
        {
            // 宽度和高度都自适应的情况下
            shouldInvalidateMeasure = true;
        }
        else if (TextEditorCore.SizeToContent is TextSizeToContent.Manual)
        {
            // 手动情况下，不需要重新布局
        }

        // 强行布局的情况下，不需要再次触发布局
        // 可能此时正在 UI 布局过程中，也可能只是其他业务需要获取值。此时再次触发布局是比较亏的
        // 除了一个情况，那就是当前是渲染强行触发的
        // 渲染强行触发的，则触发重新布局，可能此时渲染拿到的尺寸已不相同
        // 解决 问题号：b003ee 问题
        // 是否需要延迟执行重新布局
        bool shouldLazyInvalidateMeasure = shouldInvalidateMeasure // 应该布局
                                                                   // 强行布局且是渲染过程
                                           && _isInForceLayout
                                           && _isRendering;
        if (shouldLazyInvalidateMeasure)
        {
            // 延迟执行布局
            Dispatcher.UIThread.InvokeAsync(InvalidateMeasure);

            return;
        }

        if (shouldInvalidateMeasure)
        {
            InvalidateMeasure();
        }
    }

    private static bool NearlyEquals(double a, double b)
        => Math.Abs(a - b) < 0.001;

    private bool _isMeasuring;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsDebugging)
        {
            // 在这里打断点
        }

        _isMeasuring = true;
        try
        {
            var result = base.MeasureOverride(availableSize);
            _ = result;
            // 以下对应 WPF 的 MeasureTextEditorCore 方法。由于 Avalonia 里面不好分 TextView 层，于是就决定代码都写到一个方法里面

            TextSize measureResult = MeasureTextEditorCore(new TextSize(availableSize.Width, availableSize.Height));
            return new Size(measureResult.Width, measureResult.Height);
        }
        finally
        {
            _isMeasuring = false;
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        // 实际布局多大就使用多大
        TextEditorCore.DocumentManager.DocumentWidth = finalSize.Width;
        TextEditorCore.DocumentManager.DocumentHeight = finalSize.Height;

        return base.ArrangeOverride(finalSize);
    }

    #endregion

    #region 渲染

    private bool _isRendering;

    private void SkiaTextEditor_InvalidateVisualRequested(object? sender, EventArgs e)
    {
        Debug.Fail("不应该，也不可能进入此分支。因为现在全部都在 UI 框架里处理");

        if (_isRendering)
        {
            // 如果当前正在渲染中，那就不要再次触发重绘。因为再次触发重绘也是浪费
            return;
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (IsDebugging)
        {
            // 可在这里下断点
        }

        _isRendering = true;

        try
        {
            //if (TextEditorCore.IsDirty)
            //{
            //    ForceRedraw();
            //}
            //// 在 ForceLayout 里面会处理脏文本的问题。渲染时候，只能选择强制布局
            //RenderInfoProvider renderInfoProvider = ForceLayout();

            _renderEngine.Render(context);
        }
        finally
        {
            _isRendering = false;
        }
    }

    #endregion

    #region 滚动条可见范围

    private void UpdateInScrollViewer()
    {
        ScrollViewer? scrollViewer = Parent as ScrollViewer;
        _containerScrollViewer = scrollViewer;
    }

    private ScrollViewer? _containerScrollViewer;

    /// <summary>
    /// 获取可见范围
    /// </summary>
    /// <returns></returns>
    private TextRect? GetViewport()
    {
        if (_containerScrollViewer is null)
        {
            return null;
        }

        double offsetX = _containerScrollViewer.Offset.X;
        double offsetY = _containerScrollViewer.Offset.Y;
        double viewportWidth = _containerScrollViewer.Viewport.Width;
        double viewportHeight = _containerScrollViewer.Viewport.Height;
        return new TextRect(offsetX, offsetY, viewportWidth, viewportHeight);
    }

    #endregion
}