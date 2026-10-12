namespace AgentLib.Model;

/// <summary>
/// 保存一次工具调用的用户界面展示快照。
/// </summary>
/// <param name="PrimaryText">主要操作对象。</param>
/// <param name="SecondaryText">必要的补充信息。</param>
/// <param name="FullTargetText">用于提示信息的完整目标文本。</param>
public sealed record ToolCallPresentation(
    string? PrimaryText,
    string? SecondaryText,
    string? FullTargetText = null);
