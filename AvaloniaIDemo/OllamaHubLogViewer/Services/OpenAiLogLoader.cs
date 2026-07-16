using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OllamaHubLogViewer.Models;

namespace OllamaHubLogViewer.Services;

internal sealed class OpenAiLogLoader
{
    private const string RequestFileName = "request.log";
    private const string ResponseFileName = "response.log";

    public async Task<LogConversation> LoadAsync(string sessionDirectory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionDirectory);

        string fullSessionDirectory = Path.GetFullPath(sessionDirectory);
        if (!Directory.Exists(fullSessionDirectory))
        {
            throw new DirectoryNotFoundException($"日志目录不存在：{fullSessionDirectory}");
        }

        string requestPath = Path.Join(fullSessionDirectory, RequestFileName);
        string responsePath = Path.Join(fullSessionDirectory, ResponseFileName);
        List<LogChatMessage> messages = [];
        List<string> warnings = [];
        string requestModel = string.Empty;
        int requestMessageCount = 0;
        bool requestParseSucceeded = false;

        if (File.Exists(requestPath))
        {
            try
            {
                (IReadOnlyList<LogChatMessage> requestMessages, requestModel) =
                    await ParseRequestAsync(requestPath, cancellationToken).ConfigureAwait(false);
                messages.AddRange(requestMessages);
                requestMessageCount = requestMessages.Count;
                requestParseSucceeded = true;
            }
            catch (JsonException exception)
            {
                warnings.Add($"request.log 不是有效的 OpenAI JSON：{exception.Message}");
            }
        }
        else
        {
            warnings.Add("未找到 request.log。");
        }

        ResponseParseResult responseResult = File.Exists(responsePath)
            ? await ParseResponseAsync(responsePath, cancellationToken).ConfigureAwait(false)
            : ResponseParseResult.Empty("未找到 response.log。");
        messages.AddRange(responseResult.Messages);
        warnings.AddRange(responseResult.Warnings);

