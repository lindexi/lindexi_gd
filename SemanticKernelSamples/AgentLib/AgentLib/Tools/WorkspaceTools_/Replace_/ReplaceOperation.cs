namespace AgentLib.Tools;

/// <summary>
/// 表示单个字符串替换操作的参数。
/// </summary>
/// <param name="FilePath">要替换的文件路径（相对或绝对）。</param>
/// <param name="OldString">要替换的原始文本，必须在文件中唯一匹配。</param>
/// <param name="NewString">用于替换的新文本。</param>
/// <param name="Explanation">替换操作的简要说明。</param>
public readonly record struct ReplaceOperation(string FilePath, string OldString, string NewString, string Explanation);
