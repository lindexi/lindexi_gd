using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Patterns;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Layers;
using LightTextEditorPlus.Rendering;
using LightTextEditorPlus.Utils;

using FrameworkElement = System.Windows.FrameworkElement;

namespace LightTextEditorPlus;

/// <summary>
/// 文本编辑器
/// </summary>
/// 这就是整个程序集的入口
public partial class TextEditor : FrameworkElement, IRenderManager, IIMETextEditor, INotifyPropertyChanged
{
    static TextEditor()
    {
        // 设置此控件可获取焦点。只有获取焦点才能收到键盘输入法的输入内容
        FocusableProperty.OverrideMetadata(typeof(TextEditor), new UIPropertyMetadata(true));

        KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TextEditor),
            new FrameworkPropertyMetadata(true));
        KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TextEditor),
            new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
    }

    /// <summary>
    /// 创建文本框
    /// </summary>
    public TextEditor()
    {
        #region 清晰文本

        SnapsToDevicePixels = true;
        RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        #endregion

        #region 配置文本

        var textEditorPlatformProvider = new TextEditorPlatformProvider(this);
        TextEditorPlatformProvider = textEditorPlatformProvider;
        TextEditorCore = new TextEditorCore(textEditorPlatformProvider);
        TextEditorCore.TextChanged += TextEditorCore_TextChanged;
        TextEditorCore.LayoutCompleted += TextEditorCore_LayoutCompleted;

        // 在 WPF 框架里面，默认就应该使用 WPF 的行间距样式
        TextEditorCore.UseWpfLineSpacingStyle();

        #endregion

        Loaded += TextEditor_Loaded;

        TextView = new TextView(this);
        // 加入视觉树，方便调试和方便触发视觉变更
        AddVisualChild(TextView);
        AddLogicalChild(TextView);

        MouseHandler = new MouseHandler(this);

        // 挂上 IME 输入法的支持
        _ = new IMESupporter<TextEditor>(this);
    }

    #region 公开方法

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

    #endregion

    #region 框架

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
               var hasLayout = TextEditorPlatformProvider.EnsureLayoutUpdated();
               if (!hasLayout)
               {
                   // 如果是存在上次异常的情况，可能这次也不能成功
                   bool isFinishUpdateLayoutWithException = TextEditorCore.IsFinishUpdateLayoutWithException;

                   // 继续循环也是不行的，需要强行压入布局内容
                   // todo 这里可以考虑记录异常日志

                   // 如果没有压入的话，继续循环多少次也没用
                   TextEditorCore.DebugRequireReUpdateAllDocumentLayout(); // todo 换一个正确的方法来调用

                    // 压入之后，可以强行跑一次试试看
                    try
                    {
                        var hasLayout2 = TextEditorPlatformProvider.EnsureLayoutUpdated();
                        Debug.Assert(hasLayout2); // 由于前面强行压入了，现在必定是有得处理的
                    }
                    catch
                    {
                        if (isFinishUpdateLayoutWithException)
                        {
                            // 如果上次异常，这次也异常，那就基本没救了，继续靠异常炸掉吧
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

    private bool _isInForceLayout;

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

        if (_isMeasuring)
        {
            // 正在布局测量中，不需要再次触发布局
            return;
        }

        Debug.Assert(!TextEditorCore.IsDirty, "布局完成时，文本一定可用");

        TextRect documentLayoutBounds = TextEditorCore.GetDocumentLayoutBounds().DocumentOutlineBounds;
        bool widthChanged = !NearlyEquals(documentLayoutBounds.Width, DesiredSize.Width);
        bool heightChanged = !NearlyEquals(documentLayoutBounds.Height, DesiredSize.Height);

        bool shouldInvalidateMeasure = false;
        TextSizeToContent sizeToContent = TextEditorCore.SizeToContent;
        if (sizeToContent == TextSizeToContent.Width && widthChanged)
        {
            // 宽度自适应的情况下
            shouldInvalidateMeasure = true;
        }
        else if (sizeToContent == TextSizeToContent.Height && heightChanged)
        {
            // 高度自适应的情况下
            shouldInvalidateMeasure = true;
        }
        else if (sizeToContent == TextSizeToContent.WidthAndHeight && (widthChanged || heightChanged))
        {
            // 宽度和高度都自适应的情况下
            shouldInvalidateMeasure = true;
        }
        else if (sizeToContent == TextSizeToContent.Manual)
        {
            // 手动情况下，不需要重新布局
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
    protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
    {
        _isMeasuring = true;
        try
        {
            Size result = base.MeasureOverride(availableSize);
            _ = result;

            SyncControlSize();
            Size measureResult = MeasureTextEditorCore(availableSize);

            TextView.Measure(measureResult);

            return measureResult;
        }
        finally
        {
            _isMeasuring = false;
        }

        void SyncControlSize()
        {
            // 同步控件的大小到文本里面
            DocumentManager documentManager = TextEditorCore.DocumentManager;
            if (double.IsFinite(Width))
            {
                Debug.Assert(Width.Equals(documentManager.DocumentWidth),$"在 {nameof(OnPropertyChanged)}(DependencyPropertyChangedEventArgs e) 中必然已经完成了同步，正常不会单独变更 DocumentWidth 使其与 Width 不等");
                //documentManager.DocumentWidth = Width;
            }

            if (double.IsFinite(Height))
            {
                Debug.Assert(Height.Equals(documentManager.DocumentHeight));
            }
        }
    }

    private Size MeasureTextEditorCore(Size availableSize)
    {
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
        if (sizeToContent == TextSizeToContent.Width)
        {
            // 宽度自适应，高度固定
            if (notExistsHeight)
            {
                throw new InvalidOperationException($"宽度自适应时，要求高度固定。{GetWidthAndHeightFormatMessage()}");
            }

            return new Size(width, availableSize.Height);
        }
        else if (sizeToContent == TextSizeToContent.Height)
        {
            // 高度自适应，宽度固定
            if (notExistsWidth)
            {
                throw new InvalidOperationException($"高度自适应，要求宽度固定。{GetWidthAndHeightFormatMessage()}");
            }

            return new Size(availableSize.Width, height);
        }
        else if (sizeToContent == TextSizeToContent.WidthAndHeight)
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

        string GetWidthAndHeightFormatMessage() =>
            $"AvailableSize={availableSize.Width:0.00},{availableSize.Height:0.00};Width={Width:0.00},Height={Height:0.00}";
    }

    /// <inheritdoc />
    protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
    {
        // 实际布局多大就使用多大
        TextEditorCore.DocumentManager.DocumentWidth = finalSize.Width;
        TextEditorCore.DocumentManager.DocumentHeight = finalSize.Height;

        TextView.Arrange(new System.Windows.Rect(new System.Windows.Point(), finalSize));
        return base.ArrangeOverride(finalSize);
    }
    #endregion

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        if (IsInEditingInputMode)
        {
            Focus();
        }
    }

    ///// <inheritdoc />
    //protected override void OnGotFocus(RoutedEventArgs e)
    //{
    //    IsInEditingInputMode = true;
    //    base.OnGotFocus(e);
    //}

    ///// <inheritdoc />
    //protected override void OnLostFocus(RoutedEventArgs e)
    //{
    //    IsInEditingInputMode = false;
    //    base.OnLostFocus(e);
    //}

    /// <summary>
    /// 确保编辑功能初始化完成
    /// </summary>
    private void EnsureEditInit()
    {
        if (_isInitEdit) return;
        _isInitEdit = true;

        _keyboardHandler ??= new KeyboardHandler(this);
    }

    private bool _isInitEdit;

    /// <summary>
    /// 视觉呈现容器
    /// </summary>
    private TextView TextView { get; }
    private MouseHandler MouseHandler { get; }
    private KeyboardHandler? _keyboardHandler;

    /// <inheritdoc />
    protected override int VisualChildrenCount => 1; // 当前只有视觉呈现容器一个而已
    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) => TextView;

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == TextElement.ForegroundProperty)
        {
            // 被设置文本前景色
            var brush = e.NewValue as Brush;
            if (brush is null)
            {
                return;
            }

            if (!brush.IsFrozen)
            {
                brush = brush.Clone();
                brush.Freeze();
            }

            SetForeground(new ImmutableBrush(brush));
        }
        else if (e.Property == FrameworkElement.WidthProperty)
        {
            TextEditorCore.DocumentManager.DocumentWidth = (double) e.NewValue;
        }
        else if (e.Property == FrameworkElement.HeightProperty)
        {
            TextEditorCore.DocumentManager.DocumentHeight = (double) e.NewValue;
        }
    }

    #region 命中测试

    /// <inheritdoc />
    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    #endregion

    #region 对接文本库

    internal TextEditorPlatformProvider TextEditorPlatformProvider { get; }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        TextView.Render(renderInfoProvider);
        _renderCompletionSource.TrySetResult();
    }

    private TaskCompletionSource _renderCompletionSource = new TaskCompletionSource();

    #endregion

    #endregion

    #region IME 支持

    /// <inheritdoc />
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        Logger.LogDebug($"[TextEditor] GotKeyboardFocus 获取键盘焦点");

        if (IsAutoEditingModeByFocus)
        {
            IsInEditingInputMode = IsEditable && true;
        }

        base.OnGotKeyboardFocus(e);
    }

    /// <inheritdoc />
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        Logger.LogDebug($"[TextEditor] LostKeyboardFocus");

        if (IsAutoEditingModeByFocus)
        {
            IsInEditingInputMode = false;
        }

        base.OnLostKeyboardFocus(e);
    }

    string IIMETextEditor.GetFontFamilyName()
    {
        return CurrentCaretRunProperty.FontName.UserFontName;
    }

    int IIMETextEditor.GetFontSize()
    {
        return (int) Math.Round(CurrentCaretRunProperty.FontSize);
    }

    System.Windows.Point IIMETextEditor.GetTextEditorLeftTop()
    {
        return new System.Windows.Point();
    }

    System.Windows.Point IIMETextEditor.GetCaretLeftTop()
    {
        TextEditorPlatformProvider.EnsureLayoutUpdated();
        var renderInfoProvider = TextEditorCore.GetRenderInfo();
        var caretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
        var caretBounds = caretRenderInfo.GetCaretBounds(CaretConfiguration.DefaultCaretThickness);
        return caretBounds.ToWpfRect().TopLeft;
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        base.OnTextInput(e);

        if (e.Handled)
        {
            return;
        }

        //TextInputManager
        PerformTextInput(e);

        e.Handled = true;
    }

    private void PerformTextInput(TextCompositionEventArgs e)
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

                PerformInput(_emojiCache);
                _emojiCache = string.Empty;
            }
        }
        else
        {
            _emojiCache = string.Empty;
            PerformInput(e.Text);
        }

        void PerformInput(string text)
        {
            Selection? selection = null;
            if (IsOvertypeMode)
            {
                selection = TextEditorCore.GetCurrentOvertypeModeSelection(text.Length);
            }

            TextEditorCore.EditAndReplace(text, selection);
        }
    }

    /// <summary>
    /// 如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
    /// 用于接收第一个字符
    /// </summary>
    // ReSharper disable once IdentifierTypo
    private string _emojiCache = string.Empty;
    #endregion

    #region NotifyPropertyChanged

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 属性变更通知
    /// </summary>
    /// <param name="propertyName"></param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}