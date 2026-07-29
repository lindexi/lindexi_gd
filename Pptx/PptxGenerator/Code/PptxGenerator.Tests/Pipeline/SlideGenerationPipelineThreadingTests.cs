using System.ComponentModel;
using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using PptxGenerator.Evaluation;
using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Pipeline.Tests;

[TestClass]
public sealed class SlideGenerationPipelineThreadingTests
{
    [TestMethod]
    public async Task ApplyRenderResultAsync_NotifiesEachRenderPropertyOnce()
    {
        var dispatcher = new RecordingDispatcher();
        var renderTool = new SlideMlRenderTool(new UnusedRenderPipeline(), dispatcher);
        var chatManager = new SlideChatManager(new CopilotChatManager(), renderTool);
        var notificationCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        chatManager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
            {
                notificationCounts[e.PropertyName] = notificationCounts.GetValueOrDefault(e.PropertyName) + 1;
            }
        };

        await renderTool.ApplyRenderResultAsync(CreateRenderResult());

        Assert.AreEqual(1, notificationCounts[nameof(SlideChatManager.PreviewImage)]);
        Assert.AreEqual(1, notificationCounts[nameof(SlideChatManager.CurrentSlideXml)]);
        Assert.AreEqual(1, notificationCounts[nameof(SlideChatManager.RenderedXml)]);
        Assert.AreEqual(1, notificationCounts[nameof(SlideChatManager.WarningText)]);
    }

    [TestMethod]
    public async Task EvaluatePromptAsync_CommitsObservableStateThroughDispatcherInOrder()
    {
        var dispatcher = new RecordingDispatcher();
        var renderTool = new SlideMlRenderTool(new UnusedRenderPipeline(), dispatcher);
        var expectedResult = PromptEvaluationResult.Failed("expected");
        var pipeline = new SlideGenerationPipeline(
            new CopilotChatManager(),
            new SlideMlPromptProvider(),
            renderTool,
            slideEvaluator: null,
            promptEvaluator: new YieldingPromptEvaluator(expectedResult));
        var events = new List<string>();
        pipeline.PropertyChanged += (_, e) => RecordPropertyChange(pipeline, e, events);
        pipeline.PromptEvaluationCompleted += (_, result) =>
        {
            Assert.AreSame(expectedResult, result);
            events.Add("completed");
        };

        var actualResult = await pipeline.EvaluatePromptAsync();

        Assert.AreSame(expectedResult, actualResult);
        CollectionAssert.AreEqual(
            new[] { "evaluating:true", "result", "completed", "evaluating:false" },
            events);
        Assert.AreEqual(3, dispatcher.InvocationCount);
    }

    [TestMethod]
    public async Task SendMessageAsync_DoesNotWaitForAutomaticEvaluationButTracksItsCompletion()
    {
        var dispatcher = new RecordingDispatcher();
        var renderTool = new SlideMlRenderTool(new UnusedRenderPipeline(), dispatcher);
        await renderTool.ApplyRenderResultAsync(CreateRenderResult());
        var releaseEvaluation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expectedResult = SlideEvaluationResult.Failed("expected");
        var evaluator = new GatedSlideEvaluator(releaseEvaluation.Task, expectedResult);
        var chatManager = new CopilotChatManager();
        var chatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, "rendered"))),
        };
        chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
            new FakeLanguageModelProvider(chatClient));
        var pipeline = new SlideGenerationPipeline(
            chatManager,
            new SlideMlPromptProvider(),
            renderTool,
            slideEvaluator: evaluator);

        Task sendTask = pipeline.SendMessageAsync(
            "test",
            isFirstMessage: false,
            attachPreview: false,
            skipAutoEvaluation: false);
        await evaluator.Started.WaitAsync(TimeSpan.FromSeconds(2));

        await sendTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(pipeline.IsEvaluating);

        releaseEvaluation.TrySetResult();
        await pipeline.AutomaticEvaluationTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreSame(expectedResult, pipeline.LastSlideEvaluation);
        Assert.IsFalse(pipeline.IsEvaluating);
    }

    [TestMethod(DisplayName = "重叠自动评估只应提交最新结果并等待全部在途任务")]
    [Timeout(60_000)]
    public async Task OverlappingAutomaticEvaluations_OnlyLatestResultIsCommittedAndAllTasksAreTracked()
    {
        var dispatcher = new RecordingDispatcher();
        var renderTool = new SlideMlRenderTool(new UnusedRenderPipeline(), dispatcher);
        await renderTool.ApplyRenderResultAsync(CreateRenderResult());
        var evaluator = new OverlappingSlideEvaluator();
        var chatManager = new CopilotChatManager();
        var chatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, "rendered"))),
        };
        chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
            new FakeLanguageModelProvider(chatClient));
        var pipeline = new SlideGenerationPipeline(
            chatManager,
            new SlideMlPromptProvider(),
            renderTool,
            slideEvaluator: evaluator);
        var secondCommitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pipeline.EvaluationCompleted += (_, result) =>
        {
            if (ReferenceEquals(result, evaluator.SecondResult))
            {
                secondCommitted.TrySetResult();
            }
        };

        await pipeline.SendMessageAsync(
            "first",
            isFirstMessage: false,
            attachPreview: false,
            skipAutoEvaluation: false);
        await evaluator.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await pipeline.SendMessageAsync(
            "second",
            isFirstMessage: false,
            attachPreview: false,
            skipAutoEvaluation: false);
        await evaluator.SecondStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var allEvaluationsTask = pipeline.AutomaticEvaluationTask;

        evaluator.ReleaseSecond.TrySetResult();
        await evaluator.SecondCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await secondCommitted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreSame(evaluator.SecondResult, pipeline.LastSlideEvaluation);
        Assert.IsFalse(allEvaluationsTask.IsCompleted);

        evaluator.ReleaseFirst.TrySetResult();
        await allEvaluationsTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreSame(evaluator.SecondResult, pipeline.LastSlideEvaluation);
        Assert.IsFalse(pipeline.IsEvaluating);
    }

    private static void RecordPropertyChange(
        SlideGenerationPipeline pipeline,
        PropertyChangedEventArgs e,
        ICollection<string> events)
    {
        switch (e.PropertyName)
        {
            case nameof(SlideGenerationPipeline.IsEvaluating):
                events.Add($"evaluating:{pipeline.IsEvaluating.ToString().ToLowerInvariant()}");
                break;
            case nameof(SlideGenerationPipeline.LastPromptEvaluation):
                events.Add("result");
                break;
        }
    }

    private sealed class OverlappingSlideEvaluator : ISlideEvaluator
    {
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseSecond { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public SlideEvaluationResult FirstResult { get; } = SlideEvaluationResult.Failed("first");
        public SlideEvaluationResult SecondResult { get; } = SlideEvaluationResult.Failed("second");

        public async Task<SlideEvaluationResult> EvaluateAsync(
            string userPrompt,
            string slideXml,
            string renderedXml,
            string warnings,
            byte[]? previewImageBytes,
            IPreviewImage? originalScreenshot = null,
            CancellationToken cancellationToken = default)
        {
            if (userPrompt == "first")
            {
                FirstStarted.TrySetResult();
                await ReleaseFirst.Task.ConfigureAwait(false);
                return FirstResult;
            }

            SecondStarted.TrySetResult();
            await ReleaseSecond.Task.ConfigureAwait(false);
            SecondCompleted.TrySetResult();
            return SecondResult;
        }
    }

    private sealed class GatedSlideEvaluator(Task releaseTask, SlideEvaluationResult result) : ISlideEvaluator
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public async Task<SlideEvaluationResult> EvaluateAsync(
            string userPrompt,
            string slideXml,
            string renderedXml,
            string warnings,
            byte[]? previewImageBytes,
            IPreviewImage? originalScreenshot = null,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await releaseTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    private static SlideMlRenderResult CreateRenderResult() => new()
    {
        InputXml = "<Page/>",
        OutputXml = "<Page/>",
        Warnings = Array.Empty<string>(),
        Errors = Array.Empty<string>(),
        PreviewImage = new FakePreviewImage(),
    };

    private sealed class YieldingPromptEvaluator(PromptEvaluationResult result) : IPromptEvaluator
    {
        public async Task<PromptEvaluationResult> EvaluateAsync(
            string systemPrompt,
            string userPromptTemplate,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return result;
        }
    }

    private sealed class UnusedRenderPipeline : ISlideMlRenderPipeline
    {
        public Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingDispatcher : IMainThreadDispatcher
    {
        public int InvocationCount { get; private set; }

        public bool CheckAccess() => false;

        public async Task InvokeAsync(Func<Task> action)
        {
            InvocationCount++;
            await action().ConfigureAwait(false);
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            InvocationCount++;
            return await action().ConfigureAwait(false);
        }
    }
}
