using AgentLib.Coding.Sandboxes;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class WinRemoteShellProcessRunnerTests
{
    [TestMethod(DisplayName = "服务端异常堆栈即使客户端退出码为零也应视为失败")]
    public void EnsureRemoteExecutionSucceeded_WhenServerReturnsException_Throws()
    {
        const string output = """
            System.ComponentModel.Win32Exception (2): An error occurred trying to start process 'missing.exe'.
               at WinRemoteShell.Server.DirectProcessExecutor.StartProcess(IReadOnlyList`1 arguments, String workingDirectory)
               at WinRemoteShell.Server.ServerHost.MapExec()
            """;

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => WinRemoteShellProcessRunner.EnsureRemoteExecutionSucceeded(output));

        StringAssert.Contains(exception.Message, "WinRemoteShell 远端执行失败");
    }

    [TestMethod(DisplayName = "普通远端输出不应被误判为失败")]
    public void EnsureRemoteExecutionSucceeded_WhenOutputIsNormal_DoesNotThrow()
    {
        WinRemoteShellProcessRunner.EnsureRemoteExecutionSucceeded("windows-sandbox-integration-success");
    }
}
