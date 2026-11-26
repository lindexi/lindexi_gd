using System.Net;
using System.Text;
using System.Text.Json;
using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideResponsesTests
{
    [TestMethod]
    public async Task StreamingRestartSendsOnlyNativeHistoryBeforeTarget()
    {
        using var handler = new ResponsesHandler(
            TextResponse("first", "<Page/>"),
            TextResponse("discarded", "<Page><Rect Id=\"old\" Width=\"20\" Height=\"20\"/></Page>"),
            TextResponse("replacement", "<Page><Rect Id=\"new\" Width=\"20\" Height=\"20\"/></Page>"));
        using var http = new HttpClient(handler);
        var manager = CreateManager(http);
        var slide = CreateSlideManager(manager);

        await slide.SendMessageAsync("First slide", true, false, useStreaming: true);
        await slide.SendMessageAsync("Revise", false, false, useStreaming: true);
        var target = manager.SelectedSession.ChatMessages.Single(message => message.Content == "Revise");
        await slide.RestartFromMessageAsync(target);

        var restartInput = handler.Requests[2].GetProperty("input");
        CollectionAssert.AreEqual(new[] { "first" }, restartInput.EnumerateArray()
            .Where(item => item.GetProperty("role").GetString() == "assistant")
            .Select(item => item.GetProperty("id").GetString()).ToArray());
    }

    [TestMethod]
    public async Task NonStreamingGenerationReturnsToolOutputToResponses()
    {
        const string toolItem = """
            {"type":"function_call","id":"function_1","call_id":"call_render","name":"render_slide","arguments":"{\"slideXml\":\"<Page/>\"}","status":"completed"}
            """;
        using var handler = new ResponsesHandler(Completed("tool", toolItem), TextResponse("done", "Done"));
        using var http = new HttpClient(handler);
        var slide = CreateSlideManager(CreateManager(http));

        await slide.SendMessageAsync("Render a slide", true, false, skipAutoEvaluation: true);

        var output = handler.Requests[1].GetProperty("input").EnumerateArray()
            .Single(item => item.GetProperty("type").GetString() == "function_call_output");
        Assert.AreEqual("call_render", output.GetProperty("call_id").GetString());
    }

    private static CopilotChatManager CreateManager(HttpClient http)
    {
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"slides","OpenAIConfigurationList":[{"EndPoint":"https://example.test/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"slides","ModelId":"test-model","Provider":"test"}]}]}
            """));
        return manager;
    }

    private static SlideChatManager CreateSlideManager(CopilotChatManager manager)
    {
        var dispatcher = new FakeMainThreadDispatcher();
        var render = new SlideMlRenderPipeline(new SlideMlLayoutEngine(), new FakeRenderEngine(), dispatcher);
        return new SlideChatManager(manager, new SlideMlRenderTool(render, dispatcher),
            new PipelineConfiguration { UseResponsesApi = true });
    }

    private static string TextResponse(string id, string text)
    {
        var item = JsonSerializer.Serialize(new
        {
            type = "message", id, role = "assistant", status = "completed",
            content = new[] { new { type = "output_text", text, annotations = Array.Empty<object>() } },
        });
        return Event(new { type = "response.created", sequence_number = 0, response = new { id, status = "in_progress", output = Array.Empty<object>() } })
            + Event(new { type = "response.output_text.delta", sequence_number = 1, item_id = id, output_index = 0, content_index = 0, delta = text })
            + Completed(id, item);
    }

    private static string Completed(string id, string item) =>
        $$$"""
        data: {"type":"response.completed","sequence_number":2,"response":{"id":"{{{id}}}","status":"completed","output":[{{{item}}}]}}
        """ + "\n\n";

    private static string Event<T>(T update) => $"data: {JsonSerializer.Serialize(update)}\n\n";

    private sealed class ResponsesHandler(params string[] responses) : HttpMessageHandler
    {
        internal List<JsonElement> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("/v1/responses", request.RequestUri?.AbsolutePath);
            using var input = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Requests.Add(input.RootElement.Clone());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responses[Requests.Count - 1], Encoding.UTF8, "text/event-stream"),
            };
        }
    }
}
