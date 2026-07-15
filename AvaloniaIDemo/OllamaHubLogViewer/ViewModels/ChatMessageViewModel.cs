using System;
using System.Collections.Generic;
using System.Linq;
using OllamaHubLogViewer.Models;

namespace OllamaHubLogViewer.ViewModels;

internal sealed class ChatMessageViewModel
{
    public ChatMessageViewModel(LogChatMessage message, int index)
    {
        ArgumentNullException.ThrowIfNull(message);

        Role = message.Role;
        RawRole = message.RawRole;
        Content = message.Content;
        ReasoningContent = message.ReasoningContent;
        Name = message.Name;
        ToolCallId = message.ToolCallId;
        RoleDisplayName = GetRoleDisplayName(message);
        Initial = GetInitial(message.Role);
        SequenceText = $"#{index + 1}";
        MetadataText = BuildMetadata(message);
        ToolCalls = message.ToolCalls.Select(static toolCall => new ToolCallViewModel(toolCall)).ToArray();
    }

    public LogChatRole Role { get; }

    public string RawRole { get; }

    public string RoleDisplayName { get; }

    public string Initial { get; }

    public string SequenceText { get; }

    public string Content { get; }

    public string ReasoningContent { get; }

    public string Name { get; }

    public string ToolCallId { get; }

    public string MetadataText { get; }

    public IReadOnlyList<ToolCallViewModel> ToolCalls { get; }

    public bool IsSystemMessage => Role == LogChatRole.System;

    public bool IsDeveloperMessage => Role == LogChatRole.Developer;

    public bool IsCenteredMessage => Role is LogChatRole.System or LogChatRole.Developer;

    public bool IsUserMessage => Role == LogChatRole.User;

    public bool IsLeftMessage => Role is LogChatRole.Assistant or LogChatRole.Tool or LogChatRole.Unknown;

    public bool IsToolMessage => Role == LogChatRole.Tool;

    public bool HasContent => !string.IsNullOrEmpty(Content);

    public bool HasReasoningContent => !string.IsNullOrEmpty(ReasoningContent);

    public bool HasToolCalls => ToolCalls.Count > 0;

    public bool HasMetadata => !string.IsNullOrEmpty(MetadataText);

    private static string GetRoleDisplayName(LogChatMessage message)
    {
        return message.RawRole.ToLowerInvariant() switch
        {
            "system" => "系统",
            "developer" => "开发者",
            "user" => "用户",
            "assistant" => "助手",
            "tool" or "function" when !string.IsNullOrWhiteSpace(message.Name) => $"工具 · {message.Name}",
            "tool" or "function" => "工具",
            _ when !string.IsNullOrWhiteSpace(message.RawRole) => message.RawRole,
            _ => "未知角色",
        };
    }

    private static string GetInitial(LogChatRole role)
    {
        return role switch
        {
            LogChatRole.System => "S",
            LogChatRole.Developer => "D",
            LogChatRole.User => "U",
            LogChatRole.Assistant => "A",
            LogChatRole.Tool => "T",
            _ => "?",
        };
    }

    private static string BuildMetadata(LogChatMessage message)
    {
        List<string> parts = new(4);
        if (message.Timestamp is { } timestamp)
        {
            parts.Add(timestamp.ToString("HH:mm:ss"));
        }

        if (!string.IsNullOrWhiteSpace(message.Model) && message.Role == LogChatRole.Assistant)
        {
            parts.Add(message.Model);
        }

        if (!string.IsNullOrWhiteSpace(message.FinishReason))
        {
            parts.Add($"结束：{message.FinishReason}");
        }

        if (!string.IsNullOrWhiteSpace(message.ToolCallId))
        {
            parts.Add($"调用：{message.ToolCallId}");
        }

        return string.Join(" · ", parts);
    }
}

internal sealed class ToolCallViewModel
{
    public ToolCallViewModel(LogToolCall toolCall)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        DisplayName = string.IsNullOrWhiteSpace(toolCall.Name)
            ? $"工具调用 {toolCall.Index + 1}"
            : toolCall.Name;
        Arguments = toolCall.Arguments;
        DetailText = string.Join(
            " · ",
            new[] { toolCall.Type, toolCall.Id }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    public string DisplayName { get; }

    public string Arguments { get; }

    public string DetailText { get; }

    public bool HasArguments => !string.IsNullOrEmpty(Arguments);

    public bool HasDetails => !string.IsNullOrEmpty(DetailText);
}
