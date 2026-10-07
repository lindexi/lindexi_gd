#pragma warning disable OPENAI001

using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using AgentLib.Model;
using OpenAI.Responses;

namespace AgentLib.Tests;

/// <summary>
/// 可执行的 API 用法示例：SDK 负责协议，消息直接接收原生结果，管理器不参与执行。
/// </summary>
[TestClass]
public sealed class ResponsesApiUsageTests
{
    [TestMethod]
    public async Task NonStreamingRequest_UsesManualContextAndNativeResult()
    {
        using var handler = new ResponseHandler("""
            {"id":"resp_1","object":"response","status":"completed","output":[
              {"type":"message","id":"msg_1","role":"assistant","status":"completed","content":[
                {"type":"output_text","text":"Hello","annotations":[]}]}]}
            """);
        using var httpClient = new HttpClient(handler);
        var manager = CreateManager(httpClient);
        var context = await manager.CreateManualSendMessageContextAsync();
        context.UserChatMessage.AppendText("Hello");
        await context.AppendMessagesToSessionAsync();
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();

        using (context.StartChatting())
        {
            var response = await client.CreateResponseAsync(
                context.LanguageModel.ModelDefinition.ModelId, "Hello", cancellationToken: CancellationToken.None);
            context.AssistantChatMessage.ResponseInfo.AppendResponse(response.Value);
        }

        Assert.AreEqual("Hello", context.AssistantChatMessage.Content);
    }

    [TestMethod]
    public async Task StreamingRequest_ForwardsSdkUpdatesDirectlyToMessage()
    {
        using var handler = new ResponseHandler("""
            data: {"type":"response.created","sequence_number":0,"response":{"id":"resp_1","status":"in_progress","output":[]}}

            data: {"type":"response.output_text.delta","sequence_number":1,"item_id":"msg_1","output_index":0,"content_index":0,"delta":"Hello"}

            data: {"type":"response.completed","sequence_number":2,"response":{"id":"resp_1","status":"completed","output":[{"type":"message","id":"msg_1","role":"assistant","content":[{"type":"output_text","text":"Hello","annotations":[]}]}]}}


            """, "text/event-stream");
        using var httpClient = new HttpClient(handler);
        var context = await CreateManager(httpClient).CreateManualSendMessageContextAsync();
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var options = new CreateResponseOptions { Model = context.LanguageModel.ModelDefinition.ModelId, StreamingEnabled = true };
        options.InputItems.Add(ResponseItem.CreateUserMessageItem("Hello"));

        var responseInfo = context.AssistantChatMessage.ResponseInfo;
        await foreach (var update in client.CreateResponseStreamingAsync(options, CancellationToken.None))
        {
            responseInfo.AppendResponseUpdate(update);
        }

        Assert.AreEqual("Hello", context.AssistantChatMessage.Content);
    }

    [TestMethod]
    public async Task ToolRoundTrip_ReusesNativeToolOutputInNextRequest()
    {
        using var handler = new ResponseHandler("""
            {"id":"resp_2","status":"completed","output":[{"type":"message","id":"msg_2","role":"assistant","content":[{"type":"output_text","text":"2","annotations":[]}]}]}
            """);
        using var httpClient = new HttpClient(handler);
        var context = await CreateManager(httpClient).CreateManualSendMessageContextAsync();
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var first = new ResponseResult { Id = "resp_1", Status = ResponseStatus.Completed };
        first.OutputItems.Add(ResponseItem.CreateFunctionCallItem("call_1", "add", BinaryData.FromString("{\"value\":1}")));
        var responseInfo = context.AssistantChatMessage.ResponseInfo;
        responseInfo.AppendResponse(first);

        var call = first.OutputItems.OfType<FunctionCallResponseItem>().Single();
        var output = ResponseItem.CreateFunctionCallOutputItem(call.CallId, "2");
        responseInfo.AppendToolResult(output);
        var options = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            PreviousResponseId = first.Id,
        };
        options.InputItems.Add(output);
        var next = await client.CreateResponseAsync(options);
        responseInfo.AppendResponse(next.Value);

