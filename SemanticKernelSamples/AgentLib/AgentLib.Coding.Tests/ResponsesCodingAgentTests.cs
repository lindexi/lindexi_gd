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
    public async Task RunAsyncShouldSubmitActualToolResult()
    {
        string path = Path.Combine(Path.GetTempPath(), $"responses-workspace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        Console.WriteLine(path);
        using var handler = new StreamingHandler { ReturnToolCall = true };
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
            [new TextContent("calculate")], manager.SelectedSession.ResponsesSession, path);
        await run.CompletionTask;

        using var request = JsonDocument.Parse(handler.Requests[1]);
        Assert.AreEqual("5", request.RootElement.GetProperty("input").EnumerateArray()
            .Single(item => item.GetProperty("type").GetString() == "function_call_output")
            .GetProperty("output").GetString());
    }

    private sealed class TestToolSource : ICodingWorkspaceToolSource
    {
        public IReadOnlyList<AITool> CreateTools(string workspacePath) =>
            [AIFunctionFactory.Create((int left, int right) => left + right, "add")];
    }

    private sealed class StreamingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = new();
        public bool ReturnToolCall { get; init; }
        public bool NullTerminalAnnotations { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
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
