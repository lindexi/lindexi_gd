using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using AgentLib.Model;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class AgentAiChatServiceTests
{
    [TestMethod]
    public async Task GetReplyAsync_ReasoningAndText_MapsChunksAndSendsCompleteHistory()
    {
        using var temp = new TempDirectory();
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        ChatOptions? capturedOptions = null;
        var client = CreateClient((messages, options, cancellationToken) =>
        {
            capturedMessages = messages.ToArray();
            capturedOptions = options;
            return Updates(
                new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent("thinking")]),
                new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("answer")]));
        });
        var session = CreateHistory();
        var service = CreateService(temp, client);

        var chunks = await CollectAsync(service.GetReplyAsync(session, CancellationToken.None));

        CollectionAssert.AreEqual(
            new[]
            {
                new AiResponseChunk(AiResponsePart.Thought, "thinking"),
                new AiResponseChunk(AiResponsePart.Content, "answer"),
            },
            chunks);
        Assert.AreEqual(3, capturedMessages?.Count);
        Assert.AreEqual("system", capturedMessages?[0].Text);
        Assert.AreEqual("question", capturedMessages?[1].Text);
        Assert.AreEqual("old answer", capturedMessages?[2].Text);
        Assert.AreEqual(0, capturedOptions?.Tools?.Count ?? 0);
    }

    [TestMethod]
    public async Task GetReplyAsync_EmptyResponse_RetriesThreeTimesThenClassifiesEmptyResponse()
    {
        using var temp = new TempDirectory();
        var attempts = 0;
        var delays = new List<TimeSpan>();
        var client = CreateClient((_, _, _) =>
        {
            attempts++;
            return Updates();
        });
        var service = CreateService(temp, client, (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });

        var exception = await Assert.ThrowsAsync<AiChatException>(
            () => CollectAsync(service.GetReplyAsync(CreateHistory(), CancellationToken.None)));

        Assert.AreEqual(AiChatErrorCategory.EmptyResponse, exception.Category);
        Assert.AreEqual(3, attempts);
        CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }, delays);
    }

    [TestMethod]
    public async Task GetReplyAsync_UserCancellation_ThrowsOperationCanceledException()
    {
        using var temp = new TempDirectory();
        var client = CreateClient((_, _, cancellationToken) => WaitForCancellation(cancellationToken));
        var service = CreateService(temp, client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CollectAsync(service.GetReplyAsync(CreateHistory(), cancellation.Token)));
    }

    [TestMethod]
    public async Task GetReplyAsync_AuthenticationFailure_DoesNotRetry()
    {
        var result = await RunFailureAsync(new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized));

        Assert.AreEqual(AiChatErrorCategory.Authentication, result.Exception.Category);
        Assert.AreEqual(1, result.Attempts);
        Assert.AreEqual(0, result.Delays.Count);
    }

    [TestMethod]
    public async Task GetReplyAsync_RateLimitBeforeStreaming_RetriesMaximumAttempts()
    {
        var result = await RunFailureAsync(new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests));

        Assert.AreEqual(AiChatErrorCategory.RateLimit, result.Exception.Category);
        Assert.AreEqual(3, result.Attempts);
        CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }, result.Delays);
    }

    [TestMethod]
    public async Task GetReplyAsync_ServerAndNetworkFailures_AreRetryableAndClassified()
    {
        var server = await RunFailureAsync(new HttpRequestException("server", null, HttpStatusCode.ServiceUnavailable));
        var network = await RunFailureAsync(new HttpRequestException("network"));

        Assert.AreEqual(AiChatErrorCategory.Server, server.Exception.Category);
        Assert.AreEqual(AiChatErrorCategory.Network, network.Exception.Category);
        Assert.AreEqual(3, server.Attempts);
        Assert.AreEqual(3, network.Attempts);
    }

    [TestMethod]
    public async Task GetReplyAsync_FailureAfterFirstUpdate_DoesNotRetry()
    {
        using var temp = new TempDirectory();
        var attempts = 0;
        var client = CreateClient((_, _, _) => StreamThenThrow(() => attempts++));
        var service = CreateService(temp, client, (_, _) => Task.CompletedTask);
        var chunks = new List<AiResponseChunk>();

        var exception = await Assert.ThrowsAsync<AiChatException>(async () =>
        {
            await foreach (var chunk in service.GetReplyAsync(CreateHistory(), CancellationToken.None))
            {
                chunks.Add(chunk);
            }
        });

        Assert.AreEqual(AiChatErrorCategory.RateLimit, exception.Category);
        Assert.AreEqual(1, attempts);
        Assert.AreEqual("partial", chunks.Single().Delta);
    }

    private static AgentAiChatService CreateService(
        TempDirectory temp,
        TestChatClient client,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var modelService = new FakeAgentModelService { ChatClient = client };
        return new AgentAiChatService(
            modelService,
            new FakeSettingsService(temp.Path),
            new FakeAppLogger(),
            delay,
            _ => []);
    }

    private static TestChatClient CreateClient(
        Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> handler) =>
        new() { OnGetStreamingResponseAsync = handler };

    private static ChatSession CreateHistory()
    {
        var session = new ChatSession();
        session.Messages.Add(new ChatMessageViewModel(new CopilotChatMessage(ChatRole.System, "system")));
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateUser("question")));
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateAssistant("old answer", false)));
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateAssistant(string.Empty, false)));
        return session;
    }

    private static async Task<(AiChatException Exception, int Attempts, List<TimeSpan> Delays)> RunFailureAsync(Exception failure)
    {
        using var temp = new TempDirectory();
        var attempts = 0;
        var delays = new List<TimeSpan>();
        var client = CreateClient((_, _, _) => ThrowBeforeUpdate(failure, () => attempts++));
        var service = CreateService(temp, client, (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });

        var exception = await Assert.ThrowsAsync<AiChatException>(
            () => CollectAsync(service.GetReplyAsync(CreateHistory(), CancellationToken.None)));
        return (exception, attempts, delays);
    }

    private static async Task<List<AiResponseChunk>> CollectAsync(IAsyncEnumerable<AiResponseChunk> source)
    {
        var result = new List<AiResponseChunk>();
        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> Updates(params ChatResponseUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowBeforeUpdate(
        Exception exception,
        Action onAttempt)
    {
        onAttempt();
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamThenThrow(Action onAttempt)
    {
        onAttempt();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("partial")]);
        await Task.Yield();
        throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> WaitForCancellation(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        yield break;
    }
}