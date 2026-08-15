using Microsoft.Extensions.AI;

namespace AgentLib.Reducers;

/// <summary>
/// 自动对话压缩的统计信息。
/// </summary>
public sealed record CopilotChatCompressionStatistics
(
    int OriginalMessageCount,
    int CompressedMessageCount,
    int OriginalCharacterCount,
    int CharacterThreshold
);

/// <summary>
/// 自动对话压缩的完成结果。
/// </summary>
public sealed record CopilotChatCompressionResult
(
    CopilotChatCompressionStatistics Statistics,
    int ReducedMessageCount,
    IReadOnlyList<AIContent> SummaryContents
);