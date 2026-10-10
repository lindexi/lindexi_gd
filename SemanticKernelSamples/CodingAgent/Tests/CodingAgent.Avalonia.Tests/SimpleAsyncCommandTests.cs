using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class SimpleAsyncCommandTests
{
    [TestMethod(DisplayName = "异步命令失败时应将异常交给错误处理器")]
    public async Task WhenExecutionFailsThenExceptionHandlerReceivesException()
    {
        Exception? reportedException = null;
        var expectedException = new InvalidOperationException("test failure");
        var command = new SimpleAsyncCommand(
            () => Task.FromException(expectedException),
            exceptionHandler: exception => reportedException = exception);

        await command.ExecuteAsync();

        Assert.AreSame(expectedException, reportedException);
    }

    [TestMethod(DisplayName = "异步命令失败后应恢复可执行状态")]
    public async Task WhenExecutionFailsThenCommandBecomesExecutableAgain()
    {
        var command = new SimpleAsyncCommand(
            () => Task.FromException(new InvalidOperationException("test failure")),
            exceptionHandler: static _ => { });

        await command.ExecuteAsync();

        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod(DisplayName = "带参数异步命令失败时应将异常交给错误处理器")]
    public async Task WhenParameterizedExecutionFailsThenExceptionHandlerReceivesException()
    {
        Exception? reportedException = null;
        var expectedException = new InvalidOperationException("test failure");
        var command = new SimpleAsyncCommand<string>(
            _ => Task.FromException(expectedException),
            exceptionHandler: exception => reportedException = exception);

        await command.ExecuteAsync("value");

        Assert.AreSame(expectedException, reportedException);
    }
}
