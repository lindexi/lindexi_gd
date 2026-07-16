using System;
using System.Collections.Generic;

namespace OllamaHubLogViewer.Models;

internal enum LogChatRole
{
    System,
    Developer,
    User,
    Assistant,
    Tool,
    Unknown,
}

internal sealed record LogToolCall(
    int Index,
    string Id,
    string Type,
    string Name,
    string Arguments);

internal sealed record LogChatMessage(
    LogChatRole Role,
    string RawRole,
    string Content,
    string ReasoningContent,
    IReadOnlyList<LogToolCall> ToolCalls,
    string Name,
    string ToolCallId,
    DateTimeOffset? Timestamp,
    string Model,
    string FinishReason);

internal sealed record LogUsage(
    long? PromptTokens,
    long? CompletionTokens,
    long? TotalTokens,
    long? CachedPromptTokens,
    long? ReasoningTokens,
    long? AcceptedPredictionTokens,
    long? RejectedPredictionTokens,
    long? PromptAudioTokens,
    long? CompletionAudioTokens,
    TimeSpan? TotalDuration,
    TimeSpan? LoadDuration,
    TimeSpan? PromptEvaluationDuration,
    TimeSpan? EvaluationDuration);

internal sealed record LogConversation(
    IReadOnlyList<LogChatMessage> Messages,
    int RequestMessageCount,
    bool RequestParseSucceeded,
    int InvalidResponseLineCount,
    bool ResponseCompleted,
    string Model,
    string ResponseId,
    DateTimeOffset? ResponseCreatedAt,
    LogUsage? Usage,
    IReadOnlyList<string> Warnings);
