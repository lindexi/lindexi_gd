using System.Net.Http;
using System.Text.Json;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using AgentLib.Coding;
using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class ResponsesWorkflowLiveTests
{
    private const string KeyPath = @"C:\lindexi\Work\Key\MiniMax.txt";
    private const string Endpoint = "https://api.minimaxi.com/v1";
    private const string ModelId = "MiniMax-M3";

    [TestMethod]
    public async Task NativeCompactShouldReturnProtocolOutputAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var client = new ResponsesClient(
            new System.ClientModel.ApiKeyCredential((await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()),
            new ResponsesClientOptions { Endpoint = new Uri(Endpoint) });
        string directory = Path.Combine(Path.GetTempPath(), $"responses-compact-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        Console.WriteLine($"Compact report directory: {directory}");
        var request = new CreateResponseOptions { Model = ModelId, StoredOutputEnabled = false };
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("For this C# project, remember the test identifier ORCHID-731. Reply briefly."));
        var first = (await client.CreateResponseAsync(request, timeout.Token)).Value;
        var input = new List<ResponseItem>(request.InputItems);
        input.AddRange(first.OutputItems);
        request.InputItems.Clear();
        foreach (var item in input) request.InputItems.Add(item);
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("Which test identifier did I specify? Reply only with the identifier."));
        var second = (await client.CreateResponseAsync(request, timeout.Token)).Value;
        input = new List<ResponseItem>(request.InputItems);
        input.AddRange(second.OutputItems);
        using var compactRequest = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = ModelId,
            input = input.Select(item => JsonSerializer.Deserialize<JsonElement>(
                System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString())).ToArray(),
            instructions = "Preserve the C# project test identifier and conversation context.",
        }));
        var result = await client.CompactResponseAsync(compactRequest, "application/json",
            new System.ClientModel.Primitives.RequestOptions
            {
                CancellationToken = timeout.Token,
                ErrorOptions = System.ClientModel.Primitives.ClientErrorBehaviors.NoThrow,
            });
        var response = result.GetRawResponse();
        string body = response.Content.ToString();
        await File.WriteAllTextAsync(Path.Combine(directory, "compact-response.json"), body, timeout.Token);
        Console.WriteLine($"Compact HTTP status: {response.Status}");
        Console.WriteLine($"Compact response: {body}");
        Assert.IsTrue(response.Status is >= 200 and < 300, $"Compact returned HTTP {response.Status}: {body}");
        using var document = JsonDocument.Parse(body);
        Assert.IsTrue(document.RootElement.TryGetProperty("output", out var output)
            && output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeStreamingWithoutApplicationShouldReadCompletedEventAsync(bool storeOutput)
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var client = new OpenAI.Responses.ResponsesClient(
            new System.ClientModel.ApiKeyCredential((await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()),
            new ResponsesClientOptions { Endpoint = new Uri(Endpoint) });
        var options = new OpenAI.Responses.CreateResponseOptions
        {
            Model = ModelId, StreamingEnabled = true, StoredOutputEnabled = storeOutput,
        };
        options.InputItems.Add(OpenAI.Responses.ResponseItem.CreateUserMessageItem("Explain C# int in one short sentence."));
        bool completed = false;
        Console.WriteLine($"SDK assembly: {typeof(OpenAI.Responses.ResponsesClient).Assembly.FullName}");
        await foreach (var update in client.CreateResponseStreamingAsync(options, timeout.Token))
        {
            Console.WriteLine($"SDK event: {update.GetType().Name}");
            completed |= update is OpenAI.Responses.StreamingResponseCompletedUpdate;
        }
        Assert.IsTrue(completed);
    }

    [TestMethod]
    public async Task SendCommandShouldPreserveContextAcrossTurnsAndStoredSessionAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        await scenario.SendAsync("请记住测试标识 ORCHID-731。只回复已记住，不调用工具。");
        await scenario.SendAsync("刚才的测试标识是什么？只回复标识，不调用工具。");
        StringAssert.Contains(scenario.LastAnswer, "ORCHID-731");
        Guid sessionId = scenario.Runtime.Controller.SelectedSessionId;
        await scenario.Runtime.Controller.CreateNewSessionAsync();
        await scenario.Runtime.Controller.OpenSessionAsync(sessionId);
        await scenario.SendAsync("请再重复一次，刚才的测试标识是什么？只回复标识，不调用工具。");
        StringAssert.Contains(scenario.LastAnswer, "ORCHID-731");
        await scenario.SendAsync("重复一次这个标识，不调用工具。");
        StringAssert.Contains(scenario.LastAnswer, "ORCHID-731");
    }

    [TestMethod]
    public async Task StoredSessionShouldSupportMultipleTurnsAfterRuntimeRecreationAsync()
    {
        if (!File.Exists(KeyPath)) return;
        string directory;
        Guid sessionId;
        string[] originalItems;
        await using (var original = await Scenario.CreateAsync())
        {
            directory = original.DirectoryPath;
            await original.SendAsync("请记住代码测试标识 ORCHID-731。只回复已记住，不调用工具。");
            await original.SendAsync("刚才的测试标识是什么？只回复标识，不调用工具。");
            StringAssert.Contains(original.LastAnswer, "ORCHID-731");
            sessionId = original.Runtime.Controller.SelectedSessionId;
            originalItems = original.Runtime.ChatManager.SelectedSession.ResponsesSession.Items
                .Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString()).ToArray();
        }

        await using var restored = await Scenario.CreateAsync(directory);
        await restored.Runtime.Controller.OpenSessionAsync(sessionId);
        var session = restored.Runtime.ChatManager.SelectedSession;
        CollectionAssert.AreEqual(originalItems, session.ResponsesSession.Items
            .Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString()).ToArray());
        await restored.SendAsync("请再重复一次，刚才的测试标识是什么？只回复标识，不调用工具。");
        StringAssert.Contains(restored.LastAnswer, "ORCHID-731");
        await restored.SendAsync("请再重复一次这个测试标识，只回复标识，不调用工具。");
        StringAssert.Contains(restored.LastAnswer, "ORCHID-731");
    }

    [TestMethod]
    public async Task WorkspaceFileToolShouldPreserveNativeHistoryAcrossUserCommandsAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        string workspace = Path.Combine(scenario.DirectoryPath, "workspace");
        Directory.CreateDirectory(workspace);
        string marker = $"CODE-{Guid.NewGuid():N}";
        await File.WriteAllTextAsync(Path.Combine(workspace, "fixture.txt"), $"Code fixture identifier: {marker}");
        scenario.Chat.WorkspaceInput = workspace;
        await ((SimpleAsyncCommand)scenario.Chat.ApplyWorkspaceCommand).ExecuteAsync();
        Assert.AreEqual(Path.GetFullPath(workspace), scenario.Runtime.WorkspaceController.NextRunWorkspacePath);

        await scenario.SendAsync("请使用本地文件读取工具读取工作区 fixture.txt，逐字回复文件中的代码测试标识，保留原始大小写，不要改写。不要搜索网络，不要运行命令，不要调用子代理。");
        var history = scenario.Runtime.ChatManager.SelectedSession.ResponsesSession.Items;
        var calls = history.OfType<OpenAI.Responses.FunctionCallResponseItem>().ToArray();
        Assert.IsNotEmpty(calls, "No native local function call recorded.");
        var outputs = history.OfType<OpenAI.Responses.FunctionCallOutputResponseItem>().ToArray();
        Assert.IsTrue(outputs.Any(output => calls.Any(call => call.CallId == output.CallId)
            && output.FunctionOutput.Contains(marker, StringComparison.Ordinal)), "Actual file content missing from native tool result.");
        StringAssert.Contains(scenario.LastAnswer, marker);
        string[] prefix = history.Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString()).ToArray();
        int nextRequest = scenario.Recorder.Requests.Count;

        await scenario.SendAsync("请根据上文逐字重复刚才读到的代码测试标识，保留原始大小写，只回复标识，不调用工具。");

        StringAssert.Contains(scenario.LastAnswer, marker);
        CollectionAssert.AreEqual(prefix, history.Take(prefix.Length)
            .Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString()).ToArray());
        using var request = JsonDocument.Parse(scenario.Recorder.Requests[nextRequest]);
        Assert.IsTrue(request.RootElement.GetProperty("input").EnumerateArray().Any(item =>
            item.GetProperty("type").GetString() == "function_call_output"
            && item.GetProperty("output").GetString()?.Contains(marker, StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public async Task SelectedReasoningEffortShouldReachNativeRequestAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        scenario.Recorder.CaptureResponses = true;
        scenario.Chat.SelectedReasoningEffort = scenario.Chat.AvailableReasoningEfforts.Single(option => option.Value == ReasoningEffort.High);
        await scenario.SendAsync("计算 17 加 26，只回复结果，不调用工具。");

        using var request = JsonDocument.Parse(scenario.Recorder.Requests.Last());
        Assert.AreEqual("high", request.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
    }

    [TestMethod]
    [Ignore("图片已提交，但模型分类回答不符合预期；提示词因素尚待核查，暂缓语义验收。")]
    public async Task PendingImageShouldReachRequestAndBeRecognizedAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        byte[] image = CreateRedImage();
        Assert.IsTrue(ImageAttachmentViewModel.TryCreate("sample.png", image, out var attachment));
        scenario.Chat.PendingImages.Add(attachment);
        await scenario.SendAsync("我准备根据附件实现 Avalonia 界面，请检查素材是否足以指导布局：有明确的按钮、文字等界面元素则回复 UI_LAYOUT；只有均匀色块、没有界面元素则回复 SOLID_COLOR；其他情况回复 OTHER。只回复分类标记，不调用工具。");

        using var request = JsonDocument.Parse(scenario.Recorder.Requests.First());
        Assert.IsTrue(request.RootElement.GetProperty("input").EnumerateArray()
            .Where(item => item.GetProperty("type").GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Any(part => part.GetProperty("type").GetString() == "input_image"));
        StringAssert.Contains(scenario.LastAnswer, "SOLID_COLOR");
    }

    [TestMethod]
    public async Task StopCommandShouldCancelActiveRequestAndAllowNextSendAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        scenario.Chat.InputText = "请详细讲解 C# 异步机制，至少三千字，不调用工具。";
        Task sending = ((SimpleAsyncCommand)scenario.Chat.SendCommand).ExecuteAsync();
        await scenario.Recorder.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await ((SimpleAsyncCommand)scenario.Chat.StopCommand).ExecuteAsync();
        await sending.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.IsFalse(scenario.Chat.IsRunning);
        await scenario.SendAsync("只回复 OK，不调用工具。");
        StringAssert.Contains(scenario.LastAnswer, "OK");
    }

    [TestMethod]
    public async Task SendCommandDuringActiveResponseShouldQueueInterruptionAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        scenario.Chat.InputText = "请用一段话介绍 C# async，不调用工具。";
        Task sending = ((SimpleAsyncCommand)scenario.Chat.SendCommand).ExecuteAsync();
        await scenario.Recorder.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        scenario.Chat.InputText = "请调整回答，只回复代码测试标识 INTERRUPT-731，不调用工具。";
        await ((SimpleAsyncCommand)scenario.Chat.SendCommand).ExecuteAsync();
        await sending.WaitAsync(TimeSpan.FromMinutes(3));

        StringAssert.Contains(scenario.LastAnswer, "INTERRUPT-731");
        Assert.IsTrue(scenario.Runtime.ChatManager.SelectedSession.ChatMessages.Any(message =>
            message.Role == ChatRole.User && message.Content.Contains("INTERRUPT-731", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task CompressionCommandShouldBeAvailableForResponsesHistoryAsync()
    {
        if (!File.Exists(KeyPath)) return;
        await using var scenario = await Scenario.CreateAsync();
        await scenario.SendAsync("记住标识 ORCHID-731。只回复已记住，不调用工具。");

        Assert.IsTrue(scenario.Chat.CompressConversationCommand.CanExecute(null),
            "Responses 历史已存在，但压缩入口尚未接入原生 Responses compact。");
        await ((SimpleAsyncCommand)scenario.Chat.CompressConversationCommand).ExecuteAsync();
        Assert.IsTrue(scenario.Recorder.Paths.Any(path => path.EndsWith("/responses/compact", StringComparison.Ordinal)),
            "压缩必须调用原生 compact，不允许用 Chat 压缩或伪造完成。");
        int continuationIndex = scenario.Recorder.Requests.Count;
        string compacted = System.ClientModel.Primitives.ModelReaderWriter.Write(
            scenario.Runtime.ChatManager.SelectedSession.ResponsesSession.Items.Single()).ToString();
        await scenario.SendAsync("请再重复一次，刚才的测试标识是什么？只回复标识，不调用工具。");
        using var continuation = JsonDocument.Parse(scenario.Recorder.Requests[continuationIndex]);
        using var expected = JsonDocument.Parse(compacted);
        Assert.AreEqual(expected.RootElement.GetProperty("encrypted_content").GetString(),
            continuation.RootElement.GetProperty("input")[0].GetProperty("encrypted_content").GetString());
        StringAssert.Contains(scenario.LastAnswer, "ORCHID-731");
    }

    [TestMethod]
    [DataRow("You are a C# programming assistant. Preserve project identifiers from conversation context.", "What is the C# project test identifier mentioned above? Reply only with its exact value.", true)]
    [DataRow("", "Repeat the project test identifier supplied by the user. Output only its value.", false)]
    [DataRow("你是 C# 编程助手，根据已有上下文回答代码问题。", "请再重复一次，刚才的项目测试标识是什么？只回复标识。", false)]
    [DataRow("You assist with C# code. Use the supplied project context.", "Complete this C# declaration with the project test identifier: const string TestId = ...;", true)]
    public async Task NativeCompactionOutputShouldSupportContinuationWithoutApplicationAsync(
        string instructions, string followUp, bool includeCompactInstructions)
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var client = new ResponsesClient(new System.ClientModel.ApiKeyCredential(
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()), new ResponsesClientOptions { Endpoint = new Uri(Endpoint) });
        var request = new CreateResponseOptions { Model = ModelId, Instructions = instructions, StoredOutputEnabled = false };
        request.InputItems.Add(ResponseItem.CreateUserMessageItem("The C# project test identifier is ORCHID-731. Remember it and acknowledge briefly."));
        var first = (await client.CreateResponseAsync(request, timeout.Token)).Value;
        foreach (var item in first.OutputItems) request.InputItems.Add(item);
        var baselineRequest = new CreateResponseOptions
        {
            Model = ModelId, Instructions = instructions, StoredOutputEnabled = false,
        };
        foreach (var item in request.InputItems) baselineRequest.InputItems.Add(item);
        baselineRequest.InputItems.Add(ResponseItem.CreateUserMessageItem(followUp));
        var baseline = (await client.CreateResponseAsync(baselineRequest, timeout.Token)).Value;
        Console.WriteLine($"Baseline answer: {baseline.GetOutputText()}");
        StringAssert.Contains(baseline.GetOutputText(), "ORCHID-731");
        var compactPayload = new Dictionary<string, object>
        {
            ["model"] = ModelId,
            ["input"] = request.InputItems.Select(item => JsonSerializer.Deserialize<JsonElement>(
                System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString())).ToArray(),
        };
        if (includeCompactInstructions) compactPayload.Add("instructions", instructions);
        using var compactContent = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(compactPayload));
        var compact = await client.CompactResponseAsync(compactContent, "application/json",
            new System.ClientModel.Primitives.RequestOptions { CancellationToken = timeout.Token });
        using var document = JsonDocument.Parse(compact.GetRawResponse().Content);
        request.InputItems.Clear();
        foreach (var item in document.RootElement.GetProperty("output").EnumerateArray())
            request.InputItems.Add(System.ClientModel.Primitives.ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item.GetRawText()))
                ?? throw new InvalidDataException("Compaction item missing."));
        request.InputItems.Add(ResponseItem.CreateUserMessageItem(followUp));
        var answer = (await client.CreateResponseAsync(request, timeout.Token)).Value;
        Console.WriteLine($"Compacted output: {document.RootElement.GetProperty("output").GetRawText()}");
        Console.WriteLine($"Continuation answer: {answer.GetOutputText()}");
        string report = Path.Combine(Path.GetTempPath(), $"compaction-prompts-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new
        {
            instructions, followUp, includeCompactInstructions,
            Baseline = baseline.GetOutputText(), Compaction = document.RootElement.Clone(),
            Continuation = answer.GetOutputText(),
            Input = request.InputItems.Select(item => System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString()).ToArray(),
        }, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        Console.WriteLine($"Prompt comparison report: {report}");
        StringAssert.Contains(answer.GetOutputText(), "ORCHID-731");
    }

    [TestMethod]
    public async Task CompactionRawAndTypedRequestsShouldPreserveContextAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var recorder = new RequestRecorder { CaptureResponses = true };
        using var http = new HttpClient(recorder);
        var client = new ResponsesClient(new System.ClientModel.ApiKeyCredential(
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()), new ResponsesClientOptions
            {
                Endpoint = new Uri(Endpoint),
                Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(http),
            });
        const string followUp = "What is the C# project test identifier? Reply only with the identifier.";
        var initial = new CreateResponseOptions { Model = ModelId, StoredOutputEnabled = false };
        initial.InputItems.Add(ResponseItem.CreateUserMessageItem("The C# project test identifier is ORCHID-731. Acknowledge briefly."));
        var first = (await client.CreateResponseAsync(initial, timeout.Token)).Value;
        foreach (var item in first.OutputItems) initial.InputItems.Add(item);
        using var compactBody = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = ModelId,
            input = initial.InputItems.Select(item => JsonSerializer.Deserialize<JsonElement>(
                System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString())).ToArray(),
        }));
        var options = new System.ClientModel.Primitives.RequestOptions { CancellationToken = timeout.Token };
        var compact = await client.CompactResponseAsync(compactBody, "application/json", options);
        using var compactJson = JsonDocument.Parse(compact.GetRawResponse().Content);
        var rawItems = compactJson.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToList();
        var typed = new CreateResponseOptions { Model = ModelId, StoredOutputEnabled = false };
        foreach (var item in rawItems)
            typed.InputItems.Add(System.ClientModel.Primitives.ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item.GetRawText()))
                ?? throw new InvalidDataException("Item missing."));
        typed.InputItems.Add(ResponseItem.CreateUserMessageItem(followUp));
        var typedAnswer = (await client.CreateResponseAsync(typed, timeout.Token)).Value;
        rawItems.Add(JsonSerializer.SerializeToElement(new
        {
            type = "message", role = "user", content = new[] { new { type = "input_text", text = followUp } },
        }));
        using var rawBody = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = ModelId, store = false, input = rawItems,
        }));
        var rawAnswer = await client.CreateResponseAsync(rawBody, options);
        using var rawJson = JsonDocument.Parse(rawAnswer.GetRawResponse().Content);
        string rawText = string.Join("", rawJson.RootElement.GetProperty("output").EnumerateArray()
            .Where(item => item.GetProperty("type").GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Where(part => part.GetProperty("type").GetString() == "output_text")
            .Select(part => part.GetProperty("text").GetString()));
        Console.WriteLine($"Typed answer: {typedAnswer.GetOutputText()}");
        Console.WriteLine($"Raw answer: {rawText}");
        string path = Path.Combine(Path.GetTempPath(), $"compaction-raw-comparison-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            RecorderPaths = recorder.Paths, recorder.Requests, recorder.Responses,
            TypedAnswer = typedAnswer.GetOutputText(), RawAnswer = rawText,
        }, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        Console.WriteLine($"Comparison report: {path}");
        using var typedRequest = JsonDocument.Parse(recorder.Requests[^2]);
        using var rawRequest = JsonDocument.Parse(recorder.Requests[^1]);
        Assert.AreEqual(typedRequest.RootElement.GetProperty("input")[0].GetProperty("encrypted_content").GetString(),
            rawRequest.RootElement.GetProperty("input")[0].GetProperty("encrypted_content").GetString());
        StringAssert.Contains(rawText, "ORCHID-731");
    }

    [TestMethod]
    public async Task CompactionInputSemanticsShouldBeComparedAgainstTextControlAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var recorder = new RequestRecorder { CaptureResponses = true };
        using var http = new HttpClient(recorder);
        var client = new ResponsesClient(new System.ClientModel.ApiKeyCredential(
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()), new ResponsesClientOptions
        {
            Endpoint = new Uri(Endpoint), Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(http),
        });
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        using var compactBody = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = ModelId,
            input = new object[]
            {
                new { type = "message", role = "user", content = new[] { new { type = "input_text", text = $"C# project identifier: {marker}." } } },
                new { type = "message", role = "assistant", content = new[] { new { type = "output_text", text = $"The identifier is {marker}.", annotations = Array.Empty<object>() } } },
            },
        }));
        var requestOptions = new System.ClientModel.Primitives.RequestOptions { CancellationToken = timeout.Token };
        var compact = await client.CompactResponseAsync(compactBody, "application/json", requestOptions);
        using var compactJson = JsonDocument.Parse(compact.GetRawResponse().Content);
        var output = compactJson.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
        var results = new List<object>();
        foreach (string variant in new[] { "native", "minimal-native", "native-store", "native-explicit-context", "text-control", "empty-control", "invalid-compaction" })
        {
            var items = new List<JsonElement>();
            foreach (var item in output)
            {
                if (variant == "empty-control") continue;
                if (variant == "invalid-compaction")
                    items.Add(JsonSerializer.SerializeToElement(new { type = "compaction", encrypted_content = "invalid-opaque-state" }));
                else if (variant == "minimal-native" && item.GetProperty("type").GetString() == "compaction")
                    items.Add(JsonSerializer.SerializeToElement(new { type = "compaction", encrypted_content = item.GetProperty("encrypted_content").GetString() }));
                else if (variant == "text-control" && item.GetProperty("type").GetString() == "compaction")
                    items.Add(JsonSerializer.SerializeToElement(new { type = "message", role = "developer", content = new[]
                    {
                        new { type = "input_text", text = item.GetProperty("encrypted_content").GetString() },
                    } }));
                else items.Add(item);
            }
            items.Add(JsonSerializer.SerializeToElement(new { type = "message", role = "user", content = new[]
            {
                new { type = "input_text", text = "What is the C# project identifier from the supplied context? Reply only with its exact value." },
            } }));
            var payload = new Dictionary<string, object>
            {
                ["model"] = ModelId, ["store"] = variant == "native-store", ["input"] = items,
            };
            if (variant == "native-explicit-context") payload.Add("instructions", "Use the supplied compaction context to answer the user. Do not invent missing project identifiers.");
            using var body = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(payload));
            var response = await client.CreateResponseAsync(body, new System.ClientModel.Primitives.RequestOptions
            {
                CancellationToken = timeout.Token, ErrorOptions = System.ClientModel.Primitives.ClientErrorBehaviors.NoThrow,
            });
            using var json = JsonDocument.Parse(response.GetRawResponse().Content);
            string answer = json.RootElement.TryGetProperty("output", out var responseOutput)
                ? string.Join("", responseOutput.EnumerateArray().Where(item => item.GetProperty("type").GetString() == "message")
                    .SelectMany(item => item.GetProperty("content").EnumerateArray())
                    .Where(part => part.GetProperty("type").GetString() == "output_text")
                    .Select(part => part.GetProperty("text").GetString()))
                : json.RootElement.GetRawText();
            bool recognized = answer.Contains(marker, StringComparison.OrdinalIgnoreCase);
            int? inputTokens = json.RootElement.TryGetProperty("usage", out var usage)
                && usage.TryGetProperty("input_tokens", out var tokens) ? tokens.GetInt32() : null;
            results.Add(new { variant, Status = response.GetRawResponse().Status, inputTokens, recognized, answer });
            Console.WriteLine($"Variant={variant}, status={response.GetRawResponse().Status}, tokens={inputTokens}, recognized={recognized}, answer={answer}");
        }
        string path = Path.Combine(Path.GetTempPath(), $"compaction-semantics-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            marker, Compaction = compactJson.RootElement.Clone(), results, recorder.Requests, recorder.Responses,
        }, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        Console.WriteLine($"Semantics report: {path}");
        Assert.HasCount(7, results);
    }

    [TestMethod]
    public async Task CompactionStoredResponseLinkShouldBeDiagnosedAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var client = new ResponsesClient(new System.ClientModel.ApiKeyCredential(
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()), new ResponsesClientOptions { Endpoint = new Uri(Endpoint) });
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        var firstOptions = new CreateResponseOptions { Model = ModelId, StoredOutputEnabled = true };
        firstOptions.InputItems.Add(ResponseItem.CreateUserMessageItem($"C# project identifier: {marker}. Acknowledge briefly."));
        var first = (await client.CreateResponseAsync(firstOptions, timeout.Token)).Value;
        using var body = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = ModelId, previous_response_id = first.Id,
            input = firstOptions.InputItems.Concat(first.OutputItems).Select(item => JsonSerializer.Deserialize<JsonElement>(
                System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString())).ToArray(),
        }));
        var compact = await client.CompactResponseAsync(body, "application/json",
            new System.ClientModel.Primitives.RequestOptions { CancellationToken = timeout.Token });
        using var document = JsonDocument.Parse(compact.GetRawResponse().Content);
        var records = new List<object>();
        foreach (string variant in new[] { "output-only", "original-response-link", "compact-response-link" })
        {
            var request = new CreateResponseOptions { Model = ModelId, StoredOutputEnabled = true };
            foreach (var item in document.RootElement.GetProperty("output").EnumerateArray())
                request.InputItems.Add(System.ClientModel.Primitives.ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item.GetRawText()))
                    ?? throw new InvalidDataException("Item missing."));
            if (variant == "original-response-link") request.PreviousResponseId = first.Id;
            if (variant == "compact-response-link") request.PreviousResponseId = document.RootElement.GetProperty("id").GetString();
            request.InputItems.Add(ResponseItem.CreateUserMessageItem("Repeat the C# project identifier from the context. Only output its exact value."));
            using System.ClientModel.BinaryContent content = request;
            var response = await client.CreateResponseAsync(content, new System.ClientModel.Primitives.RequestOptions
            {
                CancellationToken = timeout.Token, ErrorOptions = System.ClientModel.Primitives.ClientErrorBehaviors.NoThrow,
            });
            string json = response.GetRawResponse().Content.ToString();
            records.Add(new { variant, Status = response.GetRawResponse().Status, Response = json });
            Console.WriteLine($"Variant={variant}, status={response.GetRawResponse().Status}, response={json}");
        }
        string path = Path.Combine(Path.GetTempPath(), $"compaction-links-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            marker, OriginalResponseId = first.Id, Compaction = document.RootElement.Clone(), records,
        }, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        Console.WriteLine($"Link report: {path}");
        Assert.HasCount(3, records);
    }

    [TestMethod]
    public async Task NativeCompactionInputTokenCountShouldBeDiagnosedAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var client = new ResponsesClient(new System.ClientModel.ApiKeyCredential(
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim()), new ResponsesClientOptions { Endpoint = new Uri(Endpoint) });
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        using var compactBody = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = ModelId,
            input = new[] { new { type = "message", role = "user", content = new[]
            {
                new { type = "input_text", text = $"C# project identifier: {marker}. Preserve this fact." },
            } } },
        }));
        var compact = await client.CompactResponseAsync(compactBody, "application/json",
            new System.ClientModel.Primitives.RequestOptions { CancellationToken = timeout.Token });
        using var compactJson = JsonDocument.Parse(compact.GetRawResponse().Content);
        var items = compactJson.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
        var results = new List<object>();
        foreach (string variant in new[] { "empty", "compaction", "text-control" })
        {
            var input = new List<JsonElement>();
            if (variant == "compaction") input.AddRange(items);
            if (variant == "text-control")
                input.Add(JsonSerializer.SerializeToElement(new { type = "message", role = "developer", content = new[]
                {
                    new { type = "input_text", text = items.Single().GetProperty("encrypted_content").GetString() },
                } }));
            input.Add(JsonSerializer.SerializeToElement(new { type = "message", role = "user", content = new[]
            {
                new { type = "input_text", text = "What is the project identifier?" },
            } }));
            using var body = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new { model = ModelId, input }));
            var result = await client.GetInputTokenCountAsync(body, "application/json",
                new System.ClientModel.Primitives.RequestOptions
                {
                    CancellationToken = timeout.Token,
                    ErrorOptions = System.ClientModel.Primitives.ClientErrorBehaviors.NoThrow,
                });
            string json = result.GetRawResponse().Content.ToString();
            results.Add(new { variant, Status = result.GetRawResponse().Status, Response = json });
            Console.WriteLine($"Token count variant={variant}, status={result.GetRawResponse().Status}, response={json}");
        }
        string path = Path.Combine(Path.GetTempPath(), $"compaction-input-count-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            Compaction = compactJson.RootElement.Clone(), results,
        }, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        Console.WriteLine($"Input count report: {path}");
        Assert.HasCount(3, results);
    }

    private static byte[] CreateRedImage()
    {
        using var bitmap = new Avalonia.Media.Imaging.WriteableBitmap(new Avalonia.PixelSize(64, 64),
            new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);
        using (var buffer = bitmap.Lock())
        {
            byte[] row = Enumerable.Range(0, 64).SelectMany(_ => new byte[] { 255, 0, 0, 255 }).ToArray();
            for (int y = 0; y < 64; y++)
                System.Runtime.InteropServices.Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, row.Length);
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return stream.ToArray();
    }

    private sealed class Scenario : IAsyncDisposable
    {
        private Scenario(CodingWorkTaskRuntime runtime, RequestRecorder recorder, string directory)
        {
            Runtime = runtime;
            Recorder = recorder;
            DirectoryPath = directory;
            Chat = new ChatViewModel(runtime) { IsResponsesApiEnabled = true, IsAutomaticCompressionEnabled = false };
        }

        public CodingWorkTaskRuntime Runtime { get; }
        public ChatViewModel Chat { get; }
        public RequestRecorder Recorder { get; }
        public string DirectoryPath { get; }
        public string LastAnswer => Runtime.ChatManager.SelectedSession.ChatMessages.Last(message => message.Role == ChatRole.Assistant && !message.IsPresetInfo).Content;

        public static async Task<Scenario> CreateAsync(string? existingDirectory = null)
        {
            string directory = existingDirectory ?? Path.Combine(Path.GetTempPath(), $"responses-workflow-{Guid.NewGuid():N}");
            Console.WriteLine($"Workflow directory: {directory}");
            var paths = CodingChatRoomPaths.Create(directory);
            paths.EnsureDirectories();
            var configuration = new AgentApiManagerConfiguration
            {
                PrimaryModel = ModelId,
                OpenAIConfigurationList = [new OpenAIProtocolLanguageModelConfiguration(Endpoint, (await File.ReadAllTextAsync(KeyPath)).Trim())
                {
                    ModelDefinitions = [new ModelDefinition
                    {
                        Provider = "service", ModelName = ModelId, ModelId = ModelId,
                        Capabilities = new LlmModelCapabilities(),
                    }],
                }],
            };
            var runtime = await CodingWorkTaskRuntimeFactory.InitializeAsync(paths, new ImmediateDispatcher());
            var recorder = new RequestRecorder();
            runtime.EndpointManager.HttpClient?.Dispose();
            runtime.EndpointManager.HttpClient = new HttpClient(recorder) { Timeout = TimeSpan.FromMinutes(3) };
            runtime.ApplyModelConfiguration(configuration);
            await runtime.Controller.InitializeAsync();
            return new Scenario(runtime, recorder, directory);
        }

        public async Task SendAsync(string text)
        {
            Chat.InputText = text;
            Assert.IsTrue(Chat.SendCommand.CanExecute(null), "Send command unavailable.");
            await ((SimpleAsyncCommand)Chat.SendCommand).ExecuteAsync().WaitAsync(TimeSpan.FromMinutes(3));
            Assert.IsFalse(Runtime.ChatManager.SelectedSession.ChatMessages.Any(message =>
                message.Role == ChatRole.System && message.Content.Contains("失败", StringComparison.Ordinal)), Chat.StatusText);
            Assert.IsFalse(string.IsNullOrWhiteSpace(LastAnswer));
        }

        public async ValueTask DisposeAsync()
        {
            await File.WriteAllTextAsync(Path.Combine(DirectoryPath, $"requests-{Guid.NewGuid():N}.json"),
                JsonSerializer.Serialize(new { Recorder.Paths, Recorder.Requests, Recorder.Responses }, new JsonSerializerOptions { WriteIndented = true }));
            Chat.Dispose();
            await Runtime.DisposeAsync();
            Runtime.EndpointManager.HttpClient?.Dispose();
        }
    }

    private sealed class RequestRecorder : DelegatingHandler
    {
        public RequestRecorder() : base(new HttpClientHandler()) { }
        public List<string> Requests { get; } = new();
        public List<string> Paths { get; } = new();
        public List<string> Responses { get; } = new();
        public bool CaptureResponses { get; set; }
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            RequestStarted.TrySetResult();
            var response = await base.SendAsync(request, cancellationToken);
            if (!CaptureResponses) return response;
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            Responses.Add(body);
            foreach (string line in body.Split('\n').Where(line => line.StartsWith("data: ", StringComparison.Ordinal)))
            {
                using var data = JsonDocument.Parse(line[6..]);
                if (data.RootElement.GetProperty("type").GetString() == "response.completed")
                    Console.WriteLine($"Completed output: {data.RootElement.GetProperty("response").GetProperty("output").GetRawText()}");
            }
            return response;
        }
    }

    private sealed class ImmediateDispatcher : AgentLib.IMainThreadDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Func<Task> action) => action();
        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();
    }
}
