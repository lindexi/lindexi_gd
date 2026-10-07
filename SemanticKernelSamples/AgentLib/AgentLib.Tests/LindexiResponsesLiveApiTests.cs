#pragma warning disable OPENAI001

using System.Text.Json;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using OpenAI.Responses;

namespace AgentLib.Tests;

/// <summary>
/// 渐进式真实 Responses 实验，密钥缺失时不访问网络。
/// </summary>
[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
[Ignore("正常不应该跑的测试内容")]
public sealed class LindexiResponsesLiveApiTests
{
    private const string KeyFilePath = @"C:\lindexi\Work\Key\lindexi.md";

    [TestMethod]
    public async Task StoreTrue_SingleRequestAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var options = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            MaxOutputTokenCount = 512,
        };
        options.InputItems.Add(ResponseItem.CreateUserMessageItem("Reply with OK."));

        var result = (await client.CreateResponseAsync(options, cancellation.Token)).Value;
        context.AssistantChatMessage.ResponseInfo.AppendResponse(result);

        Assert.AreEqual(ResponseStatus.Completed, result.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
        Assert.IsTrue(context.AssistantChatMessage.Content.Contains("OK", StringComparison.OrdinalIgnoreCase), result.GetOutputText());
        Assert.IsTrue(recorder.Requests.Single().GetProperty("store").GetBoolean());
        Assert.IsTrue(recorder.Responses.Single().GetProperty("store").GetBoolean(), "Response must report store=true before continuation experiments.");
    }

    [TestMethod]
    public async Task StoredResponse_CanBeRetrievedByIdAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            MaxOutputTokenCount = 512,
        };
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("Reply with OK."));
        var created = (await client.CreateResponseAsync(request, cancellation.Token)).Value;

        var retrieved = (await client.GetResponseAsync(created.Id, cancellationToken: cancellation.Token)).Value;

        Assert.AreEqual(created.Id, retrieved.Id);
        Assert.AreEqual(created.GetOutputText(), retrieved.GetOutputText());
    }

    [TestMethod]
    public async Task PreviousResponseId_RecallsLabelWithoutResendingHistoryAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        string label = Guid.NewGuid().ToString("N");
        var firstRequest = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            Instructions = "Remember the fictional planet's label for the next turn. Reply with OK.",
            MaxOutputTokenCount = 1024,
        };
        context.UserChatMessage.AppendText($"The fictional planet's label is {label}.");
        await context.AppendMessagesToSessionAsync();
        firstRequest.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));
        using var run = context.StartChatting();
        var first = (await client.CreateResponseAsync(firstRequest, cancellation.Token)).Value;
        context.AssistantChatMessage.ResponseInfo.AppendResponse(first);
        var nextContext = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        nextContext.UserChatMessage.AppendText("What was the fictional planet's label from my previous message?");
        await nextContext.AppendMessagesToSessionAsync();
        var continuation = new CreateResponseOptions
        {
            Model = firstRequest.Model,
            StoredOutputEnabled = true,
            PreviousResponseId = first.Id,
            MaxOutputTokenCount = 1024,
        };
        continuation.InputItems.Add(ResponseItem.CreateUserMessageItem(nextContext.UserChatMessage.Content));

        var second = (await client.CreateResponseAsync(continuation, cancellation.Token)).Value;
        nextContext.AssistantChatMessage.ResponseInfo.AppendResponse(second);

        Assert.HasCount(2, recorder.Requests);
        var sent = recorder.Requests[1];
        Assert.IsTrue(recorder.Requests[0].GetProperty("store").GetBoolean());
        Assert.IsTrue(sent.GetProperty("store").GetBoolean());
        Assert.AreEqual(first.Id, sent.GetProperty("previous_response_id").GetString());
        Assert.AreEqual(1, sent.GetProperty("input").GetArrayLength());
        Assert.IsFalse(sent.GetRawText().Contains(label, StringComparison.Ordinal));
        Assert.AreEqual(ResponseStatus.Completed, second.Status);
        Assert.IsTrue(nextContext.AssistantChatMessage.Content.Contains(label, StringComparison.Ordinal),
            $"Expected label: {label}\nActual output: {second.GetOutputText()}");
        Assert.IsNotNull(second.Usage);
        Assert.AreEqual((long)second.Usage.TotalTokenCount, nextContext.AssistantChatMessage.TotalUsageDetails?.TotalTokenCount);
    }

    [TestMethod]
    public async Task ExplicitHistoryControl_RecallsLabelAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        string label = Guid.NewGuid().ToString("N");
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            StoredOutputEnabled = true,
            Instructions = "Remember the fictional planet's label for the next turn. Reply with OK.",
            MaxOutputTokenCount = 1024,
        };
        request.InputItems.Add(ResponseItem.CreateUserMessageItem($"The fictional planet's label is {label}."));
        var first = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
        var continuation = new CreateResponseOptions
        {
            Model = request.Model,
            StoredOutputEnabled = true,
            MaxOutputTokenCount = 1024,
        };
        continuation.InputItems.Add(request.InputItems.Single());
        foreach (var item in first.OutputItems) continuation.InputItems.Add(item);
        continuation.InputItems.Add(ResponseItem.CreateUserMessageItem("What was the fictional planet's label from my previous message?"));

        var second = (await client.CreateResponseAsync(continuation, cancellation.Token)).Value;
        context.AssistantChatMessage.ResponseInfo.AppendResponse(second);

        Assert.AreEqual(ResponseStatus.Completed, second.Status);
        Assert.IsTrue(context.AssistantChatMessage.Content.Contains(label, StringComparison.Ordinal), second.GetOutputText());
        Assert.IsFalse(recorder.Requests[1].TryGetProperty("previous_response_id", out _));
        Assert.IsTrue(recorder.Requests[1].GetRawText().Contains(label, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ExplicitHistory_ReusesCachedInputAcrossTurnsAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        var manager = await CreateManagerAsync(httpClient, cancellation.Token);
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        // 足够长且本次实验唯一的前缀，减少最低缓存长度和历史实验命中的干扰。
        string prefix = $"Experiment {Guid.NewGuid():N}\n" + string.Join("\n", Enumerable.Range(0, 160)
            .Select(i => $"Record {i:D4}: The fictional observatory stores stable measurements of orbital distance, surface temperature, atmospheric pressure and spectral intensity for later comparison."));
        var history = new List<ResponseItem> { ResponseItem.CreateUserMessageItem(prefix) };
        var usage = new List<ResponseTokenUsage>();
        var cacheHits = new List<long>();
        for (int turn = 0; turn < 3; turn++)
        {
            history.Add(ResponseItem.CreateUserMessageItem($"Acknowledge turn {turn + 1} with OK only. Do not summarize the records."));
            var request = new CreateResponseOptions
            {
                Model = context.LanguageModel.ModelDefinition.ModelId,
                StoredOutputEnabled = true,
                Instructions = "Keep the provided records as context. Reply briefly to each new request.",
                MaxOutputTokenCount = 1024,
            };
            foreach (var item in history) request.InputItems.Add(item);
            var response = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
            context.AssistantChatMessage.ResponseInfo.AppendResponse(response);
            Assert.AreEqual(ResponseStatus.Completed, response.Status);
            Assert.IsNotNull(response.Usage);
            Assert.IsNotNull(response.Usage.InputTokenDetails);
            usage.Add(response.Usage);
            long cached = response.Usage.InputTokenDetails.CachedTokenCount;
            cacheHits.Add(cached);
            Console.WriteLine($"CACHE turn={turn + 1} input={response.Usage.InputTokenCount} cached={cached} hitRate={(double)cached / response.Usage.InputTokenCount:P2}");
            Assert.AreEqual(cached, context.AssistantChatMessage.CurrentUsageDetails?.CachedInputTokenCount);
            foreach (var item in response.OutputItems) history.Add(item);
        }

        string reportPath = Path.Combine(AppContext.BaseDirectory, $"cache-usage-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
        {
            MeasuredAt = DateTimeOffset.UtcNow,
            Rounds = usage.Select((item, index) => new
            {
                Turn = index + 1,
                InputTokens = item.InputTokenCount,
                CachedTokens = cacheHits[index],
                HitRatePercent = 100.0 * cacheHits[index] / item.InputTokenCount,
            }),
            TotalInputTokens = usage.Sum(item => (long)item.InputTokenCount),
            TotalCachedTokens = cacheHits.Sum(),
            TotalHitRatePercent = 100.0 * cacheHits.Sum() / usage.Sum(item => (long)item.InputTokenCount),
        }, new JsonSerializerOptions { WriteIndented = true }), cancellation.Token);
        Console.WriteLine($"Cache usage report: {reportPath}");

        Assert.HasCount(3, recorder.Requests);
        Assert.IsTrue(cacheHits.Skip(1).Any(count => count > cacheHits[0]),
            $"Expected increased cached input after reusing history; actual: {string.Join(", ", cacheHits)}");
        Assert.AreEqual(cacheHits.Sum(), context.AssistantChatMessage.TotalUsageDetails?.CachedInputTokenCount);
        Assert.AreEqual(usage.Sum(item => (long)item.InputTokenCount), context.AssistantChatMessage.TotalUsageDetails?.InputTokenCount);
        Assert.IsTrue(recorder.Requests.All(request => request.GetProperty("store").GetBoolean()));
        Assert.IsTrue(recorder.Requests.All(request => !request.TryGetProperty("previous_response_id", out _)));
        CollectionAssert.AreEqual(new[] { prefix, prefix, prefix }, recorder.Requests.Select(request =>
            request.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString()).ToArray());
    }

    [TestMethod]
    public async Task WebSearch_FindsLatestOpenAINewsWithNativeToolRecordsAsync()
    {
        if (!File.Exists(KeyFilePath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        string reportPath = Path.Combine(AppContext.BaseDirectory, $"web-search-{Guid.NewGuid():N}.json");
        using var recorder = new RequestRecorder();
        using var httpClient = new HttpClient(recorder);
        ResponseResult? result = null;
        try
        {
            var manager = await CreateManagerAsync(httpClient, cancellation.Token);
            var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
            var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
            context.UserChatMessage.AppendText("请实际使用内置 WebSearchTool 搜索 OpenAI 的最新新闻，包括最新发布的模型。优先查阅 OpenAI 官方公告，列出新闻标题、发布日期、模型名称、简要说明和来源链接。区分正式发布、预览和传闻，不要仅凭训练知识回答；明确说明检索时点。");
            await context.AppendMessagesToSessionAsync();
            var request = new CreateResponseOptions
            {
                Model = context.LanguageModel.ModelDefinition.ModelId,
                StoredOutputEnabled = true,
                Instructions = "You are a research assistant. Use the provided web_search tool to research current news, prefer primary sources, and cite sources. Answer in Chinese.",
                MaxOutputTokenCount = 4096,
            };
            request.Tools.Add(ResponseTool.CreateWebSearchTool());
            request.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));
            using (context.StartChatting())
            {
                result = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
                context.AssistantChatMessage.ResponseInfo.AppendResponse(result);
            }

            var calls = result.OutputItems.OfType<WebSearchCallResponseItem>().ToArray();
            Assert.IsNotEmpty(calls, "Expected native web_search_call output, not merely a textual claim of searching.");
            Assert.AreEqual(ResponseStatus.Completed, result.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(context.AssistantChatMessage.Content));
            Assert.IsTrue(recorder.Requests.Single().GetProperty("tools").EnumerateArray()
                .Any(tool => tool.GetProperty("type").GetString() == "web_search"));
        }
        finally
        {
            // 请求取消或断言失败时也保留诊断证据；不序列化管理器配置或 HTTP 头。
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
            {
                RecordedAt = DateTimeOffset.UtcNow,
                Endpoint = "https://model.server.lindexi.com/v1/responses",
                Model = "gpt-5.6-luna",
                Requests = recorder.Requests,
                HttpResponses = recorder.ResponseBodies.Select(response => new { response.StatusCode, response.Body }).ToArray(),
                RawResponses = recorder.Responses,
                ResponseId = result?.Id,
                Status = result?.Status?.ToString(),
                OutputText = result?.GetOutputText(),
                ToolCalls = result?.OutputItems.OfType<WebSearchCallResponseItem>().Select(call => new
                {
                    call.Id,
                    Status = call.Status?.ToString(),
                    Action = call.Action is null ? null : System.ClientModel.Primitives.ModelReaderWriter.Write(call.Action).ToString(),
                }).ToArray(),
                OutputItems = result?.OutputItems.Select(item => new { item.Id, Type = item.GetType().Name }).ToArray(),
                Usage = result?.Usage is null ? null : System.ClientModel.Primitives.ModelReaderWriter.Write(result.Usage).ToString(),
            }, new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Web search report: {reportPath}");
        }
    }

    private static async Task<CopilotChatManager> CreateManagerAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        string key = (await File.ReadAllTextAsync(KeyFilePath, cancellationToken)).Trim();
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        string json = JsonSerializer.Serialize(new
        {
            PrimaryModel = "gpt-5.6-luna",
            OpenAIConfigurationList = new[]
            {
                new
                {
                    EndPoint = "https://model.server.lindexi.com/v1",
                    Key = key,
                    ModelDefinitions = new[]
                    {
                        new { Provider = "lindexi", ModelName = "gpt-5.6-luna", ModelId = "gpt-5.6-luna" }
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
        public List<JsonElement> Responses { get; } = [];
        public List<(int StatusCode, string Body)> ResponseBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"REQUEST {request.Method} {request.RequestUri}");
            if (request.Content is { } content)
            {
                using var json = JsonDocument.Parse(await content.ReadAsStringAsync(cancellationToken));
                Requests.Add(json.RootElement.Clone());
                Console.WriteLine(json.RootElement.GetRawText());
            }
            var response = await base.SendAsync(request, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            ResponseBodies.Add(((int)response.StatusCode, body));
            Console.WriteLine($"RESPONSE {(int)response.StatusCode}\n{body}");
            if (response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(body);
                Responses.Add(json.RootElement.Clone());
            }
            return response;
        }
    }
}
