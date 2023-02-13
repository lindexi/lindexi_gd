using System;
using System.Windows;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Events;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法
public partial class TextEditor
{
    #region Style

    #region RunProperty

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
        SetRunProperty(p => p.FontName = fontName, PropertyType.FontSize, selection);
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    /// <param name="fontSize">使用WPF单位</param>
    /// <param name="selection"></param>
    public void SetFontSize(double fontSize, Selection? selection = null)
    {
        SetRunProperty(p => p.FontSize = fontSize, PropertyType.FontSize, selection);
    }

    /// <summary>
    /// 设置前景色
    /// </summary>
    /// <param name="foreground"></param>
    /// <param name="selection"></param>
    public void SetForeground(ImmutableBrush foreground, Selection? selection = null)
    {
        SetRunProperty(p => p.Foreground = foreground, PropertyType.Foreground, selection);
    }

    /// <summary>
    /// 开启或关闭文本斜体
    /// </summary>
    public void ToggleItalic()
    {
        FontStyle fontStyle;

        if (IsAnyRunProperty(property => property.FontStyle == FontStyles.Normal))
        {
            // 字体倾斜 Italic 和 Oblique 的差别
            // 使用 Italic 是字体提供的斜体，可以和正常字体有不同的界面
            // 使用 Oblique 只是将正常的字体进行倾斜
            // 如果一个字体没有斜体，那 Italic 和 Oblique 视觉效果相同
            // 详细请看 https://wpf.2000things.com/tag/oblique/
            fontStyle = FontStyles.Italic;
        }
        else
        {
            fontStyle = FontStyles.Normal;
        }

        SetFontStyle(fontStyle);
    }

    public void SetFontStyle(FontStyle fontStyle, Selection? selection = null)
    {
        SetRunProperty(p => p.FontStyle = fontStyle, PropertyType.FontStyle, selection);
    }

    /// <summary>
    /// 开启或关闭文本加粗
    /// </summary>
    public void ToggleBold()
    {
        FontWeight fontWeight;
        if (IsAnyRunProperty(property => property.FontWeight == FontWeights.Normal))
        {
            fontWeight = FontWeights.Bold;
        }
        else
        {
            fontWeight = FontWeights.Normal;
        }

        SetFontWeight(fontWeight);
    }

    private bool IsAnyRunProperty(Predicate<IRunProperty> predicate)
    {
        if (CurrentSelection.IsEmpty)
        {
            // 获取当前光标的属性即可
            return predicate(CurrentCaretRunProperty);
        }
        else
        {
            throw new NotImplementedException($"获取范围属性");
        }
    }

    /// <summary>
    /// 设置字重
    /// </summary>
    /// <param name="fontWeight"></param>
    /// <param name="selection">如果未设置，将采用当前文本选择。文本未选择则设置当前光标属性</param>
    public void SetFontWeight(FontWeight fontWeight, Selection? selection = null)
    {
        SetRunProperty(p => p.FontWeight = fontWeight, PropertyType.FontWeight, selection);
    }

    /// <summary>
    /// 设置字符属性。如果传入的 <paramref name="selection"/> 是空，将会使用当前选择。当前选择是空将会修改当前光标字符属性。修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更
    /// </summary>
    /// <param name="action"></param>
    /// <param name="property"></param>
    /// <param name="selection"></param>
    internal void SetRunProperty(Action<RunProperty> action, PropertyType property, Selection? selection)
    {
        selection ??= CurrentSelection;
        OnStyleChanging(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));
        
        TextEditorCore.DocumentManager.SetRunProperty<RunProperty>(action, selection);

        OnStyleChanged(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));
    }

    #endregion

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