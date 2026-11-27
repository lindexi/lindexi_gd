using System.Text.Json;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.Cli.Tests;

public sealed class IntegrationTestRunnerTests
{
    [Fact]
    public void StructuredConsoleWritesChineseCharactersWithoutEscaping()
    {
        using var output = new StringWriter();
        var console = new StructuredConsole(output, TextWriter.Null);
        console.Information("构建", "构建成功", new { 详情 = "中文日志" });
        var json = output.ToString();
        Assert.DoesNotContain("\\u", json, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("构建成功", document.RootElement.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData(0, true, 0)]
    [InlineData(0, false, 15)]
    [InlineData(14, true, 14)]
    [InlineData(14, false, 14)]
    public void GetFinalExitCodePreservesStageFailureAndReportsCleanupFailure(int exitCode, bool cleanupSucceeded, int expected)
    {
        Assert.Equal(expected, IntegrationTestRunner.GetFinalExitCode(exitCode, cleanupSucceeded));
    }

    [Fact]
    public void DistributionResolvesNativeComponentsByKindAndArchitecture()
    {
        var root = Path.Combine(Path.GetTempPath(), "XiaoXiIme distribution");
        var distribution = Distribution.Open(root);
        Assert.Equal(Path.Combine(root, "native", "ime", "win-x86", "XiaoXiIme.ime"), distribution.Ime("win-x86"));
    }

    [Fact]
    public void DistributionReportsMissingLauncherEvenIfLegacyManifestExists()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "xiaoxiime-payload.json"), "{}");
        Assert.Equal(Path.Combine(directory.Path, "XiaoXiIme.exe"), Distribution.Open(directory.Path).MissingInstallFile());
    }

    [Fact]
    public void CreateLayoutTipUsesLanguageIdFromInstalledKeyboardLayout()
    {
        Assert.Equal("0804:E0200804", WindowsImeInstaller.CreateLayoutTip("E0200804"));
    }

    [Fact]
    public void InstallationFailureCanIdentifyExistingFileConflict()
    {
        var result = ImeInstallationResult.Failure("Existing file differs.", failureKind: ImeInstallationFailureKind.ExistingFileConflict);
        Assert.Equal(ImeInstallationFailureKind.ExistingFileConflict, result.FailureKind);
    }

    [Fact]
    public void UninstallationResultCanRequireRestartForPendingFileDeletion()
    {
        var result = new ImeUninstallationResult(false, [], "Restart required.", RebootRequired: true);
        Assert.True(result.RebootRequired);
    }

    [Fact]
    public void UninstallationResultCanReportRetiredFileWithoutRequiringRestart()
    {
        const string retiredPath = @"C:\Windows\System32\XiaoXiIme.retired-20260726T132255Z-0123456789abcdef0123456789abcdef.ime";
        var result = new ImeUninstallationResult(true, [], "Moved the loaded IME aside.", RetiredFilePaths: [retiredPath]);
        Assert.Equal([retiredPath], result.RetiredFilePaths);
    }

    [Fact]
    public void ImeHostProcessManager_WhenExecutableIsMissingThenReportsFailure()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new ImeHostProcessManager();
        var result = manager.Start(Path.Combine(directory.Path, "XiaoXiIme.exe"));
        Assert.False(result.Succeeded);
        Assert.Contains("IME host executable was not found", result.Message);
    }

    [Fact]
    public void ImeHostProcessManager_WhenHostDoesNotBecomeReadyThenReportsFailure()
    {
        var result = ImeHostProcessManager.WaitUntilReady(new XiaoXiImeIpcOptions($"XiaoXiIme_Missing_{Guid.NewGuid():N}"), TimeSpan.FromMilliseconds(200));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void ImeHostProcessManager_WhenReadinessIsCanceledThenReportsCancellation()
    {
        var result = ImeHostProcessManager.WaitUntilReady(XiaoXiImeIpcOptions.Default,
            cancellationToken: new CancellationToken(canceled: true));

        Assert.Contains("startup was canceled", result.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UninstallHonorsUserDataRetention(bool purge)
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "user-dictionary.json");
        File.WriteAllText(path, "{}");
        UserDictionaryUninstall.Complete(purge, path);
        Assert.Equal(!purge, File.Exists(path));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("XiaoXiIme.Cli.Tests-").FullName;

        public void Dispose() => Directory.Delete(Path, true);
    }
}
