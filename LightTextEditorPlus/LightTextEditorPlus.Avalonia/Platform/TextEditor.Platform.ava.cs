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
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils.Patterns;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Utils;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus;

// 这里存放和 Avalonia 相关的代码
partial class TextEditor : Control
{
    public TextEditor()
    {
        // 属性初始化
        Focusable = true;

        TextEditorPlatformProvider = new AvaloniaSkiaTextEditorPlatformProvider();
        TextEditorPlatformProvider.AvaloniaTextEditor = this;
        SkiaTextEditor = new SkiaTextEditor(TextEditorPlatformProvider);

        _renderEngine = new AvaloniaTextEditorRenderEngine(this);

        SkiaTextEditor.InvalidateVisualRequested += SkiaTextEditor_InvalidateVisualRequested;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        TextEditorCore.LayoutCompleted += TextEditorCore_LayoutCompleted;
        TextEditorCore.TextChanged += TextEditorCore_TextChanged;

        //// 调试代码
        //TextEditorCore.AppendText("afg123微软雅黑123123");

        // 设计上会导致 Avalonia 总会调用二级的 SkiaTextEditor 接口实现功能。有开发资源可以做一层代理

        MouseHandler = new MouseHandler(this);

        //#if DEBUG
        //        WidthProperty.Changed.AddClassHandler((TextEditor o, AvaloniaPropertyChangedEventArgs args) =>
        //        {

        //        });
        //#endif

        IMESupporter.AddIMESupport(this);

        Cursor = new Avalonia.Input.Cursor(StandardCursorType.Ibeam);
    }

    internal AvaloniaSkiaTextEditorPlatformProvider TextEditorPlatformProvider { get; }

    /// <summary>
    /// 是否是当前正在调试的文本控件。一个界面里面包含多个控件的时候，就不太适合只用 IsInDebugMode 属性了，需要再有一个用来做业务上的区分
    /// </summary>
    private bool IsDebugging
#if DEBUG
        => DebugName?.Contains("江南莲花开，莲花惹人采") ?? false;
#else
        => false;
#endif

    #region 交互

    private MouseHandler MouseHandler { get; }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (e.Handled ||
            string.IsNullOrEmpty(e.Text) ||
            e.Text == "\x1b" ||
            // 退格键 \b 键
            e.Text == "\b" ||
            //emoji包围符
            e.Text == "\ufe0f")
            return;

        //如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
        if (RegexPatterns.Utf16SurrogatesPattern.ContainInRange(e.Text))
        {
            if (string.IsNullOrEmpty(_emojiCache))
            {
                _emojiCache += e.Text;
            }
            else
            {
                _emojiCache += e.Text;
                TextEditorCore.EditAndReplace(_emojiCache);
                _emojiCache = string.Empty;
            }
        }
        else
        {
            _emojiCache = string.Empty;
            TextEditorCore.EditAndReplace(e.Text);
        }

