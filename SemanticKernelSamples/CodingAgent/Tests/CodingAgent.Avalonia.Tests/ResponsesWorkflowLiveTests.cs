using System.Text.Json;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
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
        CollectionAssert.AreEqual(originalItems, restored.Runtime.ChatManager.SelectedSession.ResponsesSession.Items
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
        var calls = history.OfType<FunctionCallResponseItem>().ToArray();
        Assert.IsNotEmpty(calls);
        Assert.IsTrue(history.OfType<FunctionCallOutputResponseItem>().Any(output =>
            calls.Any(call => call.CallId == output.CallId) && output.FunctionOutput.Contains(marker, StringComparison.Ordinal)));
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
        Assert.IsTrue(ImageAttachmentViewModel.TryCreate("sample.png", CreateRedImage(), out var attachment));
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
        Assert.IsTrue(scenario.Chat.CompressConversationCommand.CanExecute(null));
        await ((SimpleAsyncCommand)scenario.Chat.CompressConversationCommand).ExecuteAsync();
        Assert.IsTrue(scenario.Recorder.Paths.Any(path => path.EndsWith("/responses/compact", StringComparison.Ordinal)));
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
                JsonSerializer.Serialize(new { Recorder.Paths, Recorder.Requests }, new JsonSerializerOptions { WriteIndented = true }));
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
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            RequestStarted.TrySetResult();
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class ImmediateDispatcher : AgentLib.IMainThreadDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Func<Task> action) => action();
        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();
    }
}
