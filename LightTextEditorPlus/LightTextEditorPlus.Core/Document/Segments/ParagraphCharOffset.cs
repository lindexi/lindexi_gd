using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 表示段落的偏移量
/// </summary>
readonly struct ParagraphCharOffset
{
    /// <summary>
    /// 创建段落的偏移量
    /// </summary>
    [DebuggerStepThrough]
    public ParagraphCharOffset(int offset)
    {
        Offset = offset;
    }

    /// <summary>
    /// 段落偏移量
    /// </summary>
    public int Offset { get; }

    public override string ToString() => Offset.ToString();
}