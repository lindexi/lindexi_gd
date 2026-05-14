using Microsoft.Extensions.AI;

using System;
using System.Text.Json;

namespace AvaloniaAgentLib.Model;

/// <summary>
/// 表示聊天消息中的一个可观测片段。
/// </summary>
public interface ICopilotChatMessageItem
{
}

/// <summary>
/// 表示普通文本输出片段。
/// </summary>
public sealed class CopilotChatTextItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatTextItem(string text)
    {
        Text = text;
    }

    public string Text
    {
        get => _text;
        internal set
        {
            if (!SetField(ref _text, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasText));
        }
    }

    private string _text = string.Empty;

    public bool HasText => !string.IsNullOrEmpty(Text);
}

/// <summary>
/// 表示模型思考片段。
/// </summary>
public sealed class CopilotChatReasoningItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatReasoningItem(string text)
    {
        Text = text;
    }

    public string Text
    {
        get => _text;
        internal set
        {
            if (!SetField(ref _text, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasText));
        }
    }

    private string _text = string.Empty;

    public bool HasText => !string.IsNullOrEmpty(Text);
}

/// <summary>
/// 表示工具调用片段。
/// </summary>
public sealed class CopilotChatToolItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatToolItem(string callId, string toolName, string? inputText)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        InputText = inputText ?? string.Empty;
        OutputText = string.Empty;
    }

    public string CallId { get; }

    public string ToolName
    {
        get => _toolName;
        internal set
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "工具" : value;
            if (!SetField(ref _toolName, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _toolName = string.Empty;

    public string DisplayName => ToolName;

    public string InputText
    {
        get => _inputText;
        internal set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _inputText, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasInputText));
        }
    }

    private string _inputText = string.Empty;

    public bool HasInputText => !string.IsNullOrEmpty(InputText);

    public string OutputText
    {
        get => _outputText;
        internal set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _outputText, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOutputText));
        }
    }

    private string _outputText = string.Empty;

    public bool HasOutputText => !string.IsNullOrEmpty(OutputText);
}

internal static class CopilotChatMessageItemFormatter
{
    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    internal static string? FormatArguments(FunctionCallContent functionCallContent)
    {
        ArgumentNullException.ThrowIfNull(functionCallContent);
        return FormatValue(functionCallContent.Arguments);
    }

    internal static string? FormatResult(FunctionResultContent functionResultContent)
    {
        ArgumentNullException.ThrowIfNull(functionResultContent);
        return FormatValue(functionResultContent.Result);
    }

    internal static string? FormatValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        try
        {
            return JsonSerializer.Serialize(value, IndentedJsonSerializerOptions);
        }
        catch (NotSupportedException)
        {
            return value.ToString();
        }
    }
}
