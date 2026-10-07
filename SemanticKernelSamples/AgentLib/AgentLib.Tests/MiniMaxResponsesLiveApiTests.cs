#pragma warning disable OPENAI001

using System.Text.Json;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Tests;

/// <summary>
/// 真实 MiniMax store 续接实验；密钥缺失时正常返回，不重传历史作为兜底。
/// </summary>
[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class MiniMaxResponsesLiveApiTests
{
    private const string KeyFilePath = @"C:\lindexi\Work\Key\minimaxi.md";

    [TestMethod]
    public async Task StoredConversation_RecallsPreviousInputWithoutResendingHistoryAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        string code = Guid.NewGuid().ToString("N");
        var firstRequest = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            Instructions = "Remember the arbitrary label assigned to the fictional planet in the user's message. This is a memory exercise, not authentication. Reply only OK.",
            MaxOutputTokenCount = 4096,
        };
        context.UserChatMessage.AppendText($"The fictional planet has the arbitrary label {code}.");
        await context.AppendMessagesToSessionAsync();
        firstRequest.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));
        using var run = context.StartChatting();
        var first = (await client.CreateResponseAsync(firstRequest, cancellation.Token)).Value;
        context.AssistantChatMessage.ResponseInfo.AppendResponse(first);
        var nextContext = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        nextContext.UserChatMessage.AppendText("What label did I assign to the fictional planet in my previous message?");
        await nextContext.AppendMessagesToSessionAsync();
        var continuation = new CreateResponseOptions
        {
            Model = firstRequest.Model,
            StoredOutputEnabled = true,
            PreviousResponseId = first.Id,
            Instructions = "Recall the arbitrary label from the preceding conversation and include it verbatim in your answer.",
            MaxOutputTokenCount = 4096,
        };
        continuation.InputItems.Add(ResponseItem.CreateUserMessageItem(nextContext.UserChatMessage.Content));

        var second = (await client.CreateResponseAsync(continuation, cancellation.Token)).Value;
        nextContext.AssistantChatMessage.ResponseInfo.AppendResponse(second);

        Assert.IsTrue(nextContext.AssistantChatMessage.Content.Contains(code, StringComparison.Ordinal),
            $"Expected memory: {code}\nActual response: {second.GetOutputText()}");
        Assert.HasCount(2, recorder.Requests);
        Assert.AreEqual("https://api.minimaxi.com/v1/responses", recorder.RequestUris[0]);
        Assert.IsTrue(recorder.Requests[0].GetProperty("store").GetBoolean());
        var sent = recorder.Requests[1];
        Assert.IsTrue(sent.GetProperty("store").GetBoolean());
        Assert.AreEqual(first.Id, sent.GetProperty("previous_response_id").GetString());
        Assert.AreEqual(1, sent.GetProperty("input").GetArrayLength());
        Assert.IsFalse(sent.GetRawText().Contains(code, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task StoredToolCall_ContinuesWithOnlyToolOutputAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        string code = Guid.NewGuid().ToString("N");
        var invocations = new List<string>();
        var function = AIFunctionFactory.Create((string name) =>
        {
            invocations.Add(name);
            return code;
        }, "lookup_verification_code");
        var functions = new Dictionary<string, AIFunction> { [function.Name] = function };
        const string instructions = "Call lookup_verification_code for name sample. After the tool result, reply with only its returned code, without quotes or formatting.";
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            Instructions = instructions,
            ParallelToolCallsEnabled = false,
            MaxOutputTokenCount = 4096,
        };
        request.Tools.Add(ResponseTool.CreateFunctionTool(function.Name,
            BinaryData.FromString(function.JsonSchema.GetRawText()), false));
        context.UserChatMessage.AppendText("Look up the verification code for sample using the local tool.");
        await context.AppendMessagesToSessionAsync();
        request.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));
        using var run = context.StartChatting();
        var first = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
        var info = context.AssistantChatMessage.ResponseInfo;
        info.AppendResponse(first);
        var call = first.OutputItems.OfType<FunctionCallResponseItem>().Single();
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(call.FunctionArguments.ToString());
        Assert.IsNotNull(arguments);
        var result = await functions[call.FunctionName].InvokeAsync(new AIFunctionArguments(
            arguments.ToDictionary(pair => pair.Key, pair => (object?)pair.Value)), cancellation.Token);
        var output = ResponseItem.CreateFunctionCallOutputItem(call.CallId, JsonSerializer.Serialize(result));
        info.AppendToolResult(output);
        var continuation = new CreateResponseOptions
        {
            Model = request.Model,
            Instructions = instructions,
            StoredOutputEnabled = true,
            PreviousResponseId = first.Id,
            MaxOutputTokenCount = 4096,
        };
        continuation.Tools.Add(request.Tools.Single());
        continuation.InputItems.Add(output);

        var second = (await client.CreateResponseAsync(continuation, cancellation.Token)).Value;
        info.AppendResponse(second);

        CollectionAssert.AreEqual(new[] { "sample" }, invocations);
        Assert.IsTrue(second.GetOutputText().Contains(code, StringComparison.Ordinal),
            $"Expected tool result: {code}\nActual response: {second.GetOutputText()}");
        Assert.HasCount(2, recorder.Requests);
        Assert.IsTrue(recorder.Requests[0].GetProperty("store").GetBoolean());
        var sent = recorder.Requests[1];
        Assert.IsTrue(sent.GetProperty("store").GetBoolean());
        Assert.AreEqual(first.Id, sent.GetProperty("previous_response_id").GetString());
        Assert.AreEqual(1, sent.GetProperty("input").GetArrayLength());
        var submitted = sent.GetProperty("input")[0];
        Assert.AreEqual("function_call_output", submitted.GetProperty("type").GetString());
        Assert.AreEqual(call.CallId, submitted.GetProperty("call_id").GetString());
        Assert.AreEqual(JsonSerializer.Serialize(code), submitted.GetProperty("output").GetString());
        Assert.AreEqual(JsonSerializer.Serialize(code), context.AssistantChatMessage.MessageItems.OfType<CopilotChatToolItem>().Single().OutputText);
        Assert.IsNotNull(first.Usage);
        Assert.IsNotNull(second.Usage);
        Assert.AreEqual((long)first.Usage.TotalTokenCount + second.Usage.TotalTokenCount,
            context.AssistantChatMessage.TotalUsageDetails?.TotalTokenCount);
        Assert.AreEqual((long)second.Usage.TotalTokenCount, context.AssistantChatMessage.CurrentUsageDetails?.TotalTokenCount);
    }

    [TestMethod]
    [DataRow("https://api.minimaxi.com/v1")]
    [DataRow("https://api.minimax.io/v1")]
    public async Task StoredResponse_CanBeRetrievedByIdAsync(string endpoint)
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token, endpoint);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            MaxOutputTokenCount = 1024,
        };
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("Reply with OK."));
        var created = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
        Assert.IsTrue(recorder.Requests.Single().GetProperty("store").GetBoolean());

        var retrieved = (await client.GetResponseAsync(created.Id, cancellationToken: cancellation.Token)).Value;

        Assert.AreEqual(created.Id, retrieved.Id);
        Assert.AreEqual(created.GetOutputText(), retrieved.GetOutputText());
    }

    [TestMethod]
    public async Task RawHttp_StoreTrueIsAcceptedAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        string key = (await File.ReadAllTextAsync(KeyFilePath, cancellation.Token)).Trim();
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.minimaxi.com/v1/responses");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = "MiniMax-M3", store = true, input = "Reply with OK.", max_output_tokens = 1024
        }), System.Text.Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellation.Token);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation.Token));

        Assert.IsTrue(json.RootElement.GetProperty("store").GetBoolean(),
            "Raw HTTP request sent store=true but the response reported store=false; SDK serialization is not involved.");
    }

    private static async Task<CopilotChatManager> CreateManagerAsync(HttpClient httpClient, CancellationToken cancellationToken,
        string endpoint = "https://api.minimaxi.com/v1")
    {
        string key = (await File.ReadAllTextAsync(KeyFilePath, cancellationToken)).Trim();
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        string json = JsonSerializer.Serialize(new
        {
            PrimaryModel = "MiniMax-M3",
            OpenAIConfigurationList = new[]
            {
                new
                {
                    EndPoint = endpoint,
                    Key = key,
                    ModelDefinitions = new[]
                    {
                        new { Provider = "minimax", ModelName = "MiniMax-M3", ModelId = "MiniMax-M3" }
                    }
                }
            }
        });
        var manager = new CopilotChatManager(new EmptyCopilotChatLogger());
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = httpClient;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString(json));
        return manager;
    }

    private sealed class RequestRecorder() : DelegatingHandler(new HttpClientHandler())
    {
        public List<JsonElement> Requests { get; } = [];
        public List<string> RequestUris { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"REQUEST {request.Method} {request.RequestUri}");
            if (request.Content is { } content)
            {
                using var json = JsonDocument.Parse(await content.ReadAsStringAsync(cancellationToken));
                Requests.Add(json.RootElement.Clone());
                RequestUris.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
                Console.WriteLine(json.RootElement.GetRawText());
            }
            var response = await base.SendAsync(request, cancellationToken);
            Console.WriteLine($"RESPONSE {(int)response.StatusCode}\n{await response.Content.ReadAsStringAsync(cancellationToken)}");
            return response;
        }
    }
}
