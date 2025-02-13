using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Patterns;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Layout;
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
        TextEditorCore = new TextEditorCore(textEditorPlatformProvider);
        TextEditorCore.TextChanged += TextEditorCore_TextChanged;
        //SetDefaultTextRunProperty(property => property with
        //{
        //    FontSize = 40
        //});

        TextEditorPlatformProvider = textEditorPlatformProvider;

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

    /// <inheritdoc />
    protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
    {
        TextView.Measure(availableSize);
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
    {
        TextView.Arrange(new System.Windows.Rect(new System.Windows.Point(), finalSize));
        return base.ArrangeOverride(finalSize);
    }

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
        var caretBounds = caretRenderInfo.GetCaretBounds(CaretConfiguration.DefaultCaretWidth);
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
                TextEditorCore.EditAndReplace(_emojiCache);
                _emojiCache = string.Empty;
            }
        }
        else
        {
            _emojiCache = string.Empty;
            TextEditorCore.EditAndReplace(e.Text);
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