        base.OnTextInput(e);
    }

    /// <summary>
    /// 如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
    /// 用于接收第一个字符
    /// </summary>
    private string _emojiCache = string.Empty;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            TextEditorCore.Delete();
        }
        else if (e.Key == Key.Back)
        {
            TextEditorCore.Backspace();
        }
        else if (e.Key == Key.Enter)
        {
            TextEditorCore.EditAndReplace("\n");
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (!IsInEditingInputMode)
        {
            // 没有进入编辑模式，不处理键盘事件
            return;
        }

        if (TextEditorCore.IsDirty)
        {
            // 如果有明确布局的话，可以在这里加上明确布局
            ForceLayout();
        }

        if (e.Key == Key.Up)
        {
            TextEditorCore.MoveCaret(CaretMoveType.UpByLine);
        }
        else if (e.Key == Key.Down)
        {
            TextEditorCore.MoveCaret(CaretMoveType.DownByLine);
        }
        else if (e.Key == Key.Left)
        {
            TextEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);
        }
        else if (e.Key == Key.Right)
        {
            TextEditorCore.MoveCaret(CaretMoveType.RightByCharacter);
        }
    }

    #endregion

    #region 状态同步

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        if (IsAutoEditingModeByFocus)
        {
            // 获取焦点时，允许用户编辑，才能设置为编辑模式
            IsInEditingInputMode = IsEditable && true;
        }

        base.OnGotFocus(e);
    }

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
        if (e.Property == TextElement.ForegroundProperty)
        {
            //if (e.NewValue is BindingValue<IBrush> bindingBrush)
            //{
            //    SetForegroundInternal(bindingBrush.Value);
            //}
            //else
            if (e.NewValue is IBrush brush)
            {
                SetForegroundInternal(brush);
            }
        }
        else if (e.Property == WidthProperty)
        {
            if(e.NewValue is double width)
            {
                TextEditorCore.DocumentManager.DocumentWidth = width;
            }
        }
        else if(e.Property == HeightProperty)
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
    private RenderInfoProvider ForceLayout()
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
                TextEditorPlatformProvider.EnsureLayoutUpdated();
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

        TextRect documentLayoutBounds = TextEditorCore.GetDocumentLayoutBounds().DocumentOutlineBounds;

        bool widthChanged = !NearlyEquals(documentLayoutBounds.Width, DesiredSize.Width);
        bool heightChanged = !NearlyEquals(documentLayoutBounds.Height, DesiredSize.Height);

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

            // 此时可以通知文本底层进行布局了，这是一个很好的时机
            RenderInfoProvider renderInfoProvider = ForceLayout();
            (double x, double y, double width, double height) = renderInfoProvider.GetDocumentLayoutBounds()
                // 不应该取内容，应该取外接范围。解决垂直居中和底部对齐的问题
                .DocumentOutlineBounds;
            _ = x;
            _ = y;

            var notExistsWidth = double.IsInfinity(availableSize.Width) && double.IsNaN(Width);
            var notExistsHeight = double.IsInfinity(availableSize.Height) && double.IsNaN(Height);

            TextSizeToContent sizeToContent = TextEditorCore.SizeToContent;

            if (sizeToContent is TextSizeToContent.Width)
            {
                // 宽度自适应，高度固定
                if (notExistsHeight)
                {
                    throw new InvalidOperationException($"宽度自适应时，要求高度固定。{GetWidthAndHeightFormatMessage()}");
                }

                return new Size(width, availableSize.Height);
            }
            else if (sizeToContent is TextSizeToContent.Height)
            {
                // 高度自适应，宽度固定
                if (notExistsWidth)
                {
                    throw new InvalidOperationException($"高度自适应，要求宽度固定。{GetWidthAndHeightFormatMessage()}");
                }

                return new Size(availableSize.Width, height);
            }
            else if (sizeToContent is TextSizeToContent.WidthAndHeight)
            {
                // 宽度和高度都自适应
                return new Size(width, height);
            }
            else if (sizeToContent == TextSizeToContent.Manual)
            {
                if (notExistsWidth || notExistsHeight)
                {
                    throw new InvalidOperationException($"设置为 SizeToContent 为 TextSizeToContent.Manual 手动时，不能无限定 {nameof(Width)} 和 {nameof(Height)} 放入无限尺寸的容器。{GetWidthAndHeightFormatMessage()}");
                }

                // 手动的，有多少就要多少
                return availableSize;
            }

            // 文本库，有多少就要多少
            return availableSize;
        }
        finally
        {
            _isMeasuring = false;
        }

        string GetWidthAndHeightFormatMessage() =>
            $"AvailableSize={availableSize.Width:0.00},{availableSize.Height:0.00};Width={Width:0.00},Height={Height:0.00}";
    }

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

        }

        _isRendering = true;

        try
        {
            if (TextEditorCore.IsDirty)
            {
                ForceRedraw();
            }

            _renderEngine.Render(context);
        }
        finally
        {
            _isRendering = false;
        }
    }

    #endregion
}
