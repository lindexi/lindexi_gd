using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia;
using Avalonia.Skia;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 输入法的支持
/// </summary>
/// 实现参考 [Avalonia 简单实现输入法光标跟随效果 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18669800 )
internal class IMESupporter : TextInputMethodClient
{
    public static void AddIMESupport(TextEditor textEditor)
    {
        textEditor.TextInputMethodClientRequested += (sender, args) =>
        {
            args.Client = new IMESupporter(textEditor);
        };
    }

    private IMESupporter(TextEditor textEditor)
    {
        _textEditor = textEditor;

        _textEditor.TextEditorCore.CurrentCaretOffsetChanged += TextEditor_CurrentCaretOffsetChanged;
        _textEditor.TextEditorCore.LayoutCompleted += TextEditorCore_LayoutCompleted;
    }

    private void TextEditorCore_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        if (_textEditor.TextEditorCore.IsDirty)
        {
            // 这是有可能的，因为可能有业务就刚好在布局完成后修改了文本
        }

        UpdateCaret();
    }

    private void TextEditor_CurrentCaretOffsetChanged(object? sender, TextEditorValueChangeEventArgs<CaretOffset> e)
    {
        UpdateCaret();
    }

    private void UpdateCaret()
    {
        if (_textEditor.TextEditorCore.IsDirty)
        {
            return;
        }

        CaretRenderInfo currentCaretRenderInfo = _textEditor.TextEditorCore.GetRenderInfo().GetCurrentCaretRenderInfo();
        _cursorRectangle = currentCaretRenderInfo.GetCaretBounds(caretWidth: 1).ToSKRect().ToAvaloniaRect();
        base.RaiseCursorRectangleChanged();
    }

    private readonly TextEditor _textEditor;

    public override Visual TextViewVisual => _textEditor;
    public override bool SupportsPreedit => true;
    public override bool SupportsSurroundingText => true;
    public override string SurroundingText => string.Empty;
    public override Rect CursorRectangle => _cursorRectangle;
    private Rect _cursorRectangle;
    public override TextSelection Selection { get; set; } // 似乎可以不更新选择范围
}
