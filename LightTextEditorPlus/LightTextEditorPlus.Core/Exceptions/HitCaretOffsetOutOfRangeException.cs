using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 表示进行命中的 <see cref="CaretOffset"/> 超过范围
/// </summary>
public class HitCaretOffsetOutOfRangeException : TextEditorException
{
    /// <summary>
    /// 创建命中的 <see cref="CaretOffset"/> 超过范围
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="inputCaretOffset"></param>
    /// <param name="currentDocumentCharCount"></param>
    /// <param name="argumentName"></param>
    public HitCaretOffsetOutOfRangeException(TextEditorCore textEditor, CaretOffset inputCaretOffset,
        int currentDocumentCharCount, string argumentName)
    {
        TextEditor = textEditor;
        InputCaretOffset = inputCaretOffset;
        CurrentDocumentCharCount = currentDocumentCharCount;
        ArgumentName = argumentName;
    }

    /// <summary>
    /// 输入的光标坐标
    /// </summary>
    public CaretOffset InputCaretOffset { get; }

    /// <summary>
    /// 当前文档的字符数量
    /// </summary>
    public int CurrentDocumentCharCount { get; }

    /// <summary>
    /// 参数名
    /// </summary>
    public string ArgumentName { get; }

    /// <inheritdoc />
    public override string Message =>
        $"[HitCaretOffsetOutOfRangeException] ArgumentName={ArgumentName};DocumentManagerCharCount={CurrentDocumentCharCount};CaretOffset={InputCaretOffset.Offset};TextEditor={TextEditor}";
}