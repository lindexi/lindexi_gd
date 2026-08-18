using System.Net;
using WinRemoteShell.Client;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ServerIntegrationTests
{
    [TestMethod]
    public async Task WhenDirectoryChangesThenListUsesNewWorkingDirectory()
    {
        await using var host = await TestServerHost.StartAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        await ChangeDirectoryClient.ChangeAsync(host.Address, directory);
        var listing = await ListClient.ListAsync(host.Address);

        Assert.AreEqual(directory, listing.Path);
    }

    [TestMethod]
    public async Task WhenDirectoryIsListedThenStructuredEntriesAreReturned()
    {
        await using var host = await TestServerHost.StartAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "content.txt");
        await File.WriteAllTextAsync(filePath, "content");
        var file = new FileInfo(filePath);
        await ChangeDirectoryClient.ChangeAsync(host.Address, directory);

        var listing = await ListClient.ListAsync(host.Address);

        Assert.AreEqual(
            new RemoteDirectoryEntry(
                file.Name,
                file.FullName,
                false,
                file.Length,
                file.CreationTimeUtc,
                file.LastWriteTimeUtc),
            listing.Entries.Single());
    }

    [TestMethod]
    public async Task WhenAbsoluteDirectoryIsSpecifiedThenThatDirectoryIsListed()
    {
        await using var host = await TestServerHost.StartAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "absolute.txt"), "content");

        var listing = await ListClient.ListAsync(host.Address, directory);

        Assert.AreEqual("absolute.txt", listing.Entries.Single().Name);
    }

    [TestMethod]
    public async Task WhenRelativeDirectoryIsSpecifiedThenItIsResolvedFromWorkingDirectory()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(child);
        await File.WriteAllTextAsync(Path.Combine(child, "relative.txt"), "content");
        await ChangeDirectoryClient.ChangeAsync(host.Address, root);

        var listing = await ListClient.ListAsync(host.Address, "child");

        Assert.AreEqual("relative.txt", listing.Entries.Single().Name);
    }

    [TestMethod]
    public async Task WhenDirectoryIsSpecifiedThenWorkingDirectoryIsUnchanged()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(child);
        await ChangeDirectoryClient.ChangeAsync(host.Address, root);

        await ListClient.ListAsync(host.Address, child);
        var listing = await ListClient.ListAsync(host.Address);

        Assert.AreEqual(root, listing.Path);
    }

    [TestMethod]
    public async Task WhenExecRunsExecutableThenOutputIsReturned()
    {
        await using var host = await TestServerHost.StartAsync();
        using var output = new StringWriter();

        await ExecClient.ExecuteAsync(host.Address, ["where.exe", "cmd.exe"], null, output);

        StringAssert.Contains(output.ToString(), "cmd.exe");
    }

    [TestMethod]
    public async Task WhenExecRunsExecutableFromWorkingDirectoryThenItIsFoundFirst()
    {
        await using var host = await TestServerHost.StartAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var executableName = $"where-{Guid.NewGuid():N}.exe";
        File.Copy(Path.Combine(Environment.SystemDirectory, "where.exe"), Path.Combine(directory, executableName));
        await ChangeDirectoryClient.ChangeAsync(host.Address, directory);
        using var output = new StringWriter();

        await ExecClient.ExecuteAsync(host.Address, [executableName, "cmd.exe"], null, output);

        StringAssert.Contains(output.ToString(), "cmd.exe");
    }

    [TestMethod]
    public async Task WhenExecRunsCmdExplicitlyThenShellCommandIsSupported()
    {
        await using var host = await TestServerHost.StartAsync();
        using var output = new StringWriter();

        await ExecClient.ExecuteAsync(
            host.Address,
            ["cmd.exe", "/D", "/C", "echo explicit-cmd"],
            null,
            output);

        StringAssert.Contains(output.ToString(), "explicit-cmd");
    }

    [TestMethod]
    public async Task WhenExecFailsThenExceptionIsReturned()
    {
        await using var host = await TestServerHost.StartAsync();
        using var output = new StringWriter();
        var executable = $"missing-{Guid.NewGuid():N}.exe";

        await ExecClient.ExecuteAsync(host.Address, [executable], null, output);

        StringAssert.Contains(output.ToString(), executable);
    }

    [TestMethod]
    public async Task WhenExecStartsThenChangedWorkingDirectoryIsUsed()
    {
        await using var host = await TestServerHost.StartAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        using var output = new StringWriter();
        await ChangeDirectoryClient.ChangeAsync(host.Address, directory);

        await ExecClient.ExecuteAsync(host.Address, ["cmd.exe", "/D", "/C", "cd"], null, output);

        StringAssert.Contains(output.ToString(), directory);
    }

    [TestMethod]
    public async Task WhenExecTimesOutThenDirectProcessStops()
    {
        await using var host = await TestServerHost.StartAsync();
        using var output = new StringWriter();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await ExecClient.ExecuteAsync(
            host.Address,
            ["ping.exe", "-t", "127.0.0.1"],
            1,
            output,
            cancellationSource.Token);

        Assert.IsFalse(cancellationSource.IsCancellationRequested);
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
        var creationTimeUtc = new DateTime(2024, 5, 6, 7, 8, 10, DateTimeKind.Utc);
        var lastWriteTimeUtc = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        File.SetCreationTimeUtc(source, creationTimeUtc);
        File.SetLastWriteTimeUtc(source, lastWriteTimeUtc);

        await PushClient.PushAsync(host.Address, source, remote);
        await PullClient.PullAsync(host.Address, remote, output);

        Assert.AreEqual("remote content", await File.ReadAllTextAsync(output));
        Assert.AreEqual(creationTimeUtc, File.GetCreationTimeUtc(output));
        Assert.AreEqual(lastWriteTimeUtc, File.GetLastWriteTimeUtc(output));
    }

    [TestMethod]
    public async Task WhenFileIsPushedThenFileMetadataIsPreserved()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source.txt");
        var remote = Path.Combine(root, "remote.txt");
        await File.WriteAllTextAsync(source, "metadata content");
        var creationTimeUtc = new DateTime(2024, 1, 2, 3, 4, 6, DateTimeKind.Utc);
        var lastWriteTimeUtc = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        File.SetCreationTimeUtc(source, creationTimeUtc);
        File.SetLastWriteTimeUtc(source, lastWriteTimeUtc);

        await PushClient.PushAsync(host.Address, source, remote);

        Assert.AreEqual(creationTimeUtc, File.GetCreationTimeUtc(remote));
        Assert.AreEqual(lastWriteTimeUtc, File.GetLastWriteTimeUtc(remote));
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
        var sourceCreationTimeUtc = new DateTime(2024, 3, 4, 5, 6, 8, DateTimeKind.Utc);
        var sourceLastWriteTimeUtc = new DateTime(2024, 4, 5, 6, 7, 8, DateTimeKind.Utc);
        Directory.SetCreationTimeUtc(source, sourceCreationTimeUtc);
        Directory.SetLastWriteTimeUtc(source, sourceLastWriteTimeUtc);

        await PushClient.PushAsync(host.Address, source, remote);
        await PullClient.PullAsync(host.Address, remote, output);

        Assert.AreEqual("nested content", await File.ReadAllTextAsync(Path.Combine(output, "nested", "content.txt")));
        Assert.AreEqual(sourceCreationTimeUtc, Directory.GetCreationTimeUtc(remote));
        Assert.AreEqual(sourceLastWriteTimeUtc, Directory.GetLastWriteTimeUtc(remote));
    }

    [TestMethod]
    public async Task WhenDirectoryIsPushedThenNoTemporaryArchiveIsCreated()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var remote = Path.Combine(root, "remote");
        Directory.CreateDirectory(source);
        await File.WriteAllBytesAsync(Path.Combine(source, "content.bin"), new byte[16 * 1024 * 1024]);
        var archiveCreated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(Path.GetTempPath(), "WinRemoteShell_Push_*.zip")
        {
            EnableRaisingEvents = true
        };
        watcher.Created += (_, _) => archiveCreated.TrySetResult();

        await PushClient.PushAsync(host.Address, source, remote);
        await Task.WhenAny(archiveCreated.Task, Task.Delay(100));

        Assert.IsFalse(archiveCreated.Task.IsCompleted);
    }

    [TestMethod]
    public async Task WhenDirectoryIsPushedWithMergeThenExistingExtraFileIsPreserved()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var remote = Path.Combine(root, "remote");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(remote);
        await File.WriteAllTextAsync(Path.Combine(source, "content.txt"), "new content");
        var extraFile = Path.Combine(remote, "extra.txt");
        await File.WriteAllTextAsync(extraFile, "existing content");

        await PushClient.PushAsync(host.Address, source, remote, PushMode.Merge);

        Assert.IsTrue(File.Exists(extraFile));
    }

    [TestMethod]
    public async Task WhenDirectoryIsPushedWithReplaceThenExistingExtraFileIsDeleted()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var remote = Path.Combine(root, "remote");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(remote);
        await File.WriteAllTextAsync(Path.Combine(source, "content.txt"), "new content");
        var extraFile = Path.Combine(remote, "extra.txt");
        await File.WriteAllTextAsync(extraFile, "existing content");

        await PushClient.PushAsync(host.Address, source, remote, PushMode.Replace);

        Assert.IsFalse(File.Exists(extraFile));
    }

    [TestMethod]
    public async Task WhenTargetExistsAndFailIfExistsIsUsedThenPushIsRejected()
    {
        await using var host = await TestServerHost.StartAsync();
        var root = Path.Combine(Path.GetTempPath(), $"WinRemoteShell_{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var remote = Path.Combine(root, "remote");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(remote);
        await File.WriteAllTextAsync(Path.Combine(source, "content.txt"), "new content");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            PushClient.PushAsync(host.Address, source, remote, PushMode.FailIfExists));

        Assert.AreEqual(HttpStatusCode.Conflict, exception.StatusCode);
    }

    [TestMethod]
    public async Task WhenShellExitsThenDirectExecRemainsAvailable()
    {
        await using var host = await TestServerHost.StartAsync();
        using var shellInput = new StringReader("echo shell-ready\nexit\n");
        using var shellOutput = new StringWriter();
        using var execOutput = new StringWriter();

        await ShellClient.RunAsync(host.Address, shellInput, shellOutput);
        await ExecClient.ExecuteAsync(host.Address, ["where.exe", "cmd.exe"], null, execOutput);

        StringAssert.Contains(execOutput.ToString(), "cmd.exe");
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

    [TestMethod]
    public async Task WhenProcessesAreListedThenCurrentProcessIsReturned()
    {
        await using var host = await TestServerHost.StartAsync();
        var currentProcessId = Environment.ProcessId;

        var response = await ProcessClient.ListAsync(host.Address, false);

        Assert.IsTrue(response.Processes.Any(process => process.Id == currentProcessId));
    }

    [TestMethod]
    public async Task WhenProcessesAreListedWithoutDetailsThenOptionalFieldsAreEmpty()
    {
        await using var host = await TestServerHost.StartAsync();

        var response = await ProcessClient.ListAsync(host.Address, false);
        var currentProcess = response.Processes.Single(process => process.Id == Environment.ProcessId);

        Assert.IsNull(currentProcess.WorkingSetBytes);
    }

    [TestMethod]
    public async Task WhenUnknownProcessIdIsKilledThenNoProcessesAreReturned()
    {
        await using var host = await TestServerHost.StartAsync();

        var response = await KillClient.KillAsync(host.Address, int.MaxValue, null, false);

        Assert.IsEmpty(response.Processes);
    }

    [TestMethod]
    public async Task WhenKillHasNoTargetThenRequestIsRejected()
    {
        await using var host = await TestServerHost.StartAsync();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            KillClient.KillAsync(host.Address, null, null, false));

        Assert.AreEqual(HttpStatusCode.BadRequest, exception.StatusCode);
    }
}