        CollectionAssert.AreEqual(new[] { "resp_1", "resp_2" }, responseInfo.Responses.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public void InterleavedUpdates_UpdateTheirOwnContentParts()
    {
        var message = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, false);
        var responseInfo = message.ResponseInfo;
        responseInfo.AppendResponseUpdate(new StreamingResponseOutputTextDeltaUpdate { OutputIndex = 0, ContentIndex = 0, Delta = "A" });
        responseInfo.AppendResponseUpdate(new StreamingResponseOutputTextDeltaUpdate { OutputIndex = 1, ContentIndex = 0, Delta = "B" });
        responseInfo.AppendResponseUpdate(new StreamingResponseOutputTextDeltaUpdate { OutputIndex = 0, ContentIndex = 0, Delta = "C" });

        CollectionAssert.AreEqual(new[] { "AC", "B" }, message.MessageItems.OfType<CopilotChatTextItem>().Select(item => item.Text).ToArray());
    }

    [TestMethod]
    public void ReadingResponseInfo_DoesNotChangeMessageContent()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        message.AppendText("Hello");

        _ = message.ResponseInfo;

        Assert.AreEqual("Hello", message.Content);
    }

    [TestMethod]
    public void ResponseInfo_IsCreatedOnlyOnce()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        var responseInfo = message.ResponseInfo;

        Assert.AreSame(responseInfo, message.ResponseInfo);
    }

    [TestMethod]
    public void StreamingText_UsesExistingMessageIncrementalEvent()
    {
        var message = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, false);
        var received = new List<string>();
        message.TextAppended += (_, text) => received.Add(text);

        message.ResponseInfo.AppendResponseUpdate(new StreamingResponseOutputTextDeltaUpdate
        { OutputIndex = 0, ContentIndex = 0, Delta = "A" });
        message.ResponseInfo.AppendResponseUpdate(new StreamingResponseOutputTextDoneUpdate
        { OutputIndex = 0, ContentIndex = 0, Text = "A" });

        CollectionAssert.AreEqual(new[] { "A" }, received);
    }

    [TestMethod]
    public void StreamingReasoning_UsesExistingMessageIncrementalEvent()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        var received = new List<string>();
        message.ReasoningAppended += (_, text) => received.Add(text);

        message.ResponseInfo.AppendResponseUpdate(new StreamingResponseReasoningSummaryTextDeltaUpdate
        { OutputIndex = 0, SummaryIndex = 0, Delta = "Summary" });

        CollectionAssert.AreEqual(new[] { "Summary" }, received);
    }

    [TestMethod]
    public void RepeatedFinalResponse_DoesNotCountUsageTwice()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        var response = new ResponseResult
        {
            Id = "response-usage", Status = ResponseStatus.Completed,
            Usage = new ResponseTokenUsage { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 }
        };

        message.ResponseInfo.AppendResponse(response);
        message.ResponseInfo.AppendResponse(response);

        Assert.AreEqual(15L, message.TotalUsageDetails?.TotalTokenCount);
    }

    [TestMethod]
    public void MultipleResponses_UpdateExistingCurrentUsage()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        message.ResponseInfo.AppendResponse(new ResponseResult
        {
            Id = "first", Status = ResponseStatus.Completed,
            Usage = new ResponseTokenUsage { TotalTokenCount = 15 }
        });

        message.ResponseInfo.AppendResponse(new ResponseResult
        {
            Id = "second", Status = ResponseStatus.Completed,
            Usage = new ResponseTokenUsage { TotalTokenCount = 7 }
        });

        Assert.AreEqual(7L, message.CurrentUsageDetails?.TotalTokenCount);
    }

    [TestMethod]
    public async Task ToolRoundTrip_ExecutesToolAndContinuesWithUsage()
    {
        using var handler = new ResponseHandler(new[]
        {
            """
            {"id":"resp_tool","object":"response","status":"completed","output":[
              {"type":"function_call","id":"fc_1","call_id":"call_add","name":"add",
               "arguments":"{\"left\":2,\"right\":3}","status":"completed"}],
             "usage":{"input_tokens":10,"output_tokens":5,"total_tokens":15}}
            """,
            """
            {"id":"resp_answer","object":"response","status":"completed","previous_response_id":"resp_tool",
             "output":[{"type":"message","id":"msg_answer","role":"assistant","status":"completed",
               "content":[{"type":"output_text","text":"2 + 3 = 5","annotations":[]}]}],
             "usage":{"input_tokens":20,"output_tokens":8,"total_tokens":28}}
            """
        });
        using var httpClient = new HttpClient(handler);
        var context = await CreateManager(httpClient).CreateManualSendMessageContextAsync();
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var invocationArguments = new List<(int Left, int Right)>();
        var function = AIFunctionFactory.Create((int left, int right) =>
        {
            invocationArguments.Add((left, right));
            return left + right;
        }, "add");
        var tools = new Dictionary<string, AIFunction> { [function.Name] = function };
        var request = new CreateResponseOptions { Model = context.LanguageModel.ModelDefinition.ModelId };
        request.Tools.Add(ResponseTool.CreateFunctionTool(function.Name,
            BinaryData.FromString(function.JsonSchema.GetRawText()), false));
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("Calculate 2 + 3 using add."));
        context.UserChatMessage.AppendText("Calculate 2 + 3 using add.");
        await context.AppendMessagesToSessionAsync();
        var responseInfo = context.AssistantChatMessage.ResponseInfo;

        using (context.StartChatting())
        {
            var first = (await client.CreateResponseAsync(request)).Value;
            responseInfo.AppendResponse(first);
            var call = first.OutputItems.OfType<FunctionCallResponseItem>().Single();
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(call.FunctionArguments.ToString());
            Assert.IsNotNull(arguments);
            var result = await tools[call.FunctionName].InvokeAsync(new AIFunctionArguments(
                arguments.ToDictionary(pair => pair.Key, pair => (object?)pair.Value)));
            var output = ResponseItem.CreateFunctionCallOutputItem(call.CallId, JsonSerializer.Serialize(result));
            responseInfo.AppendToolResult(output);
            var continuation = new CreateResponseOptions
            {
                Model = request.Model,
                PreviousResponseId = first.Id,
            };
            continuation.Tools.Add(request.Tools.Single());
            continuation.InputItems.Add(output);
            var second = (await client.CreateResponseAsync(continuation)).Value;
            responseInfo.AppendResponse(second);
        }

        CollectionAssert.AreEqual(new[] { (2, 3) }, invocationArguments);
        Assert.HasCount(2, handler.Requests);
        using var firstRequest = JsonDocument.Parse(handler.Requests[0]);
        using var secondRequest = JsonDocument.Parse(handler.Requests[1]);
        Assert.AreEqual("model-id", firstRequest.RootElement.GetProperty("model").GetString());
        Assert.AreEqual("add", firstRequest.RootElement.GetProperty("tools")[0].GetProperty("name").GetString());
        Assert.IsFalse(firstRequest.RootElement.TryGetProperty("previous_response_id", out _));
        Assert.AreEqual("Calculate 2 + 3 using add.", firstRequest.RootElement.GetProperty("input")[0]
            .GetProperty("content")[0].GetProperty("text").GetString());
        Assert.AreEqual("resp_tool", secondRequest.RootElement.GetProperty("previous_response_id").GetString());
        Assert.AreEqual(1, secondRequest.RootElement.GetProperty("input").GetArrayLength());
        var submittedOutput = secondRequest.RootElement.GetProperty("input")[0];
        Assert.AreEqual("function_call_output", submittedOutput.GetProperty("type").GetString());
        Assert.AreEqual("call_add", submittedOutput.GetProperty("call_id").GetString());
        Assert.AreEqual("5", submittedOutput.GetProperty("output").GetString());
        var displayedTool = context.AssistantChatMessage.MessageItems.OfType<CopilotChatToolItem>().Single();
        Assert.AreEqual("{\"left\":2,\"right\":3}", displayedTool.InputText);
        Assert.AreEqual("5", displayedTool.OutputText);
        Assert.AreEqual("2 + 3 = 5", context.AssistantChatMessage.Content);
        Assert.AreEqual("resp_answer", responseInfo.CurrentResponse?.Id);
        Assert.AreEqual(30L, context.AssistantChatMessage.TotalUsageDetails?.InputTokenCount);
        Assert.AreEqual(13L, context.AssistantChatMessage.TotalUsageDetails?.OutputTokenCount);
        Assert.AreEqual(43L, context.AssistantChatMessage.TotalUsageDetails?.TotalTokenCount);
        Assert.AreEqual(28L, context.AssistantChatMessage.CurrentUsageDetails?.TotalTokenCount);
    }

    private static CopilotChatManager CreateManager(HttpClient httpClient)
    {
        var manager = new CopilotChatManager(new EmptyCopilotChatLogger());
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = httpClient;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        return manager;
    }

    private sealed class ResponseHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;
        private readonly string _mediaType;

        public ResponseHandler(string response, string mediaType = "application/json")
            : this(new[] { response }, mediaType)
        {
        }

        public ResponseHandler(IEnumerable<string> responses, string mediaType = "application/json")
        {
            _responses = new Queue<string>(responses);
            _mediaType = mediaType;
        }

        public List<string> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(request.Content);
            Requests.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, _mediaType),
            };
        }
    }
}
