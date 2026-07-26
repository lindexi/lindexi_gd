using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgentLib.Tools;
using DeepSeekWpf.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

namespace DeepSeekWpf.Services;

public sealed class AgentAiChatService : IAiChatService
{
    private const int MaximumAttempts = 3;
    private readonly IAgentModelService _agentModelService;
    private readonly ISettingsService _settingsService;
    private readonly IAppLogger _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Func<string, IReadOnlyList<AITool>> _workspaceToolsFactory;

    public AgentAiChatService(
        IAgentModelService agentModelService,
        ISettingsService settingsService,
        IAppLogger logger,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Func<string, IReadOnlyList<AITool>>? workspaceToolsFactory = null)
    {
        _agentModelService = agentModelService;
        _settingsService = settingsService;
        _logger = logger;
        _delayAsync = delayAsync ?? Task.Delay;
        _workspaceToolsFactory = workspaceToolsFactory ?? CreateWorkspaceTools;
    }

    public async IAsyncEnumerable<AiResponseChunk> GetReplyAsync(
        ChatSession session,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var correlationId = Guid.NewGuid().ToString("N");
        var modelSpecifier = _agentModelService.SelectedModel?.Specifier ?? "unselected";
        var stopwatch = Stopwatch.StartNew();
        var messages = CreateChatHistory(session);
        using var timeoutCancellation = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(1, _settingsService.CurrentSettings.ChatRequestTimeoutSeconds)));
        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);

        await LogAttemptAsync("started", session.Id, modelSpecifier, correlationId, 1, null, stopwatch.Elapsed);

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            var attemptProducedContent = false;
            var attemptStartedStreaming = false;
            Exception? failure = null;
            await using var enumerator = StreamAttemptAsync(
                    messages,
                    () => attemptStartedStreaming = true,
                    requestCancellation.Token)
                .GetAsyncEnumerator(requestCancellation.Token);

            while (true)
            {
                AiResponseChunk? chunk = null;
                bool movedNext;
                try
                {
                    movedNext = await enumerator.MoveNextAsync();
                    if (movedNext)
                    {
                        chunk = enumerator.Current;
                        attemptProducedContent = true;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }

                if (!movedNext)
                {
                    break;
                }

                yield return chunk!;
            }

            if (failure is null)
            {
                if (!attemptProducedContent)
                {
                    failure = new EmptyAiResponseException();
                }
                else
                {
                    await LogAttemptAsync("completed", session.Id, modelSpecifier, correlationId, attempt, null, stopwatch.Elapsed);
                    yield break;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                await LogAttemptAsync(
                    "canceled",
                    session.Id,
                    modelSpecifier,
                    correlationId,
                    attempt,
                    AiChatErrorCategory.Canceled,
                    stopwatch.Elapsed);
                throw new OperationCanceledException(cancellationToken);
            }

            var domainException = MapException(failure, correlationId, timeoutCancellation.IsCancellationRequested);
            await LogAttemptAsync(
                attempt < MaximumAttempts && domainException.IsRetryable && !attemptStartedStreaming ? "retrying" : "failed",
                session.Id,
                modelSpecifier,
                correlationId,
                attempt,
                domainException.Category,
                stopwatch.Elapsed);

            if (attemptStartedStreaming || !domainException.IsRetryable || attempt == MaximumAttempts)
            {
                throw domainException;
            }

            try
            {
                await _delayAsync(TimeSpan.FromSeconds(attempt), requestCancellation.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                throw MapException(exception, correlationId, timeoutCancellation.IsCancellationRequested);
            }

            await LogAttemptAsync("started", session.Id, modelSpecifier, correlationId, attempt + 1, null, stopwatch.Elapsed);
        }
    }

    private async IAsyncEnumerable<AiResponseChunk> StreamAttemptAsync(
        IReadOnlyList<ChatMessage> messages,
        Action onUpdateReceived,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IChatClient chatClient = await _agentModelService.GetSelectedChatClientAsync().ConfigureAwait(false);
        try
        {
            var tools = _workspaceToolsFactory(_settingsService.CurrentSettings.DataPath);
            var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Tools = [.. tools],
                },
            });

            await foreach (var update in agent.RunStreamingAsync(messages, cancellationToken: cancellationToken)
                               .ConfigureAwait(false))
            {
                onUpdateReceived();
                var textFromContents = false;
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextReasoningContent reasoningContent when !string.IsNullOrEmpty(reasoningContent.Text):
                            yield return new AiResponseChunk(AiResponsePart.Thought, reasoningContent.Text);
                            break;
                        case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                            textFromContents = true;
                            yield return new AiResponseChunk(AiResponsePart.Content, textContent.Text);
                            break;
                    }
                }

                if (!textFromContents && !string.IsNullOrEmpty(update.Text))
                {
                    yield return new AiResponseChunk(AiResponsePart.Content, update.Text);
                }
            }
        }
        finally
        {
            if (chatClient is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static IReadOnlyList<ChatMessage> CreateChatHistory(ChatSession session)
    {
        var messages = new List<ChatMessage>(session.Messages.Count);
        for (var index = 0; index < session.Messages.Count; index++)
        {
            var message = session.Messages[index];
            var isCurrentAssistantPlaceholder = index == session.Messages.Count - 1
                && message.Role == ChatRole.Assistant
                && string.IsNullOrWhiteSpace(message.Content)
                && string.IsNullOrWhiteSpace(message.ThoughtContent);
            if (isCurrentAssistantPlaceholder)
            {
                continue;
            }

            messages.Add(new ChatMessage(message.Role, message.Content ?? string.Empty));
        }

        return messages;
    }

    private static IReadOnlyList<AITool> CreateWorkspaceTools(string workspacePath)
    {
        var workspaceTools = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath,
        };
        return workspaceTools.CreateDefaultTools();
    }

    private static AiChatException MapException(
        Exception exception,
        string correlationId,
        bool requestTimedOut)
    {
        if (exception is AiChatException domainException)
        {
            return domainException;
        }

        if (requestTimedOut || exception is TimeoutException or TaskCanceledException)
        {
            return Create(AiChatErrorCategory.Timeout, "请求模型超时，请稍后重试。", true);
        }

        if (exception is EmptyAiResponseException)
        {
            return Create(AiChatErrorCategory.EmptyResponse, "模型返回了空响应，请重试。", true);
        }

        if (exception is HttpRequestException httpException)
        {
            return MapHttpStatus(httpException.StatusCode, httpException);
        }

        if (exception is InvalidOperationException invalidOperationException
            && TryExtractHttpStatus(invalidOperationException.Message, out var statusCode))
        {
            return MapHttpStatus(statusCode, invalidOperationException);
        }

        if (exception is JsonException or FormatException)
        {
            return Create(AiChatErrorCategory.Protocol, "模型响应格式无效，请检查服务兼容性。", false);
        }

        if (exception is IOException or UnauthorizedAccessException)
        {
            return Create(AiChatErrorCategory.Storage, "访问本地 Agent 工作区失败，请检查目录权限。", false);
        }

        if (exception is ArgumentException
            || exception is InvalidOperationException configurationException
                && IsConfigurationError(configurationException.Message))
        {
            return Create(AiChatErrorCategory.Configuration, "模型配置不可用，请检查并重新加载 Agent 配置。", false);
        }

        if (exception is OperationCanceledException)
        {
            return Create(AiChatErrorCategory.Canceled, "请求已取消。", false);
        }

        return Create(AiChatErrorCategory.Unknown, "调用模型时发生未知错误，请稍后重试。", false);

        AiChatException Create(AiChatErrorCategory category, string message, bool isRetryable) =>
            new(category, message, correlationId, isRetryable, exception);

        AiChatException MapHttpStatus(HttpStatusCode? statusCode, Exception innerException)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new AiChatException(AiChatErrorCategory.Authentication, "模型服务认证失败，请检查凭据。", correlationId, false, innerException),
                HttpStatusCode.TooManyRequests =>
                    new AiChatException(AiChatErrorCategory.RateLimit, "请求过于频繁，请稍后重试。", correlationId, true, innerException),
                >= HttpStatusCode.InternalServerError =>
                    new AiChatException(AiChatErrorCategory.Server, "模型服务暂时不可用，请稍后重试。", correlationId, true, innerException),
                null => new AiChatException(AiChatErrorCategory.Network, "无法连接模型服务，请检查网络。", correlationId, true, innerException),
                _ => new AiChatException(AiChatErrorCategory.Protocol, "模型服务拒绝了请求，请检查请求与模型兼容性。", correlationId, false, innerException),
            };
        }
    }

    private ValueTask LogAttemptAsync(
        string state,
        Guid sessionId,
        string modelSpecifier,
        string correlationId,
        int attempt,
        AiChatErrorCategory? category,
        TimeSpan elapsed)
    {
        return _logger.InformationAsync(
            $"AgentChat state={state} correlationId={correlationId} sessionId={sessionId} model={modelSpecifier} attempt={attempt} category={category?.ToString() ?? "None"} elapsedMs={elapsed.TotalMilliseconds:F0}");
    }

    private static bool IsConfigurationError(string message)
    {
        return message.Contains("配置", StringComparison.OrdinalIgnoreCase)
            || message.Contains("模型", StringComparison.OrdinalIgnoreCase)
            || message.Contains("API Key", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExtractHttpStatus(string message, out HttpStatusCode statusCode)
    {
        var match = Regex.Match(
            message,
            @"(?:HTTP\s*|状态码\s*)(\d{3})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success
            && int.TryParse(match.Groups[1].Value, out var value)
            && Enum.IsDefined(typeof(HttpStatusCode), value))
        {
            statusCode = (HttpStatusCode)value;
            return true;
        }

        statusCode = default;
        return false;
    }

    private sealed class EmptyAiResponseException : Exception;
}