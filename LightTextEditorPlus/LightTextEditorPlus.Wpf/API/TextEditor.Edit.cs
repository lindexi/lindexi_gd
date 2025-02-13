using System;
using System.Windows;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Events;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法
[APIConstraint("TextEditor.Edit.Style.txt")]
[APIConstraint("TextEditor.Edit.Input.txt")]
[APIConstraint("TextEditor.Edit.txt")]
public partial class TextEditor
{
    #region 编辑模式

    /// <summary>
    /// 是否进入用户编辑模式。进入用户编辑模式将闪烁光标，支持输入法输入
    /// </summary>
    public bool IsInEditingInputMode
    {
        set
        {
            if (_isInEditingInputMode == value)
            {
                return;
            }

            EnsureEditInit();

            Logger.LogDebug(value ? "进入用户编辑模式" : "退出用户编辑模式");

            _isInEditingInputMode = value;

            if (value)
            {
                Focus();
            }

            IsInEditingInputModeChanged?.Invoke(this, EventArgs.Empty);
        }
        get => _isInEditingInputMode;
    }

    private bool _isInEditingInputMode = false;

    /// <summary>
    /// 是否进入编辑的模式变更完成事件
    /// </summary>
    public event EventHandler? IsInEditingInputModeChanged;

    /// <summary>
    /// 是否自动根据是否获取焦点设置是否进入编辑模式
    /// </summary>
    public bool IsAutoEditingModeByFocus { get; set; } = true;

    #endregion

    #region Style

    #region RunProperty

    /// <inheritdoc cref="DocumentManager.CurrentCaretRunProperty"/>
    /// 这里的转换使用 <see cref="RunProperty"/> 明确类型，这是为了在乱用的时候，可以更好抛出异常
    public IRunProperty CurrentCaretRunProperty => (RunProperty) TextEditorCore.DocumentManager.CurrentCaretRunProperty;

    /// <inheritdoc cref="DocumentManager.StyleRunProperty"/>
    public IRunProperty StyleRunProperty => (RunProperty) TextEditorCore.DocumentManager.StyleRunProperty;

    /// <summary>
    /// 创建一个新的 RunProperty 对象
    /// </summary>
    /// <param name="createRunProperty">传入默认的 <see cref="StyleRunProperty"/> 字符属性，返回创建的新的字符属性</param>
    /// <returns></returns>
    public IRunProperty CreateRunProperty(CreateRunProperty createRunProperty) =>
        createRunProperty((RunProperty) StyleRunProperty);

    /// <summary>
    /// 设置字体名
    /// </summary>
    /// <param name="fontName">如果字体不存在，将会自动回滚</param>
    /// <param name="selection"></param>
    public void SetFontName(string fontName, Selection? selection = null)
        => SetFontName(new FontName(fontName), selection);

