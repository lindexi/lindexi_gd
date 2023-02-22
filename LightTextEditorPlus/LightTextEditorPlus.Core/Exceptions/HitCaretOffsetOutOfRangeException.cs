using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 用于进行命中的 <see cref="CaretOffset"/> 超过范围
/// </summary>
public class HitCaretOffsetOutOfRangeException : TextEditorException
{
    public HitCaretOffsetOutOfRangeException(TextEditorCore textEditor, CaretOffset inputCaretOffset, int currentDocumentCharCount, string argumentName)
    {
        TextEditor = textEditor;
        InputCaretOffset = inputCaretOffset;
        CurrentDocumentCharCount = currentDocumentCharCount;
        ArgumentName = argumentName;
    }

    public CaretOffset InputCaretOffset { get; }
    public int CurrentDocumentCharCount { get; }
    public string ArgumentName { get; }
    public TextEditorCore TextEditor { get; }

    public override string Message =>
        $"ArgumentName={ArgumentName};DocumentManagerCharCount={CurrentDocumentCharCount};CaretOffset={InputCaretOffset.Offset}";
}