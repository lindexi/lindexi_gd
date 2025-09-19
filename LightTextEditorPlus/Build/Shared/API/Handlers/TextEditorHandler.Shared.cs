#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Patterns;

using System;
using LightTextEditorPlus.Core;

namespace LightTextEditorPlus.Editing;

// todo 考虑命名为 interaction 交互处理器
public partial class TextEditorHandler
{
    public TextEditorHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;

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
}
#endif
