using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.DeepSeek;

using Microsoft.Extensions.AI;

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public sealed partial class DeepSeekChatClient : IChatClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly Uri _baseUri;
    private readonly string _apiKey;
    private readonly string _defaultModelId;
    private readonly bool _enableThinkingMode;
    private readonly int _reasoningBudgetTokens;
    private readonly ChatClientMetadata _metadata;

    public DeepSeekChatClient
    (
        string apiKey,
        string modelId,
        string baseUrl = "https://api.deepseek.com/v1",
        bool enableThinkingMode = true,
        int reasoningBudgetTokens = 8000,
        HttpClient? httpClient = null
    )
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(baseUrl));
        }

        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
        _baseUri = new Uri($"{baseUrl.AsSpan().TrimEnd('/')}/", UriKind.Absolute);
        _apiKey = apiKey;
        _defaultModelId = modelId;
        _enableThinkingMode = enableThinkingMode;
        _reasoningBudgetTokens = reasoningBudgetTokens;
        _metadata = new ChatClientMetadata("DeepSeek", _baseUri, modelId);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        List<AIContent> responseContents = [];
        UsageDetails? usage = null;
        ChatFinishReason? finishReason = null;
        string? responseId = null;
        string? modelId = null;
        DateTimeOffset? createdAt = null;
        object? rawRepresentation = null;
        ChatRole role = ChatRole.Assistant;

        await foreach (var update in GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.Role is { } updateRole)
            {
                role = updateRole;
            }

            responseId ??= update.ResponseId;
            modelId ??= update.ModelId;
            createdAt ??= update.CreatedAt;
            finishReason = update.FinishReason ?? finishReason;
            rawRepresentation = update.RawRepresentation ?? rawRepresentation;

            foreach (var content in update.Contents)
            {
                if (content is UsageContent usageContent)
                {
                    usage = usageContent.Details;
                    continue;
                }

                responseContents.Add(content);
            }
        }

        var responseMessage = new ChatMessage(role, responseContents)
        {
            CreatedAt = createdAt,
            MessageId = responseId,
            RawRepresentation = rawRepresentation,
        };

        return new ChatResponse(responseMessage)
        {
            ResponseId = responseId,
            ModelId = modelId ?? options?.ModelId ?? _defaultModelId,
            CreatedAt = createdAt,
            FinishReason = finishReason,
            Usage = usage,
            RawRepresentation = rawRepresentation,
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var requestJson = BuildRequest(messages, options);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, "chat/completions"))
        {
            Content = new StringContent(requestJson.ToJsonString(SerializerOptions), Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(MapErrorToMessage((int) response.StatusCode, errorBody));
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? responseId = null;
        string? modelId = null;
        DateTimeOffset? createdAt = null;
        ChatFinishReason? finishReason = null;
        UsageDetails? usage = null;
        var toolCallAccumulators = new Dictionary<int, ToolCallAccumulator>();

        await foreach (var chunk in ReadSseChunksAsync(reader, cancellationToken))
        {
            responseId ??= chunk.Id;
            modelId ??= chunk.Model;
            if (createdAt is null && chunk.Created is { } created)
            {
                createdAt = DateTimeOffset.FromUnixTimeSeconds(created);
            }

            var choice = chunk.Choices?.FirstOrDefault();
            if (choice?.Delta is { } delta)
            {
                List<AIContent> contents = [];

                if (!string.IsNullOrEmpty(delta.ReasoningContent))
                {
                    contents.Add(new TextReasoningContent(delta.ReasoningContent)
                    {
                        RawRepresentation = chunk,
                    });
                }

                if (!string.IsNullOrEmpty(delta.Content))
                {
                    contents.Add(new TextContent(delta.Content)
                    {
                        RawRepresentation = chunk,
                    });
                }

                if (delta.ToolCalls is { Length: > 0 })
                {
                    foreach (var toolCall in delta.ToolCalls)
                    {
                        if (!toolCallAccumulators.TryGetValue(toolCall.Index, out var accumulator))
                        {
                            accumulator = new ToolCallAccumulator();
                            toolCallAccumulators[toolCall.Index] = accumulator;
                        }

                        accumulator.Apply(toolCall);
                    }
                }

                if (contents.Count > 0)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, contents)
                    {
                        ResponseId = responseId,
                        ModelId = modelId,
                        CreatedAt = createdAt,
                        RawRepresentation = chunk,
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(choice?.FinishReason))
            {
                finishReason = ParseFinishReason(choice.FinishReason);
            }

            if (chunk.Usage is { } chunkUsage)
            {
                usage = CreateUsageDetails(chunkUsage);
            }
        }

        List<AIContent> finalContents = [];
        foreach (var toolCall in toolCallAccumulators.OrderBy(static pair => pair.Key).Select(static pair => pair.Value))
        {
            finalContents.Add(toolCall.ToFunctionCallContent());
        }

        if (usage is not null)
        {
            finalContents.Add(new UsageContent(usage));
        }

        if (finalContents.Count > 0 || finishReason is not null)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, finalContents)
            {
                ResponseId = responseId,
                ModelId = modelId ?? options?.ModelId ?? _defaultModelId,
                CreatedAt = createdAt,
                FinishReason = finishReason,
            };
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        if (serviceType.IsInstanceOfType(_metadata))
        {
            return _metadata;
        }

        if (serviceType.IsInstanceOfType(_httpClient))
        {
            return _httpClient;
        }

        return null;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private JsonObject BuildRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var request = new JsonObject
        {
            ["model"] = options?.ModelId ?? _defaultModelId,
            ["stream"] = true,
            ["stream_options"] = new JsonObject { ["include_usage"] = true },
            ["messages"] = BuildMessages(messages, options),
        };

        if (options?.Temperature is { } temperature)
        {
            request["temperature"] = JsonValue.Create(temperature);
        }

        if (options?.TopP is { } topP)
        {
            request["top_p"] = JsonValue.Create(topP);
        }

        if (options?.MaxOutputTokens is { } maxOutputTokens)
        {
            request["max_tokens"] = JsonValue.Create(maxOutputTokens);
        }

        if (options?.StopSequences is { Count: > 0 } stopSequences)
        {
            var stop = new JsonArray();
            foreach (var stopSequence in stopSequences)
            {
                stop.Add(stopSequence);
            }

            request["stop"] = stop;
        }

        if (_enableThinkingMode)
        {
            request["thinking"] = new JsonObject
            {
                ["type"] = "enabled",
                ["budget_tokens"] = _reasoningBudgetTokens,
            };
        }

        var tools = BuildTools(options?.Tools);
        if (tools.Count > 0)
        {
            request["tools"] = tools;
            request["tool_choice"] = "auto";
        }

        return request;
    }

    private static JsonArray BuildMessages(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        JsonArray jsonMessages = [];

        if (!string.IsNullOrWhiteSpace(options?.Instructions))
        {
            jsonMessages.Add(new JsonObject
            {
                ["role"] = "system",
                ["content"] = options.Instructions,
            });
        }

        foreach (var message in messages)
        {
            foreach (var jsonMessage in BuildMessages(message))
            {
                jsonMessages.Add(jsonMessage);
            }
        }

        return jsonMessages;
    }

    private static IEnumerable<JsonObject> BuildMessages(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Role == ChatRole.Tool)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionResultContent functionResult)
                {
                    yield return new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = functionResult.CallId,
                        ["content"] = SerializeToolResult(functionResult.Result),
                    };
                }
            }

            yield break;
        }

        string? text = null;
        string? reasoning = null;
        JsonArray? toolCalls = null;

        if (message.Contents.Count > 0)
        {
            var textBuilder = new StringBuilder();
            var reasoningBuilder = new StringBuilder();

            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                        textBuilder.Append(textContent.Text);
                        break;
                    case TextReasoningContent reasoningContent when !string.IsNullOrEmpty(reasoningContent.Text):
                        reasoningBuilder.Append(reasoningContent.Text);
                        break;
                    case FunctionCallContent functionCall:
                        toolCalls ??= [];
                        toolCalls.Add(new JsonObject
                        {
                            ["id"] = functionCall.CallId,
                            ["type"] = "function",
                            ["function"] = new JsonObject
                            {
                                ["name"] = functionCall.Name,
#if NET8_0_OR_GREATER
                                ["arguments"] = JsonSerializer.Serialize(functionCall.Arguments, SourceGenerationContext.Default.Options),
#else
                                ["arguments"] = JsonSerializer.Serialize(functionCall.Arguments, SerializerOptions),
#endif
                            },
                        });
                        break;
                }
            }

            text = textBuilder.Length > 0 ? textBuilder.ToString() : null;
            reasoning = reasoningBuilder.Length > 0 ? reasoningBuilder.ToString() : null;
        }
        else if (!string.IsNullOrWhiteSpace(message.Text))
        {
            text = message.Text;
        }

        var role = message.Role == ChatRole.System ? "system"
            : message.Role == ChatRole.Assistant ? "assistant"
            : "user";

        yield return new JsonObject
        {
            ["role"] = role,
            ["content"] = text,
            ["reasoning_content"] = reasoning,
            ["tool_calls"] = toolCalls,
        };
    }

    private static JsonArray BuildTools(IList<AITool>? tools)
    {
        JsonArray jsonTools = [];
        if (tools is null)
        {
            return jsonTools;
        }

        foreach (var tool in tools)
        {
            if (tool is not AIFunctionDeclaration function)
            {
                continue;
            }

            jsonTools.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = function.Name,
                    ["description"] = function.Description,
                    ["parameters"] = ToJsonNode(function.JsonSchema) ?? new JsonObject(),
                },
            });
        }

        return jsonTools;
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        return value switch
        {
            null => null,
            JsonNode node => node.DeepClone(),
            JsonElement element => JsonNode.Parse(element.GetRawText()),
            JsonDocument document => JsonNode.Parse(document.RootElement.GetRawText()),
            string text when !string.IsNullOrWhiteSpace(text) => JsonNode.Parse(text),
#if NET8_0_OR_GREATER
            _ => JsonSerializer.SerializeToNode(value, SourceGenerationContext.Default.Options),
#else
            _ => JsonSerializer.SerializeToNode(value, SerializerOptions),
#endif
        };
    }

    private static string SerializeToolResult(object? result)
    {
        return result switch
        {
            null => "null",
            string text => text,
            JsonElement element => element.GetRawText(),
            JsonDocument document => document.RootElement.GetRawText(),
#if NET8_0_OR_GREATER
            _ => JsonSerializer.Serialize(result, SourceGenerationContext.Default.Options),
#else
            _ => JsonSerializer.Serialize(result, SerializerOptions),
#endif
        };
    }

    private static async IAsyncEnumerable<DeepSeekChatChunk> ReadSseChunksAsync(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
#if NET8_0_OR_GREATER
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
#endif
            if (line is null)
            {
                yield break;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line["data: ".Length..];
            if (payload == "[DONE]")
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            DeepSeekChatChunk? chunk;
            try
            {
#if NET8_0_OR_GREATER
                chunk = JsonSerializer.Deserialize(payload, SourceGenerationContext.Default.DeepSeekChatChunk);
#else
                chunk = JsonSerializer.Deserialize<DeepSeekChatChunk>(payload, SerializerOptions);
#endif
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk is not null)
            {
                yield return chunk;
            }
        }
    }

    private static UsageDetails CreateUsageDetails(DeepSeekUsage usage)
    {
        var usageDetails = new UsageDetails
        {
            InputTokenCount = usage.PromptTokens,
            OutputTokenCount = usage.CompletionTokens,
            TotalTokenCount = usage.TotalTokens > 0 ? usage.TotalTokens : usage.PromptTokens + usage.CompletionTokens,
            CachedInputTokenCount = usage.PromptCacheHitTokens,
            ReasoningTokenCount = usage.CompletionTokensDetails?.ReasoningTokens,
        };

        if (usage.PromptCacheMissTokens > 0)
        {
            usageDetails.AdditionalCounts = new AdditionalPropertiesDictionary<long>
            {
                ["prompt_cache_miss_tokens"] = usage.PromptCacheMissTokens,
            };
        }

        return usageDetails;
    }

    private static ChatFinishReason? ParseFinishReason(string? finishReason)
    {
        return finishReason switch
        {
            null => null,
            "stop" => ChatFinishReason.Stop,
            "length" => ChatFinishReason.Length,
            "tool_calls" => ChatFinishReason.ToolCalls,
            "content_filter" => ChatFinishReason.ContentFilter,
            _ => new ChatFinishReason(finishReason),
        };
    }

    private static string MapErrorToMessage(int statusCode, string responseBody)
    {
        string? errorCode = null;
        string? errorMessage = null;

        try
        {
#if NET8_0_OR_GREATER
            var error = JsonSerializer.Deserialize(responseBody, SourceGenerationContext.Default.DeepSeekErrorResponse);
#else
            var error = JsonSerializer.Deserialize<DeepSeekErrorResponse>(responseBody, SerializerOptions);
#endif
            errorCode = error?.Error?.Code;
            errorMessage = error?.Error?.Message;
        }
        catch (JsonException)
        {
        }

        var detail = errorCode switch
        {
            "insufficient_balance" => "账户余额不足,请充值后重试。",
            "rate_limit_exceeded" => "请求速率超限,请稍后重试。",
            "context_length_exceeded" => "输入内容超出模型上下文限制。",
            "invalid_api_key" => "API Key 无效,请检查配置。",
            "model_not_found" => "模型不存在,请检查模型名称。",
            "server_error" => "服务端内部错误,请稍后重试。",
            _ => null,
        };

        if (detail is not null)
        {
            return $"API 错误（HTTP {statusCode}, code={errorCode}）：{detail}";
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            return $"API 错误（HTTP {statusCode}）：{errorMessage}";
        }

        return $"API 请求失败,状态码 {statusCode}。";
    }

