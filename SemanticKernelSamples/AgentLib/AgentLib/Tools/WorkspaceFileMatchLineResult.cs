namespace AgentLib.Tools;

/// <summary>
/// 表示单行文件模式匹配的结果，包含命中行号、该行完整文本、匹配起始位置以及截断后的上下文文本。
/// </summary>
/// <param name="LineNumber">命中行号（1-based）。</param>
/// <param name="LineText">命中行的完整文本内容。</param>
/// <param name="MatchStartIndex">匹配内容在该行文本中的起始字符索引（0-based）。</param>
/// <param name="TruncatedContextText">截断后的上下文文本，以匹配位置为中心前后各取约 50 字符，超出部分用 … 标记。</param>
public readonly record struct WorkspaceFileMatchLineResult(int LineNumber, string LineText, int MatchStartIndex, string TruncatedContextText);