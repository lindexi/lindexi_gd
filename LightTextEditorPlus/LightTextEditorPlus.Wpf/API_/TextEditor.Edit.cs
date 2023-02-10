using System;
using System.Windows;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Events;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法
public partial class TextEditor
{
    #region Style

    #region RunProperty

    /// <summary>
    /// 开启或关闭文本斜体
    /// </summary>
    public void ToggleItalic()
    {
        FontStyle fontStyle;

        if (IsAnyRunProperty(property => property.FontStyle == FontStyles.Normal))
        {
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
        SetRunProperty(p => p.FontStyle = fontStyle, PropertyType.FontWeight, selection);
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

    internal void SetRunProperty(Action<RunProperty> action, PropertyType property, Selection? selection)
    {
        selection ??= CurrentSelection;
        OnStyleChanging(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));

        // 设置当前的属性，如果没有选择内容，则设置当前光标的属性。设置光标属性，在输入之后，将会修改光标，从而干掉光标属性。干掉了光标属性，将会获取当前光标对应的字符的属性
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