using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class AsyncRelayCommandTests
{
    [TestMethod(DisplayName = "异步命令应暴露可等待执行任务并在执行期间禁用")]
    [Timeout(60_000)]
    public async Task ExecuteAsyncShouldExposeAwaitableExecutionAndDisableWhileRunning()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(async _ =>
        {
            started.TrySetResult();
            await release.Task;
        });

        var executionTask = command.ExecuteAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreSame(executionTask, command.ExecutionTask);
        Assert.IsTrue(command.IsExecuting);
        Assert.IsFalse(command.CanExecute(null));

        release.TrySetResult();
        await executionTask;

        Assert.IsFalse(command.IsExecuting);
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod(DisplayName = "异步命令在 UI 重入时默认只应执行一次")]
    [Timeout(60_000)]
    public async Task ExecuteAsyncShouldRejectReentrantExecution()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;
        var command = new AsyncRelayCommand(async _ =>
        {
            executionCount++;
            started.TrySetResult();
            await release.Task;
        });

        var firstExecutionTask = command.ExecuteAsync();
        var reentrantExecutionTasks = Enumerable.Range(0, 7)
            .Select(_ => command.ExecuteAsync())
            .ToArray();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        release.TrySetResult();
        await firstExecutionTask;
        await Task.WhenAll(reentrantExecutionTasks);

        Assert.AreEqual(1, executionCount);
    }

    [TestMethod(DisplayName = "ICommand 同步入口应观察异步执行异常")]
    [Timeout(60_000)]
    public async Task ExecuteShouldObserveException()
    {
        var observedException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(
            _ => Task.FromException(new InvalidOperationException("expected")),
            onException: exception => observedException.TrySetResult(exception));

        command.Execute(null);
        var exception = await observedException.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsInstanceOfType<InvalidOperationException>(exception);
        Assert.AreEqual("expected", exception.Message);
    }

    [TestMethod(DisplayName = "ICommand 执行任务应包含异常观察回调")]
    [Timeout(60_000)]
    public async Task ExecuteShouldExposeExceptionObserverCompletion()
    {
        var exceptionObserved = false;
        var command = new AsyncRelayCommand(
            _ => Task.FromException(new InvalidOperationException("expected")),
            onException: _ => exceptionObserved = true);

        command.Execute(null);
        await command.ExecutionTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsTrue(exceptionObserved);
    }
}
