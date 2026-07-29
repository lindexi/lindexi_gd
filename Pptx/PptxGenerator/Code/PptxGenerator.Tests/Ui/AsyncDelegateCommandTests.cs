using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Ui;

[TestClass]
[DoNotParallelize]
public sealed class AsyncDelegateCommandTests
{
    [TestMethod(DisplayName = "异步命令应暴露完整执行任务")]
    [Timeout(60_000)]
    public async Task ExecuteAsync_ExposesCompleteExecution()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var notificationCount = 0;
        var command = new AsyncDelegateCommand(
            async () =>
            {
                started.TrySetResult();
                await release.Task.ConfigureAwait(false);
            },
            _ => Assert.Fail("The command should not fail."));
        command.CanExecuteChanged += (_, _) => Interlocked.Increment(ref notificationCount);

        Task executionTask = command.ExecuteAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreSame(executionTask, command.ExecutionTask);
        Assert.IsTrue(command.IsExecuting);
        Assert.IsFalse(command.CanExecute(null));

        release.TrySetResult();
        await executionTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(command.IsExecuting);
        Assert.IsTrue(command.CanExecute(null));
        Assert.AreEqual(2, notificationCount);
    }

    [TestMethod(DisplayName = "ICommand 同步入口应观察异步执行异常")]
    [Timeout(60_000)]
    public async Task Execute_ObservesFailure()
    {
        var observedException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncDelegateCommand(
            () => Task.FromException(new InvalidOperationException("expected")),
            exception => observedException.TrySetResult(exception));

        command.Execute(null);

        Exception exception = await observedException.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsInstanceOfType<InvalidOperationException>(exception);
        Assert.AreEqual("expected", exception.Message);
    }

    [TestMethod(DisplayName = "ICommand 执行任务应包含异常观察回调")]
    [Timeout(60_000)]
    public async Task Execute_ExecutionTaskIncludesExceptionObserver()
    {
        var exceptionObserved = false;
        var command = new AsyncDelegateCommand(
            () => Task.FromException(new InvalidOperationException("expected")),
            _ => exceptionObserved = true);

        command.Execute(null);
        await command.ExecutionTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(exceptionObserved);
    }

    [TestMethod(DisplayName = "异步命令应保持 WPF 命令入口的 UI 上下文")]
    [Timeout(60_000)]
    public async Task Execute_PreservesWpfCommandEntryContext()
    {
        var threadIds = new List<int>();

        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            var command = new AsyncDelegateCommand(
                async () =>
                {
                    threadIds.Add(Environment.CurrentManagedThreadId);
                    await Task.Yield();
                    Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                    threadIds.Add(Environment.CurrentManagedThreadId);
                },
                _ => Assert.Fail("The command should not fail."));
            command.CanExecuteChanged += (_, _) =>
            {
                Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                threadIds.Add(Environment.CurrentManagedThreadId);
            };

            command.Execute(null);
            await command.ExecutionTask;
        });

        Assert.HasCount(4, threadIds);
        Assert.AreEqual(1, threadIds.Distinct().Count());
    }

    [TestMethod(DisplayName = "泛型异步命令应保持 WPF 命令入口的 UI 上下文")]
    [Timeout(60_000)]
    public async Task GenericExecute_PreservesWpfCommandEntryContext()
    {
        const string ExpectedParameter = "expected";
        var observedParameter = string.Empty;
        var threadIds = new List<int>();

        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            var command = new AsyncDelegateCommand<string>(
                async parameter =>
                {
                    observedParameter = parameter;
                    threadIds.Add(Environment.CurrentManagedThreadId);
                    await Task.Yield();
                    Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                    threadIds.Add(Environment.CurrentManagedThreadId);
                },
                _ => Assert.Fail("The command should not fail."));
            command.CanExecuteChanged += (_, _) =>
            {
                Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                threadIds.Add(Environment.CurrentManagedThreadId);
            };

            command.Execute(ExpectedParameter);
            await command.ExecutionTask;
        });

        Assert.AreEqual(ExpectedParameter, observedParameter);
        Assert.HasCount(4, threadIds);
        Assert.AreEqual(1, threadIds.Distinct().Count());
    }

    [TestMethod(DisplayName = "重新渲染命令应通过 ViewModel 提交开始和结束状态")]
    [Timeout(60_000)]
    public async Task RerenderCommand_CommitsStartAndFinalViewModelStateThroughDispatcher()
    {
        var dispatcher = new RecordingDispatcher();
        var chatManager = new CopilotChatManager();
        var chatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, "unused"))),
        };
        chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
            new FakeLanguageModelProvider(chatClient));
        var renderTool = new SlideMlRenderTool(new YieldingRenderPipeline(), dispatcher);
        var viewModel = new MainWindowViewModel(
            new SlideChatManager(chatManager, renderTool),
            dispatcher)
        {
            EditableSlideXml = "<Page/>",
        };
        var observedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) =>
        {
            Assert.IsTrue(dispatcher.IsDispatching, $"{e.PropertyName} was raised outside the dispatcher.");
            if (e.PropertyName is not null)
            {
                observedProperties.Add(e.PropertyName);
            }
        };

        var command = (AsyncDelegateCommand)viewModel.RerenderCommand;
        await command.ExecuteAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(viewModel.IsBusy);
        Assert.AreEqual("重新渲染完成", viewModel.StatusText);
        CollectionAssert.Contains(observedProperties, nameof(MainWindowViewModel.IsBusy));
        CollectionAssert.Contains(observedProperties, nameof(MainWindowViewModel.StatusText));
        Assert.AreEqual("<Page/>", viewModel.SlideChatManager.CurrentSlideXml);
    }

    private sealed class RecordingDispatcher : IMainThreadDispatcher
    {
        private readonly AsyncLocal<bool> _isDispatching = new();

        public int InvocationCount { get; private set; }

        public bool IsDispatching => _isDispatching.Value;

        public bool CheckAccess() => IsDispatching;

        public async Task InvokeAsync(Func<Task> action)
        {
            InvocationCount++;
            _isDispatching.Value = true;
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                _isDispatching.Value = false;
            }
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            InvocationCount++;
            _isDispatching.Value = true;
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                _isDispatching.Value = false;
            }
        }
    }

    private sealed class YieldingRenderPipeline : ISlideMlRenderPipeline
    {
        public async Task<SlideMlRenderResult> RenderAsync(
            string slideXml,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new SlideMlRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Warnings = Array.Empty<string>(),
                Errors = Array.Empty<string>(),
                PreviewImage = new FakePreviewImage(),
            };
        }
    }

    private sealed class FakePreviewImage : IPreviewImage
    {
        public void Save(string filePath)
        {
            File.WriteAllBytes(filePath, []);
        }

        public void Save(Stream stream)
        {
        }
    }
}