        string model = string.IsNullOrWhiteSpace(requestModel)
            ? responseResult.Model
            : requestModel;
        return new LogConversation(
            messages,
            requestMessageCount,
            requestParseSucceeded,
            responseResult.InvalidLineCount,
            responseResult.Completed,
            model,
            responseResult.ResponseId,
            responseResult.CreatedAt,
            responseResult.Usage,
            warnings);
    }

    private static async Task<(IReadOnlyList<LogChatMessage> Messages, string Model)> ParseRequestAsync(
        string requestPath,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            requestPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 16 * 1024,
            useAsync: true);
        using JsonDocument document = await JsonDocument
            .ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        JsonElement root = document.RootElement;
        string model = GetString(root, "model");
        if (!root.TryGetProperty("messages", out JsonElement messagesElement)
            || messagesElement.ValueKind != JsonValueKind.Array)
        {
            return ([], model);
        }

        int messageCount = messagesElement.GetArrayLength();
        List<LogChatMessage> messages = new(messageCount);
        foreach (JsonElement messageElement in messagesElement.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string rawRole = GetString(messageElement, "role");
            string content = ReadContent(messageElement);
            string reasoningContent = GetString(messageElement, "reasoning_content");
            IReadOnlyList<LogToolCall> toolCalls = ReadCompleteToolCalls(messageElement);
            messages.Add(new LogChatMessage(
                ParseRole(rawRole),
                rawRole,
                content,
                reasoningContent,
                toolCalls,
                GetString(messageElement, "name"),
                GetString(messageElement, "tool_call_id"),
                null,
                model,
                string.Empty));
        }

        return (messages, model);
    }

    private static async Task<ResponseParseResult> ParseResponseAsync(
        string responsePath,
        CancellationToken cancellationToken)
    {
        var choices = new Dictionary<int, ResponseChoiceBuilder>();
        List<string> warnings = [];
        string responseId = string.Empty;
        string model = string.Empty;
        DateTimeOffset? createdAt = null;
        var usageBuilder = new UsageBuilder();
        int invalidLineCount = 0;
        bool completed = false;

        await using FileStream stream = new(
            responsePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 16 * 1024,
            useAsync: true);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string payload = NormalizeResponseLine(line);
            if (payload.Length == 0)
            {
                continue;
            }

            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                completed = true;
                continue;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement root = document.RootElement;
                responseId = FirstNonEmpty(responseId, GetString(root, "id"));
                model = FirstNonEmpty(model, GetString(root, "model"));
                createdAt ??= ReadResponseTimestamp(root);
                usageBuilder.Append(root);
                completed |= IsResponseCompleted(root);

                if (root.TryGetProperty("choices", out JsonElement choicesElement)
                    && choicesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement choiceElement in choicesElement.EnumerateArray())
                    {
                        int choiceIndex = GetInt32(choiceElement, "index", choices.Count);
                        if (!choices.TryGetValue(choiceIndex, out ResponseChoiceBuilder? builder))
                        {
                            builder = new ResponseChoiceBuilder(choiceIndex);
                            choices.Add(choiceIndex, builder);
                        }

                        builder.Append(choiceElement, model, createdAt);
                    }
                }
                else
                {
                    AppendNonStreamingResponse(root, choices, model, createdAt);
                }
            }
            catch (JsonException)
            {
                invalidLineCount++;
            }
        }

        if (invalidLineCount > 0)
        {
            warnings.Add($"response.log 中有 {invalidLineCount} 行无法解析，已跳过。");
        }

        List<LogChatMessage> messages = new(choices.Count);
        foreach (ResponseChoiceBuilder builder in choices.OrderBy(static pair => pair.Key).Select(static pair => pair.Value))
        {
            LogChatMessage? message = builder.Build();
            if (message is not null)
            {
                messages.Add(message);
            }
        }

        if (messages.Count == 0 && invalidLineCount == 0)
        {
            warnings.Add("response.log 中没有可显示的助手响应。");
        }

        return new ResponseParseResult(
            messages,
            model,
            responseId,
            createdAt,
            usageBuilder.Build(),
            warnings,
            invalidLineCount,
            completed);
    }

    private static bool IsResponseCompleted(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (root.TryGetProperty("done", out JsonElement doneElement)
            && doneElement.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(GetString(root, "finish_reason"))
            || !string.IsNullOrWhiteSpace(GetString(root, "done_reason")))
        {
            return true;
        }

        if (!root.TryGetProperty("choices", out JsonElement choicesElement)
            || choicesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return choicesElement
            .EnumerateArray()
            .Any(static choice => !string.IsNullOrWhiteSpace(GetString(choice, "finish_reason")));
    }

    private static void AppendNonStreamingResponse(
        JsonElement root,
        Dictionary<int, ResponseChoiceBuilder> choices,
        string model,
        DateTimeOffset? createdAt)
    {
        if (!root.TryGetProperty("message", out JsonElement messageElement)
            || messageElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (!choices.TryGetValue(0, out ResponseChoiceBuilder? builder))
        {
            builder = new ResponseChoiceBuilder(0);
            choices.Add(0, builder);
        }

        string finishReason = FirstNonEmpty(
            GetString(root, "finish_reason"),
            GetString(root, "done_reason"));
        builder.AppendMessage(messageElement, model, createdAt, finishReason);
    }

    private static string NormalizeResponseLine(string line)
    {
        string trimmedLine = line.Trim();
        if (trimmedLine.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmedLine["data:".Length..].TrimStart();
        }

        return trimmedLine;
    }

    private static IReadOnlyList<LogToolCall> ReadCompleteToolCalls(JsonElement messageElement)
    {
        if (!messageElement.TryGetProperty("tool_calls", out JsonElement toolCallsElement)
            || toolCallsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = toolCallsElement.GetArrayLength();
        List<LogToolCall> toolCalls = new(count);
        int fallbackIndex = 0;
        foreach (JsonElement toolCallElement in toolCallsElement.EnumerateArray())
        {
            int index = GetInt32(toolCallElement, "index", fallbackIndex++);
            JsonElement functionElement = toolCallElement.TryGetProperty("function", out JsonElement function)
                ? function
                : default;
            toolCalls.Add(new LogToolCall(
                index,
                GetString(toolCallElement, "id"),
                GetString(toolCallElement, "type"),
                GetString(functionElement, "name"),
                GetString(functionElement, "arguments")));
        }

        return toolCalls;
    }

    private static string ReadContent(JsonElement messageElement)
    {
        if (!messageElement.TryGetProperty("content", out JsonElement contentElement))
        {
            return string.Empty;
        }

        return ReadTextValue(contentElement);
    }

    private static string ReadTextValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Array => ReadContentArray(element),
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => element.GetRawText(),
        };
    }

    private static string ReadContentArray(JsonElement contentElement)
    {
        var builder = new StringBuilder();
        foreach (JsonElement part in contentElement.EnumerateArray())
        {
            string text = part.ValueKind == JsonValueKind.String
                ? part.GetString() ?? string.Empty
                : GetString(part, "text");
            if (string.IsNullOrEmpty(text) && part.ValueKind == JsonValueKind.Object)
            {
                text = part.GetRawText();
            }

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(text);
        }

        return builder.ToString();
    }

    private static LogChatRole ParseRole(string rawRole)
    {
        return rawRole.ToLowerInvariant() switch
        {
            "system" => LogChatRole.System,
            "developer" => LogChatRole.Developer,
            "user" => LogChatRole.User,
            "assistant" => LogChatRole.Assistant,
            "tool" or "function" => LogChatRole.Tool,
            _ => LogChatRole.Unknown,
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => property.GetRawText(),
        };
    }

    private static int GetInt32(JsonElement element, string propertyName, int fallback)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(propertyName, out JsonElement property)
               && property.TryGetInt32(out int value)
            ? value
            : fallback;
    }

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(propertyName, out JsonElement property)
               && property.TryGetInt64(out long value)
            ? value
            : null;
    }

    private static DateTimeOffset? ReadResponseTimestamp(JsonElement element)
    {
        DateTimeOffset? unixTimestamp = ReadUnixTimestamp(element, "created");
        if (unixTimestamp is not null)
        {
            return unixTimestamp;
        }

        string createdAt = GetString(element, "created_at");
        return DateTimeOffset.TryParse(
            createdAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
            out DateTimeOffset timestamp)
            ? timestamp.ToLocalTime()
            : null;
    }

    private static DateTimeOffset? ReadUnixTimestamp(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property)
            || !property.TryGetInt64(out long value))
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static string FirstNonEmpty(string currentValue, string candidate)
    {
        return string.IsNullOrWhiteSpace(currentValue) ? candidate : currentValue;
    }

    private sealed class ResponseChoiceBuilder(int index)
    {
        private readonly StringBuilder _content = new();
        private readonly StringBuilder _reasoningContent = new();
        private readonly Dictionary<int, ToolCallBuilder> _toolCalls = [];
        private string _rawRole = "assistant";
        private string _model = string.Empty;
        private string _finishReason = string.Empty;
        private DateTimeOffset? _createdAt;

        public void Append(JsonElement choiceElement, string model, DateTimeOffset? createdAt)
        {
            _model = FirstNonEmpty(_model, model);
            _createdAt ??= createdAt;
            _finishReason = FirstNonEmpty(_finishReason, GetString(choiceElement, "finish_reason"));

            if (choiceElement.TryGetProperty("delta", out JsonElement deltaElement)
                && deltaElement.ValueKind == JsonValueKind.Object)
            {
                AppendDelta(deltaElement);
            }
            else if (choiceElement.TryGetProperty("message", out JsonElement messageElement)
                     && messageElement.ValueKind == JsonValueKind.Object)
            {
                AppendMessage(messageElement, model, createdAt, _finishReason);
            }
        }

        public void AppendMessage(
            JsonElement messageElement,
            string model,
            DateTimeOffset? createdAt,
            string finishReason)
        {
            _model = FirstNonEmpty(_model, model);
            _createdAt ??= createdAt;
            _finishReason = FirstNonEmpty(_finishReason, finishReason);
            _rawRole = FirstNonEmpty(GetString(messageElement, "role"), _rawRole);
            _content.Append(ReadContent(messageElement));
            _reasoningContent.Append(GetString(messageElement, "reasoning_content"));
            AppendToolCalls(messageElement);
        }

        public LogChatMessage? Build()
        {
            IReadOnlyList<LogToolCall> toolCalls = _toolCalls
                .OrderBy(static pair => pair.Key)
                .Select(static pair => pair.Value.Build())
                .ToArray();
            string content = _content.ToString();
            string reasoningContent = _reasoningContent.ToString();
            if (content.Length == 0 && reasoningContent.Length == 0 && toolCalls.Count == 0)
            {
                return null;
            }

            return new LogChatMessage(
                ParseRole(_rawRole),
                _rawRole,
                content,
                reasoningContent,
                toolCalls,
                string.Empty,
                string.Empty,
                _createdAt,
                _model,
                _finishReason);
        }

        private void AppendDelta(JsonElement deltaElement)
        {
            _rawRole = FirstNonEmpty(GetString(deltaElement, "role"), _rawRole);
            _content.Append(ReadContent(deltaElement));
            _reasoningContent.Append(GetString(deltaElement, "reasoning_content"));
            AppendToolCalls(deltaElement);
        }

        private void AppendToolCalls(JsonElement element)
        {
            if (!element.TryGetProperty("tool_calls", out JsonElement toolCallsElement)
                || toolCallsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            int fallbackIndex = 0;
            foreach (JsonElement toolCallElement in toolCallsElement.EnumerateArray())
            {
                int toolCallIndex = GetInt32(toolCallElement, "index", fallbackIndex++);
                if (!_toolCalls.TryGetValue(toolCallIndex, out ToolCallBuilder? builder))
                {
                    builder = new ToolCallBuilder(toolCallIndex);
                    _toolCalls.Add(toolCallIndex, builder);
                }

                builder.Append(toolCallElement);
            }
        }
    }

    private sealed class ToolCallBuilder(int index)
    {
        private readonly StringBuilder _id = new();
        private readonly StringBuilder _type = new();
        private readonly StringBuilder _name = new();
        private readonly StringBuilder _arguments = new();

        public void Append(JsonElement toolCallElement)
        {
            AppendFragment(_id, GetString(toolCallElement, "id"));
            AppendFragment(_type, GetString(toolCallElement, "type"));
            if (toolCallElement.TryGetProperty("function", out JsonElement functionElement)
                && functionElement.ValueKind == JsonValueKind.Object)
            {
                AppendFragment(_name, GetString(functionElement, "name"));
                AppendFragment(_arguments, GetString(functionElement, "arguments"));
            }
        }

        public LogToolCall Build()
        {
            return new LogToolCall(
                index,
                _id.ToString(),
                _type.ToString(),
                _name.ToString(),
                _arguments.ToString());
        }

        private static void AppendFragment(StringBuilder builder, string fragment)
        {
            if (!string.IsNullOrEmpty(fragment))
            {
                builder.Append(fragment);
            }
        }
    }

    private sealed class UsageBuilder
    {
        private long? _promptTokens;
        private long? _completionTokens;
        private long? _totalTokens;
        private long? _cachedPromptTokens;
        private long? _reasoningTokens;
        private long? _acceptedPredictionTokens;
        private long? _rejectedPredictionTokens;
        private long? _promptAudioTokens;
        private long? _completionAudioTokens;
        private TimeSpan? _totalDuration;
        private TimeSpan? _loadDuration;
        private TimeSpan? _promptEvaluationDuration;
        private TimeSpan? _evaluationDuration;

        public void Append(JsonElement root)
        {
            if (root.TryGetProperty("usage", out JsonElement usageElement)
                && usageElement.ValueKind == JsonValueKind.Object)
            {
                SetIfPresent(ref _promptTokens, GetInt64(usageElement, "prompt_tokens"));
                SetIfPresent(ref _completionTokens, GetInt64(usageElement, "completion_tokens"));
                SetIfPresent(ref _totalTokens, GetInt64(usageElement, "total_tokens"));

                if (usageElement.TryGetProperty("prompt_tokens_details", out JsonElement promptDetails)
                    && promptDetails.ValueKind == JsonValueKind.Object)
                {
                    SetIfPresent(ref _cachedPromptTokens, GetInt64(promptDetails, "cached_tokens"));
                    SetIfPresent(ref _promptAudioTokens, GetInt64(promptDetails, "audio_tokens"));
                }

                if (usageElement.TryGetProperty("completion_tokens_details", out JsonElement completionDetails)
                    && completionDetails.ValueKind == JsonValueKind.Object)
                {
                    SetIfPresent(ref _reasoningTokens, GetInt64(completionDetails, "reasoning_tokens"));
                    SetIfPresent(ref _acceptedPredictionTokens, GetInt64(completionDetails, "accepted_prediction_tokens"));
                    SetIfPresent(ref _rejectedPredictionTokens, GetInt64(completionDetails, "rejected_prediction_tokens"));
                    SetIfPresent(ref _completionAudioTokens, GetInt64(completionDetails, "audio_tokens"));
                }
            }

            SetIfPresent(ref _promptTokens, GetInt64(root, "prompt_eval_count"));
            SetIfPresent(ref _completionTokens, GetInt64(root, "eval_count"));
            SetIfPresent(ref _totalDuration, ReadNanoseconds(root, "total_duration"));
            SetIfPresent(ref _loadDuration, ReadNanoseconds(root, "load_duration"));
            SetIfPresent(ref _promptEvaluationDuration, ReadNanoseconds(root, "prompt_eval_duration"));
            SetIfPresent(ref _evaluationDuration, ReadNanoseconds(root, "eval_duration"));
        }

        public LogUsage? Build()
        {
            long? totalTokens = _totalTokens;
            if (totalTokens is null && (_promptTokens is not null || _completionTokens is not null))
            {
                totalTokens = (_promptTokens ?? 0) + (_completionTokens ?? 0);
            }

            if (_promptTokens is null
                && _completionTokens is null
                && totalTokens is null
                && _cachedPromptTokens is null
                && _reasoningTokens is null
                && _acceptedPredictionTokens is null
                && _rejectedPredictionTokens is null
                && _promptAudioTokens is null
                && _completionAudioTokens is null
                && _totalDuration is null
                && _loadDuration is null
                && _promptEvaluationDuration is null
                && _evaluationDuration is null)
            {
                return null;
            }

            return new LogUsage(
                _promptTokens,
                _completionTokens,
                totalTokens,
                _cachedPromptTokens,
                _reasoningTokens,
                _acceptedPredictionTokens,
                _rejectedPredictionTokens,
                _promptAudioTokens,
                _completionAudioTokens,
                _totalDuration,
                _loadDuration,
                _promptEvaluationDuration,
                _evaluationDuration);
        }

        private static TimeSpan? ReadNanoseconds(JsonElement element, string propertyName)
        {
            long? nanoseconds = GetInt64(element, propertyName);
            return nanoseconds is >= 0
                ? TimeSpan.FromTicks(nanoseconds.Value / 100)
                : null;
        }

        private static void SetIfPresent<T>(ref T? target, T? value)
            where T : struct
        {
            if (value is not null)
            {
                target = value;
            }
        }
    }

    private sealed record ResponseParseResult(
        IReadOnlyList<LogChatMessage> Messages,
        string Model,
        string ResponseId,
        DateTimeOffset? CreatedAt,
        LogUsage? Usage,
        IReadOnlyList<string> Warnings,
        int InvalidLineCount,
        bool Completed)
    {
        public static ResponseParseResult Empty(string warning)
        {
            return new ResponseParseResult([], string.Empty, string.Empty, null, null, [warning], 0, false);
        }
    }
}
