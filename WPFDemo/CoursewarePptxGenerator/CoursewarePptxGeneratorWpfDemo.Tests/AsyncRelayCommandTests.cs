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

    [TestMethod(DisplayName = "异步命令并发启动时默认只应执行一次")]
    [Timeout(60_000)]
    public async Task ExecuteAsyncShouldAtomicallyRejectConcurrentExecution()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;
        var command = new AsyncRelayCommand(async _ =>
        {
            Interlocked.Increment(ref executionCount);
            started.TrySetResult();
            await release.Task;
        });

        var executionTasks = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Task.Run<Task>(() => command.ExecuteAsync())));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        release.TrySetResult();
        await Task.WhenAll(executionTasks);

        Assert.AreEqual(1, executionCount);
    }
}