#if NET8_0_OR_GREATER
    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(DeepSeekChatChunk))]
    [JsonSerializable(typeof(DeepSeekErrorResponse))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(IDictionary<string, object>), GenerationMode = JsonSourceGenerationMode.Serialization)]
    private partial class SourceGenerationContext : JsonSerializerContext
    {
    }
#endif

    private sealed class ToolCallAccumulator
    {
        private string? _id;
        private string? _name;
        private readonly StringBuilder _arguments = new();

        public void Apply(DeepSeekToolCallDelta delta)
        {
            if (!string.IsNullOrWhiteSpace(delta.Id))
            {
                _id = delta.Id;
            }

            if (!string.IsNullOrWhiteSpace(delta.Function?.Name))
            {
                _name = delta.Function.Name;
            }

            if (!string.IsNullOrEmpty(delta.Function?.Arguments))
            {
                _arguments.Append(delta.Function.Arguments);
            }
        }

        public FunctionCallContent ToFunctionCallContent()
        {
            var argumentsJson = _arguments.Length > 0 ? _arguments.ToString() : "{}";
            return new FunctionCallContent(
                _id ?? Guid.NewGuid().ToString("N"),
                _name ?? string.Empty,
                ParseArguments(argumentsJson))
            {
                RawRepresentation = argumentsJson,
            };
        }

        private static Dictionary<string, object?> ParseArguments(string argumentsJson)
        {
            try
            {
#if NET8_0_OR_GREATER
                var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson, SourceGenerationContext.Default.Options);
#else
                var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson, SerializerOptions);
#endif
                if (json is null)
                {
                    return [];
                }

                return json.ToDictionary(static pair => pair.Key, static pair => (object?) pair.Value.Clone(), StringComparer.Ordinal);
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    private sealed class DeepSeekChatChunk
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("choices")]
        public DeepSeekChoice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public DeepSeekUsage? Usage { get; set; }
    }

    private sealed class DeepSeekChoice
    {
        [JsonPropertyName("delta")]
        public DeepSeekDelta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private sealed class DeepSeekDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }

        [JsonPropertyName("tool_calls")]
        public DeepSeekToolCallDelta[]? ToolCalls { get; set; }
    }

    private sealed class DeepSeekToolCallDelta
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("function")]
        public DeepSeekFunctionDelta? Function { get; set; }
    }

    private sealed class DeepSeekFunctionDelta
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
    }

    private sealed class DeepSeekUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_cache_hit_tokens")]
        public int PromptCacheHitTokens { get; set; }

        [JsonPropertyName("prompt_cache_miss_tokens")]
        public int PromptCacheMissTokens { get; set; }

        [JsonPropertyName("completion_tokens_details")]
        public DeepSeekCompletionTokensDetails? CompletionTokensDetails { get; set; }
    }

    private sealed class DeepSeekCompletionTokensDetails
    {
        [JsonPropertyName("reasoning_tokens")]
        public int? ReasoningTokens { get; set; }
    }

    private sealed class DeepSeekErrorResponse
    {
        [JsonPropertyName("error")]
        public DeepSeekError? Error { get; set; }
    }

    private sealed class DeepSeekError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