    /// <summary>
    /// 设置字体名
    /// </summary>
    /// <param name="fontName">如果字体不存在，将会自动回滚</param>
    /// <param name="selection"></param>
    public void SetFontName(FontName fontName, Selection? selection = null)
    {
        SetRunProperty(p => p with{ FontName = fontName }, PropertyType.FontName, selection);
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    /// <param name="fontSize">使用WPF单位</param>
    /// <param name="selection"></param>
    public void SetFontSize(double fontSize, Selection? selection = null)
    {
        SetRunProperty(p => p with{ FontSize = fontSize }, PropertyType.FontSize, selection);
    }

    /// <summary>
    /// 设置前景色
    /// </summary>
    /// <param name="foreground"></param>
    /// <param name="selection"></param>
    public void SetForeground(ImmutableBrush foreground, Selection? selection = null)
    {
        SetRunProperty(p => p with { Foreground = foreground }, PropertyType.Foreground, selection);
    }

    /// <summary>
    /// 开启或关闭文本斜体
    /// </summary>
    public void ToggleItalic(Selection? selection = null)
    {
        FontStyle fontStyle;

        if (IsAllRunPropertyMatchPredicate(property => property.FontStyle == FontStyles.Normal, selection))
        {
            // 字体倾斜 Italic 和 Oblique 的差别
            // 使用 Italic 是字体提供的斜体，可以和正常字体有不同的界面
            // 使用 Oblique 只是将正常的字体进行倾斜
            // 如果一个字体没有斜体，那 Italic 和 Oblique 视觉效果相同
            // 详细请看 [WPF 字体 FontStyle 的 Italic 和 Oblique 的区别](https://blog.lindexi.com/post/WPF-%E5%AD%97%E4%BD%93-FontStyle-%E7%9A%84-Italic-%E5%92%8C-Oblique-%E7%9A%84%E5%8C%BA%E5%88%AB.html )
            fontStyle = FontStyles.Italic;
        }
        else
        {
            fontStyle = FontStyles.Normal;
        }

        SetFontStyle(fontStyle, selection);
    }

    /// <summary>
    /// 设置字体样式
    /// </summary>
    /// <param name="fontStyle"></param>
    /// <param name="selection"></param>
    public void SetFontStyle(FontStyle fontStyle, Selection? selection = null)
    {
        SetRunProperty(p => p with { FontStyle = fontStyle }, PropertyType.FontStyle, selection);
    }

    /// <summary>
    /// 开启或关闭文本加粗
    /// </summary>
    public void ToggleBold(Selection? selection = null)
    {
        FontWeight fontWeight;
        if (IsAllRunPropertyMatchPredicate(property => property.FontWeight == FontWeights.Normal, selection))
        {
            fontWeight = FontWeights.Bold;
        }
        else
        {
            fontWeight = FontWeights.Normal;
        }

        SetFontWeight(fontWeight, selection);
    }

    private bool IsAllRunPropertyMatchPredicate(Predicate<IRunProperty> predicate, Selection? selection)
    {
        return TextEditorCore.DocumentManager.AreAllRunPropertiesMatch(predicate, selection);
    }

    /// <summary>
    /// 设置字重
    /// </summary>
    /// <param name="fontWeight"></param>
    /// <param name="selection">如果未设置，将采用当前文本选择。文本未选择则设置当前光标属性</param>
    public void SetFontWeight(FontWeight fontWeight, Selection? selection = null)
    {
        SetRunProperty(p => p with { FontWeight = fontWeight }, PropertyType.FontWeight, selection);
    }

    /// <summary>
    /// 设置字符属性。如果传入的 <paramref name="selection"/> 是空，将会使用当前选择。当前选择是空将会修改当前光标字符属性。修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更
    /// </summary>
    /// <param name="config"></param>
    /// <param name="property"></param>
    /// <param name="selection"></param>
    internal void SetRunProperty(ConfigRunProperty config, PropertyType property, Selection? selection)
    {
        // 如果是在编辑模式，那就使用当前选择。如果非编辑模式，且当前没有选择任何内容，那就是对整个文本
        if (IsInEditingInputMode)
        {
            // 如果是在编辑模式，那就使用当前选择
            selection ??= TextEditorCore.CurrentSelection;
        }
        else
        {
            // 如果非编辑模式，且当前没有选择任何内容，那就是对整个文本
            if (selection is null)
            {
                selection = TextEditorCore.GetAllDocumentSelection();
            }
            else
            {
                // 有传入的话，使用传入的选择范围
            }
        }

        OnStyleChanging(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));

        TextEditorCore.DocumentManager.SetRunProperty<RunProperty>(runProperty => config(runProperty), selection);

        OnStyleChanged(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));
    }

    #endregion

    #region 文本属性

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/>
    public SizeToContent SizeToContent
    {
        get => TextEditorCore.SizeToContent switch
        {
            TextSizeToContent.Width => SizeToContent.Width,
            TextSizeToContent.Height => SizeToContent.Height,
            TextSizeToContent.Manual => SizeToContent.Manual,
            TextSizeToContent.WidthAndHeight => SizeToContent.WidthAndHeight,
            var t => (SizeToContent) t,
        };
        set
        {
            if (SizeToContent == value)
            {
                return;
            }

            TextEditorCore.SizeToContent = value switch
            {
                SizeToContent.Width => TextSizeToContent.Width,
                SizeToContent.Height => TextSizeToContent.Height,
                SizeToContent.Manual => TextSizeToContent.Manual,
                SizeToContent.WidthAndHeight => TextSizeToContent.WidthAndHeight,
                var t => (TextSizeToContent) t,
            };

            InvalidateMeasure();
        }
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.ArrangingType"/>
    public ArrangingType ArrangingType
    {
        set => TextEditorCore.ArrangingType = value;
        get => TextEditorCore.ArrangingType;
    }

    #endregion

    #endregion

    #region 输入

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendText"/>
    public void AppendText(string text) => TextEditorCore.AppendText(text);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendRun"/>
    public void AppendRun(ImmutableRun run) => TextEditorCore.AppendRun(run);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplace"/>
    public void EditAndReplace(string text, Selection? selection = null) => TextEditorCore.EditAndReplace(text, selection);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplaceRun"/>
    public void EditAndReplaceRun(ImmutableRun run, Selection? selection = null) => TextEditorCore.EditAndReplaceRun(run, selection);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Backspace"/>
    public void Backspace() => TextEditorCore.Backspace();

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Delete"/>
    public void Delete() => TextEditorCore.Delete();

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Remove"/>
    public void Remove(in Selection selection) => TextEditorCore.Remove(in selection);

    #endregion

    #region 事件

    /// <summary>
    /// 当设置样式时触发
    /// </summary>
    public event EventHandler<StyleChangeEventArgs>? StyleChanging;

    /// <summary>
    /// 当设置样式后触发
    /// </summary>
    public event EventHandler<StyleChangeEventArgs>? StyleChanged;

    internal void OnStyleChanging(StyleChangeEventArgs styleChangeEventArgs)
    {
        StyleChanging?.Invoke(this, styleChangeEventArgs);
    }

    internal void OnStyleChanged(StyleChangeEventArgs styleChangeEventArgs)
    {
        StyleChanged?.Invoke(this, styleChangeEventArgs);
    }

    #endregion
}

/// <summary>
/// 创建一个新的 RunProperty 对象的委托
/// </summary>
/// <param name="styleRunProperty"></param>
/// <returns></returns>
public delegate IRunProperty CreateRunProperty(RunProperty styleRunProperty);
