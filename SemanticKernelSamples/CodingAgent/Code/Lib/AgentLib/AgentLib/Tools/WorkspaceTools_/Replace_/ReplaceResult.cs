namespace AgentLib.Tools;

/// <summary>
/// 表示单个字符串替换操作的结果。
/// </summary>
/// <param name="FilePath">被替换的文件路径。</param>
/// <param name="Success">替换操作是否成功。</param>
/// <param name="Message">操作结果描述信息，成功或失败的具体原因。</param>
public readonly record struct ReplaceResult(string FilePath, bool Success, string Message);
