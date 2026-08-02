using WinRemoteShell.Client;

namespace WinRemoteShell.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ServerIntegrationTests
{
    [TestMethod]
    public async Task WhenExecRunsTwiceThenCmdStateIsPreserved()
    {
        await using var host = await TestServerHost.StartAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        using var firstOutput = new StringWriter();
        using var secondOutput = new StringWriter();

        await ExecClient.ExecuteAsync(host.Address, ["cd", "/d", directory], null, firstOutput);
        await ExecClient.ExecuteAsync(host.Address, ["cd"], null, secondOutput);

        StringAssert.Contains(secondOutput.ToString(), directory);
    }

    [TestMethod]
    public async Task WhenFileIsPushedAndPulledThenContentIsPreserved()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source.txt");
        var remote = Path.Combine(root, "remote.txt");
        var output = Path.Combine(root, "output.txt");
        await File.WriteAllTextAsync(source, "remote content");

        await PushClient.PushAsync(host.Address, source, remote);
        await PullClient.PullAsync(host.Address, remote, output);

        Assert.AreEqual("remote content", await File.ReadAllTextAsync(output));
    }

    [TestMethod]
    public async Task WhenDirectoryIsPushedAndPulledThenNestedContentIsPreserved()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var nested = Path.Combine(source, "nested");
        var remote = Path.Combine(root, "remote");
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(nested);
        await File.WriteAllTextAsync(Path.Combine(nested, "content.txt"), "nested content");

        await PushClient.PushAsync(host.Address, source, remote);
        await PullClient.PullAsync(host.Address, remote, output);

        Assert.AreEqual("nested content", await File.ReadAllTextAsync(Path.Combine(output, "nested", "content.txt")));
    }

    [TestMethod]
    public async Task WhenShellExitsThenCmdRemainsAvailableToExec()
    {
        await using var host = await TestServerHost.StartAsync();
        using var shellInput = new StringReader("echo shell-ready\nexit\n");
        using var shellOutput = new StringWriter();
        using var execOutput = new StringWriter();

        await ShellClient.RunAsync(host.Address, shellInput, shellOutput);
        await ExecClient.ExecuteAsync(host.Address, ["echo", "exec-ready"], null, execOutput);

        StringAssert.Contains(execOutput.ToString(), "exec-ready");
    }

    [TestMethod]
    public async Task WhenScreenshotIsCapturedThenFileHasPngSignature()
    {
        await using var host = await TestServerHost.StartAsync();
        var output = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}.png");

        await ScreenshotClient.CaptureAsync(host.Address, output);
        var header = new byte[8];
        await using var file = File.OpenRead(output);
        await file.ReadExactlyAsync(header);

        CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header);
    }
}