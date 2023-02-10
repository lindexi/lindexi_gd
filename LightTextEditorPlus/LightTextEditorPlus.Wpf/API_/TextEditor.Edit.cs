using System;
using System.Windows;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法

public partial class TextEditor
{
    #region Style

    #region RunProperty

    /// <summary>
    /// 开启或关闭文本加粗
    /// </summary>
    public void ToggleBold()
    {
        // todo 实现开启或关闭文本加粗
        SetFontWeight(FontWeights.Bold);
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
        if (selection is null)
        {
            // 获取当前选择，文本未选择则设置当前光标属性
        }


    }

    #endregion



    #endregion
}