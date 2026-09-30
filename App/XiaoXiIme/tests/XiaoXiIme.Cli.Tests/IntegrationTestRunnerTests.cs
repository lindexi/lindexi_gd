using System.Text.Json;
using XiaoXiIme.Cli;
using XiaoXiIme.Dictionary;
using XiaoXiIme.ImeIpc;

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
    public void CreateLayoutTipUsesLanguageIdFromInstalledKeyboardLayout()
    {
        var result = WindowsImeInstaller.CreateLayoutTip("E0200804");

        Assert.Equal("0804:E0200804", result);
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
    public void VerifyDictionaryPackages_WhenPackagesAreValidThenReturnsNull()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        CompilePackage(fullPinyin, DefaultDictionaryPackageBuilder.FullPinyinInputScheme);
        CompilePackage(xiaohe, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme);

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Null(result);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenInputSchemesAreSwappedThenReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        CompilePackage(fullPinyin, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme);
        CompilePackage(xiaohe, DefaultDictionaryPackageBuilder.FullPinyinInputScheme);

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Expected input scheme", result);
        Assert.Contains(DefaultDictionaryPackageBuilder.FullPinyinInputScheme, result);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenPackageIsMissingThenReturnsError()
    {
        using var directory = new TemporaryDirectory();

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Dictionary package verification failed", result);
    }

    [Fact]
    public void ResolveDictionarySourceDirectory_WhenExplicitPathIsProvidedThenUsesThatDirectory()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = Path.Combine(directory.Path, "copied-dictionaries");
        Directory.CreateDirectory(sourceDirectory);

        var resolved = IntegrationPayloadBuilder.ResolveDictionarySourceDirectory(
            new PayloadBuildOptions { DictionarySource = sourceDirectory },
            stagingDirectory: Path.Combine(directory.Path, "staging"),
            solutionDirectory: directory.Path);

        Assert.Equal(Path.GetFullPath(sourceDirectory), resolved);
    }

    [Fact]
    public void ResolveDictionarySourceDirectory_WhenNoBuildThenWalksFromStagingDirectory()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = Path.Combine(directory.Path, "data", "dictionaries");
        Directory.CreateDirectory(sourceDirectory);
        var stagingDirectory = Path.Combine(directory.Path, "artifacts", "integration-publish");
        Directory.CreateDirectory(stagingDirectory);

        var resolved = IntegrationPayloadBuilder.ResolveDictionarySourceDirectory(
            new PayloadBuildOptions { NoBuild = true },
            stagingDirectory,
            solutionDirectory: null);

        Assert.Equal(Path.GetFullPath(sourceDirectory), resolved);
    }

    [Fact]
    public async Task BuildAsync_WhenRequiredSourcesAreMissingThenFailsBeforePublishOutputs()
    {
        using var directory = new TemporaryDirectory();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await IntegrationPayloadBuilder.BuildAsync(
            new PayloadBuildOptions
            {
                Output = Path.Combine(directory.Path, "payload"),
                DictionarySource = Path.Combine(directory.Path, "missing-sources"),
                StagingDirectory = Path.Combine(directory.Path, "staging"),
                NoBuild = true,
            },
            output,
            error);

        Assert.Equal(13, exitCode);
        Assert.Contains("dictionary source directory was not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(Path.Combine(directory.Path, "payload")));
    }

    [Fact]
    public async Task BuildAsync_WhenNativeSourcesAreValidThenCompilesPackagesBeforeMissingPublishOutputs()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = CreateNativeSources(directory.Path);
        var stagingRoot = Path.Combine(directory.Path, "artifacts", "integration-publish");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await IntegrationPayloadBuilder.BuildAsync(
            new PayloadBuildOptions
            {
                Output = Path.Combine(directory.Path, "payload"),
                DictionarySource = sourceDirectory,
                StagingDirectory = stagingRoot,
                NoBuild = true,
            },
            output,
            error);

        Assert.Equal(11, exitCode);
        Assert.Contains("Required publish outputs are missing", error.ToString());
        Assert.False(Directory.Exists(Path.Combine(directory.Path, "payload")));
        Assert.False(Directory.Exists(Path.Combine(stagingRoot, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName)));
        var dictionaryStaging = IntegrationPayloadBuilder.GetDictionaryPackageStagingDirectory(stagingRoot);
        Assert.Null(IntegrationTestRunner.VerifyHostDictionaryPackages(dictionaryStaging));
        Assert.Equal(
            "你好",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(dictionaryStaging, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName)).Query(new ImeDictionaryQuery("nihao"))).Text);
        Assert.Equal(
            "你好",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(dictionaryStaging, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath)).Query(new ImeDictionaryQuery("nihc"))).Text);
        Assert.Equal(
            "XiaoXiIme",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(dictionaryStaging, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName)).Query(new ImeDictionaryQuery("xiaoxiaimuyi"))).Text);
        Assert.Equal(
            "XiaoXiIme",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(dictionaryStaging, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath)).Query(new ImeDictionaryQuery("xnxiaimuyi"))).Text);
    }

    [Fact]
    public void CopyAndVerifyDictionaryPackages_WhenHostPublishOutputExistsThenCopiesPackagesIntoPayloadHost()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = CreateNativeSources(directory.Path);
        var dictionaryStaging = IntegrationPayloadBuilder.GetDictionaryPackageStagingDirectory(directory.Path);
        var payloadDirectory = Path.Combine(directory.Path, "payload");
        var hostDirectory = Path.Combine(payloadDirectory, "app", "host");
        Directory.CreateDirectory(hostDirectory);
        File.WriteAllText(Path.Combine(hostDirectory, "XiaoXiIme.ImeHost.exe"), "host");

        Assert.Null(IntegrationPayloadBuilder.CompileAndVerifyDictionaryPackages(sourceDirectory, dictionaryStaging));
        Assert.Null(IntegrationPayloadBuilder.CopyAndVerifyDictionaryPackages(dictionaryStaging, payloadDirectory));
        Assert.True(File.Exists(Path.Combine(hostDirectory, "XiaoXiIme.ImeHost.exe")));
        Assert.Null(IntegrationTestRunner.VerifyDictionaryPackages(payloadDirectory));
        Assert.Equal(
            "你好",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(hostDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName)).Query(new ImeDictionaryQuery("nihao"))).Text);
        Assert.Equal(
            "你好",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(hostDirectory, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath)).Query(new ImeDictionaryQuery("nihc"))).Text);
        Assert.Equal(
            "XiaoXiIme",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(hostDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName)).Query(new ImeDictionaryQuery("xiaoxiaimuyi"))).Text);
        Assert.Equal(
            "XiaoXiIme",
            Assert.Single(DictionaryPackageLoader.Load(Path.Combine(hostDirectory, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath)).Query(new ImeDictionaryQuery("xnxiaimuyi"))).Text);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenExpectedQueryIsMissingThenReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("测试", "ce shi", 100)],
            fullPinyin,
            new DictionaryPackageParameters { InputScheme = DefaultDictionaryPackageBuilder.FullPinyinInputScheme });
        CompilePackage(xiaohe, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme);

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Expected 'nihao' to return candidates", result);
        Assert.Contains(DefaultDictionaryPackageBuilder.FullPinyinInputScheme, result);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenProjectTermQueryIsMissingThenReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你好", "ni hao", 100)],
            fullPinyin,
            new DictionaryPackageParameters { InputScheme = DefaultDictionaryPackageBuilder.FullPinyinInputScheme });
        CompilePackage(xiaohe, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme);

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Expected 'xiaoxiaimuyi' to return candidates", result);
        Assert.Contains(DefaultDictionaryPackageBuilder.FullPinyinInputScheme, result);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenProjectTermTextDoesNotMatchThenReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你好", "ni hao", 100), new PhoneticDictionaryEntry("别名", "xiao xi ai mu yi", 100)],
            fullPinyin,
            new DictionaryPackageParameters { InputScheme = DefaultDictionaryPackageBuilder.FullPinyinInputScheme });
        CompilePackage(xiaohe, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme);

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Expected 'xiaoxiaimuyi' to return 'XiaoXiIme'", result);
        Assert.Contains(DefaultDictionaryPackageBuilder.FullPinyinInputScheme, result);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenXiaoheProjectTermQueryIsMissingThenReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        CompilePackage(fullPinyin, DefaultDictionaryPackageBuilder.FullPinyinInputScheme);
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你好", "ni hao", 100)],
            xiaohe,
            new DictionaryPackageParameters { InputScheme = DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme });

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Expected 'xnxiaimuyi' to return candidates", result);
        Assert.Contains(DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme, result);
    }

    [Fact]
    public void VerifyDictionaryPackages_WhenXiaoheProjectTermTextDoesNotMatchThenReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var fullPinyin = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName);
        var xiaohe = Path.Combine(directory.Path, "app", "host", DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath);
        CompilePackage(fullPinyin, DefaultDictionaryPackageBuilder.FullPinyinInputScheme);
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你好", "ni hao", 100), new PhoneticDictionaryEntry("别名", "xiao xi ai mu yi", 100)],
            xiaohe,
            new DictionaryPackageParameters { InputScheme = DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme });

        var result = IntegrationTestRunner.VerifyDictionaryPackages(directory.Path);

        Assert.Contains("Expected 'xnxiaimuyi' to return 'XiaoXiIme'", result);
        Assert.Contains(DefaultDictionaryPackageBuilder.XiaoheDoublePinyinInputScheme, result);
    }

    [Fact]
    public void ImeHostProcessManager_WhenExecutableIsMissingThenReportsFailure()
    {
        using var directory = new TemporaryDirectory();
        var missing = Path.Combine(directory.Path, "missing", "XiaoXiIme.ImeHost.exe");

        var result = new ImeHostProcessManager().Start(missing);

        Assert.False(result.Succeeded);
        Assert.Contains("IME host executable was not found", result.Message);
        Assert.Equal(Path.GetFullPath(missing), result.ExecutablePath);
        Assert.Null(result.HostStatus);
    }

    [Fact]
    public void ImeHostProcessManager_WhenHostDoesNotBecomeReadyThenReportsFailure()
    {
        var result = ImeHostProcessManager.WaitUntilReady(
            new XiaoXiImeIpcOptions($"XiaoXiIme_Missing_{Guid.NewGuid():N}"),
            TimeSpan.FromMilliseconds(200));

        Assert.False(result.Succeeded);
        Assert.Contains("IME host did not become ready", result.Message);
        Assert.Null(result.HostStatus);
    }

    [Fact]
    public void Uninstall_WhenUserDataIsRetainedThenKeepsLearnedFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "user-dictionary.json");
        File.WriteAllText(path, "{}");

        var message = UserDictionaryUninstall.Complete(purgeUserData: false, path);

        Assert.True(File.Exists(path));
        Assert.Contains("User dictionary retained", message);
        Assert.Contains(path, message);
    }

    [Fact]
    public void Uninstall_WhenUserDataIsPurgedThenDeletesLearnedFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "user-dictionary.json");
        File.WriteAllText(path, "{}");

        var message = UserDictionaryUninstall.Complete(purgeUserData: true, path);

        Assert.False(File.Exists(path));
        Assert.Contains("User dictionary purged", message);
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

    private static void CompilePackage(string packageDirectory, string inputScheme)
    {
        var entries = new List<PhoneticDictionaryEntry> { new("你好", "ni hao", 100) };
        entries.AddRange(XiaoXiImeProjectTerms.Parse());
        DictionaryPackageCompiler.Compile(
            entries,
            packageDirectory,
            new DictionaryPackageParameters { InputScheme = inputScheme });
    }

    private static string CreateNativeSources(string root)
    {
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "phonetic"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "shape"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "symbols"));
        File.WriteAllText(Path.Combine(sourceDirectory, "phonetic", "sewzc-default.phonetic.tsv"), "你好\tni hao\t100");
        XiaoXiImeProjectTerms.WriteTo(Path.Combine(sourceDirectory, XiaoXiImeProjectTerms.OutputRelativePath));
        File.WriteAllText(Path.Combine(sourceDirectory, "shape", "sewzc-moqi.shape.tsv"), "你\trb\t亻尔");
        File.WriteAllText(Path.Combine(sourceDirectory, "symbols", "sewzc-default.symbols.tsv"), "/xh\t★\t☆");
        return sourceDirectory;
    }
}
