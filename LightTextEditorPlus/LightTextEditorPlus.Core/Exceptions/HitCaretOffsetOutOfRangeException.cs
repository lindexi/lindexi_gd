using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 用于进行命中的 <see cref="CaretOffset"/> 超过范围
/// </summary>
public class HitCaretOffsetOutOfRangeException : TextEditorException
{
    public HitCaretOffsetOutOfRangeException(TextEditorCore textEditor, CaretOffset inputCaretOffset, int currentDocumentCharCount)
    {
        TextEditor = textEditor;
        InputCaretOffset = inputCaretOffset;
        CurrentDocumentCharCount = currentDocumentCharCount;
    }

    public CaretOffset InputCaretOffset { get; }
    public int CurrentDocumentCharCount { get; }
    public TextEditorCore TextEditor { get; }
}