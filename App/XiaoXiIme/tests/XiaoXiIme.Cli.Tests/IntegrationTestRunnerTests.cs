using System.Text.Json;
using XiaoXiIme.Cli;

namespace XiaoXiIme.Cli.Tests;

public sealed class IntegrationTestRunnerTests
{
    [Fact]
    public void StructuredConsoleWritesChineseCharactersWithoutEscaping()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var console = new StructuredConsole(output, error);

        console.Information("构建", "构建成功", new { 详情 = "中文日志" });

        var json = output.ToString();
        Assert.Contains("构建成功", json);
        Assert.Contains("中文日志", json);
        Assert.DoesNotContain("\\u", json, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("构建成功", document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void ResolveManifestPathFindsManifestAboveExecutableDirectory()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = directory.CreateManifest();
        var executableDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, "app", "cli")).FullName;

        var result = IntegrationTestRunner.ResolveManifestPath(null, executableDirectory);

        Assert.Equal(manifestPath, result);
    }

    [Fact]
    public void ResolveManifestPathFallsBackToCurrentDirectoryHierarchy()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = directory.CreateManifest();
        var currentDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, "work", "child")).FullName;
        var unrelatedDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"XiaoXiIme.Cli.Tests-{Guid.NewGuid():N}")).FullName;
        try
        {
            var result = IntegrationTestRunner.ResolveManifestPath(null, unrelatedDirectory, currentDirectory);

            Assert.Equal(manifestPath, result);
        }
        finally
        {
            Directory.Delete(unrelatedDirectory, true);
        }
    }

    [Fact]
    public void ResolveManifestPathUsesExplicitManifestPath()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = directory.CreateManifest();

        var result = IntegrationTestRunner.ResolveManifestPath(manifestPath, Path.GetTempPath());

        Assert.Equal(manifestPath, result);
    }

    [Fact]
    public void ResolveManifestPathReturnsNullWhenManifestDoesNotExist()
    {
        using var directory = new TemporaryDirectory();

        var result = IntegrationTestRunner.ResolveManifestPath(null, directory.Path);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0, true, 0)]
    [InlineData(0, false, 15)]
    [InlineData(14, true, 14)]
    [InlineData(14, false, 14)]
    public void GetFinalExitCodePreservesStageFailureAndReportsCleanupFailure(int exitCode, bool cleanupSucceeded, int expected)
    {
        var result = IntegrationTestRunner.GetFinalExitCode(exitCode, cleanupSucceeded);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void InstallationFailureCanIdentifyExistingFileConflict()
    {
        var result = ImeInstallationResult.Failure(
            "Existing file differs.",
            installedPath: @"C:\Windows\System32\XiaoXiIme.ime",
            failureKind: ImeInstallationFailureKind.ExistingFileConflict);

        Assert.False(result.Succeeded);
        Assert.Equal(ImeInstallationFailureKind.ExistingFileConflict, result.FailureKind);
        Assert.Equal(@"C:\Windows\System32\XiaoXiIme.ime", result.InstalledPath);
    }

    [Fact]
    public void UninstallationResultCanRequireRestartForPendingFileDeletion()
    {
        var pendingPath = @"C:\Windows\System32\XiaoXiIme.ime";
        var result = new ImeUninstallationResult(
            false,
            [],
            "Restart required.",
            RebootRequired: true,
            PendingDeletePaths: [pendingPath]);

        Assert.False(result.Succeeded);
        Assert.True(result.RebootRequired);
        Assert.Equal([pendingPath], result.PendingDeletePaths);
    }

    [Fact]
    public void UninstallationResultCanReportRetiredFileWithoutRequiringRestart()
    {
        var retiredPath = @"C:\Windows\System32\XiaoXiIme.retired-20260726T132255Z-0123456789abcdef0123456789abcdef.ime";
        var result = new ImeUninstallationResult(
            true,
            [],
            "Moved the loaded IME aside.",
            RetiredFilePaths: [retiredPath]);

        Assert.True(result.Succeeded);
        Assert.False(result.RebootRequired);
        Assert.Equal([retiredPath], result.RetiredFilePaths);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"XiaoXiIme.Cli.Tests-{Guid.NewGuid():N}")).FullName;
        }

        public string Path { get; }

        public string CreateManifest()
        {
            var manifestPath = System.IO.Path.Combine(Path, IntegrationPayloadManifest.FileName);
            File.WriteAllText(manifestPath, "{}");
            return manifestPath;
        }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
