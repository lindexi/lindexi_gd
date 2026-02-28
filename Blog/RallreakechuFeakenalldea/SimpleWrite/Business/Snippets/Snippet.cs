namespace SimpleWrite.Business.Snippets;

/// <summary>
/// 文本片段，代码片
/// </summary>
public class Snippet
{
    /// <summary>
    /// 触发的文本内容
    /// </summary>
    public required string TriggerText { get; init; }

    /// <summary>
    /// 代码片的内容
    /// </summary>
    public required string ContentText { get; init; }

    /// <summary>
    /// 相对于内容的光标偏移量。不能超过内容长度
    /// </summary>
    public int RelativeCaretOffset { get; init; }
}