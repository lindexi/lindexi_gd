using System.Net;
using System.Text;
using System.Text.Json;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class ResponsesCodingAgentTests
{
    [TestMethod]
    [DataRow("explicit-model", null, "explicit-model", false)]
    [DataRow(null, null, "demo", false)]
    [DataRow(null, "mapped-model", "mapped-model", false)]
    [DataRow("explicit-model", "mapped-model", "explicit-model", false)]
    [DataRow(null, null, "demo", true)]
    [DataRow(null, "mapped-model", "mapped-model", true)]
    public async Task RunAsyncShouldUseResolvedModelAcrossRequestsAsync(string? modelId, string? mappedId, string expectedId, bool toolCall)
    {
        using var handler = new StreamingHandler { ReturnToolCall = toolCall, TotalTokens = 300000, OutputTokens = 10000 };
        using var http = new HttpClient(handler);
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        var provider = new JsonConfigurationOpenAIProtocolLanguageModelProvider(
            new OpenAIProtocolLanguageModelConfiguration("https://example.com/v1", "test-key")
            {
                ModelDefinitions = [new AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition
                {
                    Provider = "test", ModelName = "demo", ModelId = modelId,
                }],
            }, manager.AgentApiEndpointManager)
        {
            ModelNameToIdMap = new DictionaryModelNameToIdMap(mappedId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["demo"] = mappedId }),
        };
        manager.AgentApiEndpointManager.RegisterLanguageModelProvider(provider);
        string workspace = Path.Combine(Path.GetTempPath(), $"responses-model-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        Console.WriteLine(workspace);
        await using var coding = new CodingAgent(new CodingAgentOptions { AdditionalToolSources = [new TestToolSource()] });
        var agent = new ResponsesCodingAgent(coding);
        var context = await manager.CreateManualSendMessageContextAsync();

        var run = await agent.RunAsync(context, [new TextContent("calculate")],
            manager.SelectedSession.ResponsesSession, workspace, new CodingChatRunOptions(true, false, null));
        await run.CompletionTask;
        await agent.TryCompactConversationAsync(context.LanguageModel, manager.SelectedSession.ResponsesSession);

        var actualIds = handler.Requests.Concat(handler.CompactRequests).Select(json =>
        {
            using var request = JsonDocument.Parse(json);
            return request.RootElement.GetProperty("model").GetString();
        }).ToArray();
        CollectionAssert.AreEqual(Enumerable.Repeat(expectedId, toolCall ? 4 : 2).ToArray(), actualIds);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RunAsyncShouldContinueUsingNativeHistory(bool nullTerminalAnnotations)
    {
        using var handler = new StreamingHandler { NullTerminalAnnotations = nullTerminalAnnotations };
        using var http = new HttpClient(handler);
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        await using var runtime = new CodingAgent();
        var agent = new ResponsesCodingAgent(runtime);
        var state = manager.SelectedSession.ResponsesSession;
        var first = await agent.RunAsync(await manager.CreateManualSendMessageContextAsync(),
            [new Microsoft.Extensions.AI.TextContent("first")], state, null, new CodingChatRunOptions(false, false, ReasoningEffort.High));
        await first.CompletionTask;
        var second = await agent.RunAsync(await manager.CreateManualSendMessageContextAsync(),
            [new Microsoft.Extensions.AI.TextContent("second")], state, null);
        await second.CompletionTask;

        using var request = JsonDocument.Parse(handler.Requests[1]);
        Assert.AreEqual(3, request.RootElement.GetProperty("input").GetArrayLength());
        using var firstRequest = JsonDocument.Parse(handler.Requests[0]);
        Assert.AreEqual("high", firstRequest.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
    }

    [TestMethod]
    [DataRow(false, 300000, 10000, 0)]
    [DataRow(true, 199998, 10000, 0)]
    [DataRow(true, 200000, 9999, 0)]
    [DataRow(true, 200000, 10000, 1)]
    [DataRow(true, 300000, 1, 1)]
    public async Task RunAsyncShouldSubmitActualToolResult(bool automaticCompression, int totalTokens, int outputTokens, int expectedCompactions)
    {
        string path = Path.Combine(Path.GetTempPath(), $"responses-workspace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        Console.WriteLine(path);
        using var handler = new StreamingHandler { ReturnToolCall = true, TotalTokens = totalTokens, OutputTokens = outputTokens };
        using var http = new HttpClient(handler);
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        await using var runtime = new CodingAgent(new CodingAgentOptions
        {
            AdditionalToolSources = [new TestToolSource()],
        });
        var agent = new ResponsesCodingAgent(runtime);

        var run = await agent.RunAsync(await manager.CreateManualSendMessageContextAsync(),
            [new TextContent("calculate")], manager.SelectedSession.ResponsesSession, path,
            new CodingChatRunOptions(automaticCompression, false, null));
        await run.CompletionTask;

        Assert.AreEqual(expectedCompactions, handler.CompactRequests.Count);
        using var request = JsonDocument.Parse(expectedCompactions == 1 ? handler.CompactRequests.Single() : handler.Requests[1]);
        Assert.AreEqual("5", request.RootElement.GetProperty("input").EnumerateArray()
            .Single(item => item.GetProperty("type").GetString() == "function_call_output")
            .GetProperty("output").GetString());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task InjectedMessagesShouldContinueAtRequestBoundary(bool toolCall)
    {
        string path = Path.Combine(Path.GetTempPath(), $"responses-injection-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        using var handler = new StreamingHandler { ReturnToolCall = toolCall, BlockFirstRequest = true };
        using var http = new HttpClient(handler);
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        await using var coding = new CodingAgent(new CodingAgentOptions { AdditionalToolSources = [new TestToolSource()] });
        var agent = new ResponsesCodingAgent(coding);
        var run = await agent.RunAsync(await manager.CreateManualSendMessageContextAsync(),
            [new TextContent("initial")], manager.SelectedSession.ResponsesSession, path);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await run.InjectMessageAsync([new TextContent("interrupt-one")]);
        await run.InjectMessageAsync([new TextContent("interrupt-two")]);
        handler.Release.TrySetResult();
        await run.CompletionTask.WaitAsync(TimeSpan.FromSeconds(5));

        using var request = JsonDocument.Parse(handler.Requests[1]);
        string[] userTexts = request.RootElement.GetProperty("input").EnumerateArray()
            .Where(item => item.GetProperty("type").GetString() == "message" && item.GetProperty("role").GetString() == "user")
            .Select(item => item.GetProperty("content")[0].GetProperty("text").GetString()!).ToArray();
        CollectionAssert.AreEqual(new[] { "initial", "interrupt-one", "interrupt-two" }, userTexts);
        Assert.HasCount(0, handler.CompactRequests);
    }

    private sealed class TestToolSource : ICodingWorkspaceToolSource
    {
        public IReadOnlyList<AITool> CreateTools(string workspacePath) =>
            [AIFunctionFactory.Create((int left, int right) => left + right, "add")];
    }

    private sealed class StreamingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = new();
        public List<string> CompactRequests { get; } = new();
        public bool ReturnToolCall { get; init; }
        public int TotalTokens { get; init; }
        public int OutputTokens { get; init; }
        public bool NullTerminalAnnotations { get; init; }
        public bool BlockFirstRequest { get; init; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/responses/compact", StringComparison.Ordinal))
            {
                CompactRequests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"output":[{"type":"compaction","id":"cmp_test","encrypted_content":"opaque"}]}""",
                        Encoding.UTF8, "application/json"),
                };
            }
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (BlockFirstRequest && Requests.Count == 1)
            {
                Started.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            string id = $"resp_{Requests.Count}";
            string response = JsonSerializer.Serialize(new
            {
                id, status = "completed",
                output = new[] { new { type = "message", id = $"msg_{Requests.Count}", role = "assistant", status = "completed",
                    content = new[] { new { type = "output_text", text = "done", annotations = Array.Empty<object>() } } } }
            });
            if (ReturnToolCall && Requests.Count == 1)
            {
                response = """
                    {"id":"resp_tool","status":"completed","output":[{"type":"function_call","id":"fc_1","call_id":"call_add","name":"add","arguments":"{\"left\":2,\"right\":3}","status":"completed"}]}
                    """;
            }
            if (ReturnToolCall && Requests.Count == 1 && TotalTokens > 0)
                response = response[..^1] + $",\"usage\":{{\"input_tokens\":{TotalTokens - OutputTokens},\"output_tokens\":{OutputTokens},\"total_tokens\":{TotalTokens}}}}}";
            string prefix = string.Empty;
            if (NullTerminalAnnotations)
            {
                using var document = JsonDocument.Parse(response);
                string item = document.RootElement.GetProperty("output")[0].GetRawText();
                prefix = $"data: {{\"type\":\"response.created\",\"sequence_number\":0,\"response\":{{\"id\":\"{id}\",\"status\":\"in_progress\",\"output\":[]}}}}\n\n"
                    + $"data: {{\"type\":\"response.output_item.done\",\"sequence_number\":1,\"output_index\":0,\"item\":{item}}}\n\n";
                response = response.Replace("\"annotations\":[]", "\"annotations\":null", StringComparison.Ordinal);
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prefix + $"data: {{\"type\":\"response.completed\",\"sequence_number\":0,\"response\":{response}}}\n\n",
                    Encoding.UTF8, "text/event-stream")
            };
        }
    }
}
