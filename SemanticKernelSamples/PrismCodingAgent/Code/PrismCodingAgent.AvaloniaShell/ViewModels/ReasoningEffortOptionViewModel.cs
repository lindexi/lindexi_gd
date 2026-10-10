using Microsoft.Extensions.AI;

namespace PrismCodingAgent.AvaloniaShell.ViewModels;

/// <summary>
/// 表示聊天界面中可选择的思考强度。
/// </summary>
public sealed record ReasoningEffortOptionViewModel(string DisplayName, ReasoningEffort? Value);
