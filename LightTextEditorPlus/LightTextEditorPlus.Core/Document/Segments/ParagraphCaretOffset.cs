using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 表示段落的光标偏移量
/// </summary>
readonly struct ParagraphCaretOffset
{
    /// <summary>
    /// 创建段落的光标偏移量
    /// </summary>
    [DebuggerStepThrough]
    public ParagraphCaretOffset(int offset)
    {
        Offset = offset;
    }

    /// <summary>
    /// 段落光标偏移量
    /// </summary>
    public int Offset { get; }

    public override string ToString() => Offset.ToString();
}