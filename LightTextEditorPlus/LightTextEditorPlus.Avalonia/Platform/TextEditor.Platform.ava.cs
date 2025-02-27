using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

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

        PlatformProvider = new AvaloniaSkiaTextEditorPlatformProvider();
        PlatformProvider.AvaloniaTextEditor = this;
        SkiaTextEditor = new SkiaTextEditor(PlatformProvider);

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
    }

    internal AvaloniaSkiaTextEditorPlatformProvider PlatformProvider { get; }

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

    #region 布局

    /// <summary>
    /// 立刻布局
    /// </summary>
    private void ForceLayout()
    {
        _isInForceLayout = true;

        try
        {
            // 当前实现的 ForceLayout 是不亏的，因为只有文本存在变更的时候，才会执行实际逻辑
            // 而不是让文本必定需要重新布局
            PlatformProvider.EnsureLayoutUpdated();
        }
        finally
        {
            _isInForceLayout = false;
        }
    }

    private bool _isInForceLayout;

    private void ForceRedraw()
    {
        // 现在只需立刻布局即可，在布局完成之后会自动触发重绘
        ForceLayout();
    }

    private void TextEditorCore_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        InvalidateMeasureAfterLayoutCompleted();
        OnLayoutCompleted(e);
    }

    /// <summary>
    /// 布局完成之后，判断是否需要重新测量
    /// </summary>
    private void InvalidateMeasureAfterLayoutCompleted()
    {
        if (_isInForceLayout)
        {
            // 强行布局的情况下，不需要再次触发布局
            // 可能此时正在 UI 布局过程中，也可能只是其他业务需要获取值。此时再次触发布局是比较亏的
            return;
        }

        if (_isRendering)
        {
            // 如果当前正在渲染中，那就不要再次触发重绘。因为再次触发重绘也是浪费
            return;
        }

        if (_isMeasuring)
        {
            // 正在布局测量中，不需要再次触发布局。预计是触发 ForceLayout 或空文本布局。由于前面已经经过了 _isInForceLayout 判断了，所以能进入这里的只有空文本布局
            return;
        }

        TextRect documentLayoutBounds = TextEditorCore.GetDocumentLayoutBounds();

        bool widthChanged = Math.Abs(documentLayoutBounds.Width - DesiredSize.Width) > 0.001;
        bool heightChanged = Math.Abs(documentLayoutBounds.Height - DesiredSize.Height) > 0.001;

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

        if (shouldInvalidateMeasure)
        {
            InvalidateMeasure();
        }
    }

    private bool _isMeasuring;

    protected override Size MeasureOverride(Size availableSize)
    {
        _isMeasuring = true;
        try
        {
            var result = base.MeasureOverride(availableSize);

            if (TextEditorCore.SizeToContent is TextSizeToContent.Width)
            {
                // 宽度自适应，高度固定
                if (TextEditorCore.IsDirty)
                {
                    ForceLayout();
                }
                (double x, double y, double width, double height) = TextEditorCore.GetDocumentLayoutBounds();
                return new Size(width, availableSize.Height);
            }
            else if (TextEditorCore.SizeToContent is TextSizeToContent.Height)
            {
                // 高度自适应，宽度固定
                if (TextEditorCore.IsDirty)
                {
                    ForceLayout();
                }
                (double x, double y, double width, double height) = TextEditorCore.GetDocumentLayoutBounds();
                return new Size(availableSize.Width, height);
            }
            else if (TextEditorCore.SizeToContent is TextSizeToContent.WidthAndHeight)
            {
                // 宽度和高度都自适应
                if (TextEditorCore.IsDirty)
                {
                    ForceLayout();
                }
                (double x, double y, double width, double height) = TextEditorCore.GetDocumentLayoutBounds();
                return new Size(width, height);
            }
            else if (TextEditorCore.SizeToContent == TextSizeToContent.Manual)
            {
                if (IsInvalidSize(availableSize))
                {
                    throw new InvalidOperationException($"设置为 SizeToContent 为 TextSizeToContent.Manual 手动时，不能无限定 {nameof(Width)} 和 {nameof(Height)} 放入无限尺寸的容器");
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
    }

    /// <summary>
    /// Tests whether any of a <see cref="Size"/>'s properties include negative values,
    /// a NaN or Infinity.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <returns>True if the size is invalid; otherwise false.</returns>
    /// Copy from Avalonia https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Base/Layout/Layoutable.cs#L890-L901
    private static bool IsInvalidSize(Size size)
    {
        return size.Width < 0 || size.Height < 0 ||
               double.IsInfinity(size.Width) || double.IsInfinity(size.Height) ||
               double.IsNaN(size.Width) || double.IsNaN(size.Height);
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

    public override void Render(DrawingContext context)
    {
        _isRendering = true;

        try
        {
            if (TextEditorCore.IsDirty)
            {
                // 准备要渲染了，结果文本还是脏的，那就强制布局
                ForceRedraw();
            }

            ITextEditorSkiaRender textEditorSkiaRender = SkiaTextEditor.GetCurrentTextRender();
            context.Custom(new TextEditorCustomDrawOperation(new Rect(DesiredSize), textEditorSkiaRender));

            if (IsInEditingInputMode
                // 如果配置了选择区域在非编辑模式下也会绘制，那在非编辑模式下也会绘制选择区域
                || CaretConfiguration.ShowSelectionWhenNotInEditingInputMode)
            {
                // 只有编辑模式下才会绘制光标和选择区域
                context.Custom(new TextEditorCustomDrawOperation(new Rect(DesiredSize),
                    SkiaTextEditor.GetCurrentCaretAndSelectionRender()));
            }
        }
        finally
        {
            _isRendering = false;
        }
    }

    #endregion
}

class TextEditorCustomDrawOperation : ICustomDrawOperation
{
    public TextEditorCustomDrawOperation(Rect bounds, ITextEditorSkiaRender render)
    {
        _render = render;
        Bounds = bounds;

        render.AddReference();
    }

    private readonly ITextEditorSkiaRender _render;

    public void Dispose()
    {
        _render.ReleaseReference();
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(_render, (other as TextEditorCustomDrawOperation)?._render);
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature? skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skiaSharpApiLeaseFeature != null)
        {
            using ISkiaSharpApiLease skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            _render.Render(skiaSharpApiLease.SkCanvas);
        }
        else
        {
            // 不支持 Skia 绘制
        }
    }

    public Rect Bounds { get; }
}
