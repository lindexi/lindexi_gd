using System.Windows;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法

public partial class TextEditor
{
    #region Style

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
    public void SetFontWeight(FontWeight fontWeight, Selection? selection=null)
    {
        
    }

    #endregion
}