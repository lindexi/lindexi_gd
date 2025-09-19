#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Patterns;

using System;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 文本编辑器的交互处理器
/// </summary>
// todo 考虑命名为 interaction 交互处理器
public partial class TextEditorHandler
{
    /// <summary>
    /// 创建文本编辑器的交互处理器
    /// </summary>
    /// <param name="textEditor"></param>
    public TextEditorHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;

    #region 鼠标相关


    #endregion

    #region 键盘相关

    /// <summary>
    /// 输入 Insert 键的处理，切换插入/覆盖模式
    /// </summary>
    protected virtual void SwitchOvertypeMode()
    {
        if (TextEditor.CheckFeaturesDisableWithLog(TextFeatures.OvertypeModeEnable))
        {
            return;
        }

        TextEditor.IsOvertypeMode = !TextEditor.IsOvertypeMode;
    }

    #endregion

    #region 文本输入

    /// <summary>
    /// 收到原始文本输入，可能此时需要考虑处理 Emoji 等情况。可考虑只重写 <see cref="PerformInput"/> 方法，获取更上层的支持
    /// </summary>
    /// <param name="text"></param>
    protected virtual void RawTextInput(string text)
    {
        //如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
        if (RegexPatterns.Utf16SurrogatesPattern.ContainInRange(text))
        {
            if (string.IsNullOrEmpty(_emojiCache))
            {
                _emojiCache += text;
            }
            else
            {
                _emojiCache += text;

                PerformInput(_emojiCache);
                _emojiCache = string.Empty;
            }
        }
        else
        {
            _emojiCache = string.Empty;
            PerformInput(text);
        }
    }

    /// <summary>
    /// 处理输入的文本，直接插入到文本中
    /// </summary>
    /// <param name="text"></param>
    protected virtual void PerformInput(string text)
    {
        Selection? selection = null;
        if (TextEditor.IsOvertypeMode)
        {
            selection = TextEditorCore.GetCurrentOvertypeModeSelection(text.Length);
        }

        TextEditorCore.EditAndReplace(text, selection);
    }

    /// <summary>
    /// 如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
    /// 用于接收第一个字符
    /// </summary>
    private string _emojiCache = string.Empty;
    #endregion

    #region 剪贴板

    

    #endregion
}
#endif
