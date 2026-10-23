using System.Net;
using System.Text;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class ResponsesCompactionTests
{
    [TestMethod]
    [DataRow(200)]
    [DataRow(400)]
    public async Task CompactShouldApplyOnlySuccessfulServerOutput(int status)
    {
        using var handler = new CompactHandler(status);
        using var http = new HttpClient(handler);
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        var context = await manager.CreateManualSendMessageContextAsync();
        await using var coding = new CodingAgent();
        var agent = new ResponsesCodingAgent(coding);
        var state = manager.SelectedSession.ResponsesSession;
        var original = ResponseItem.CreateUserMessageItem("original");
        state.AppendItem(original);
        if (status == 400)
        {
            await Assert.ThrowsAsync<System.ClientModel.ClientResultException>(() =>
                agent.TryCompactConversationAsync(context.LanguageModel, state));
            Assert.AreSame(original, state.Items.Single());
        }
        else
        {
            Assert.IsTrue(await agent.TryCompactConversationAsync(context.LanguageModel, state));
            string json = System.ClientModel.Primitives.ModelReaderWriter.Write(state.Items.Single()).ToString();
            StringAssert.Contains(json, "encrypted_content");
        }
    }

    [TestMethod]
    public void CompactionItemShouldPreserveContentInSerializedRequest()
    {
        const string json = """
            {"id":"cmp_0717d2cec8761b27b45b6537fd0abf5b","type":"compaction","status":"completed","encrypted_content":"# Technical Handoff: ORCHID-731\n\n## Context \u0026 Intent\n- Identifier to remember: **ORCHID-731** (per user instruction).\n- User explicitly required: memorize the identifier and respond only with confirmation, no tool invocation.\n- Current session state: no code tasks, no file modifications, no investigations performed.\n\n## Critical Entities\n- Identifier: `ORCHID-731`\n- Workspace: untouched (no reads/writes this session).\n- Tool calls made this session: 0.\n\n## Pitfalls \u0026 Resolutions\n- None encountered. Prior turn required zero-tool compliance — observe the same constraint during context handoff (this response itself contains no tool invocations).\n\n## Current State\n- Memory of `ORCHID-731` recorded in this conversation thread.\n- No filesystem, build, or test changes pending.\n\n## Immediate Next Action\n- Await the next user instruction. Do not proactively start any task; do not invoke any tools."}
            """;
        var item = System.ClientModel.Primitives.ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(json))
            ?? throw new InvalidDataException("Item missing.");
        var request = new CreateResponseOptions { Model = "test-model" };
        request.InputItems.Add(item);
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("Repeat the identifier."));
        using var expected = System.Text.Json.JsonDocument.Parse(json);
        using var actual = System.Text.Json.JsonDocument.Parse(System.ClientModel.Primitives.ModelReaderWriter.Write(request));
        Console.WriteLine($"SDK type: {item.GetType().FullName}, kind: {item.Kind}");
        Console.WriteLine($"Serialized request: {actual.RootElement.GetRawText()}");

        Assert.AreEqual(expected.RootElement.GetProperty("encrypted_content").GetString(),
            actual.RootElement.GetProperty("input")[0].GetProperty("encrypted_content").GetString());
    }

    [TestMethod]
    public async Task CompactWithoutHistoryShouldNotSendRequest()
    {
        using var handler = new ControlledCompactHandler();
        using var http = new HttpClient(handler);
        var model = await CreateModelAsync(http);
        await using var coding = new CodingAgent();
        var agent = new ResponsesCodingAgent(coding);

        Assert.IsFalse(await agent.TryCompactConversationAsync(model, new CopilotResponsesSession()));
        Assert.AreEqual(0, handler.RequestCount);
    }

    [TestMethod]
    public async Task CompactCancellationShouldPreserveOriginalHistory()
    {
        using var handler = new ControlledCompactHandler { WaitForCancellation = true };
        using var http = new HttpClient(handler);
        var model = await CreateModelAsync(http);
        await using var coding = new CodingAgent();
        var agent = new ResponsesCodingAgent(coding);
        var state = new CopilotResponsesSession();
        var original = ResponseItem.CreateUserMessageItem("original");
        state.AppendItem(original);
        using var cancellation = new CancellationTokenSource();
        Task<bool> compact = agent.TryCompactConversationAsync(model, state, cancellationToken: cancellation.Token);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await compact.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreSame(original, state.Items.Single());
    }

    [TestMethod]
    public async Task CompactMalformedOutputShouldPreserveOriginalHistory()
    {
        using var handler = new ControlledCompactHandler
        {
            Body = """{"output":[{"type":"compaction","encrypted_content":"valid"},null]}""",
        };
        using var http = new HttpClient(handler);
        var model = await CreateModelAsync(http);
        await using var coding = new CodingAgent();
        var agent = new ResponsesCodingAgent(coding);
        var state = new CopilotResponsesSession();
        var original = ResponseItem.CreateUserMessageItem("original");
        state.AppendItem(original);

        await Assert.ThrowsAsync<InvalidDataException>(() => agent.TryCompactConversationAsync(model, state));
        Assert.AreSame(original, state.Items.Single());
    }

    [TestMethod]
    public async Task CompactShouldReplaceEntireHistoryInServerOutputOrder()
    {
        using var handler = new ControlledCompactHandler
        {
            Body = """{"output":[{"type":"message","role":"user","content":[{"type":"input_text","text":"retained"}]},{"type":"compaction","id":"cmp_test","encrypted_content":"opaque-state"}]}""",
        };
        using var http = new HttpClient(handler);
        var model = await CreateModelAsync(http);
        await using var coding = new CodingAgent();
        var agent = new ResponsesCodingAgent(coding);
        var state = new CopilotResponsesSession();
        state.AppendItem(ResponseItem.CreateUserMessageItem("old"));
        state.AppendItem(ResponseItem.CreateAssistantMessageItem("old reply"));
        await agent.TryCompactConversationAsync(model, state);
        using var expected = System.Text.Json.JsonDocument.Parse(handler.Body);
        var actual = state.Items.Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString()).ToArray();

        CollectionAssert.AreEqual(expected.RootElement.GetProperty("output").EnumerateArray()
            .Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(
                System.ClientModel.Primitives.ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item.GetRawText()))!).ToString()).ToArray(), actual);
    }

    private static async Task<ILanguageModel> CreateModelAsync(HttpClient http)
    {
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        return (await manager.CreateManualSendMessageContextAsync()).LanguageModel;
    }

    private sealed class ControlledCompactHandler : HttpMessageHandler
    {
        public string Body { get; init; } = """{"output":[]}""";
        public bool WaitForCancellation { get; init; }
        public int RequestCount { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Started.TrySetResult();
            if (WaitForCancellation) await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class CompactHandler(int status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith("/responses/compact", StringComparison.Ordinal));
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent(status == 200
                    ? """{"output":[{"type":"compaction","id":"cmp_test","encrypted_content":"opaque-state"}]}"""
                    : """{"error":{"message":"compact unavailable","type":"invalid_request_error"}}""",
                    Encoding.UTF8, "application/json"),
            });
        }
    }
}
