using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli;

internal static class IntegrationTestRunner
{
    public static async Task<int> RunAsync
    (
        IntegrationRunOptions options,
        TextWriter output,
        TextWriter error,
        Func<IImeInstaller> createInstaller
    )
    {
        var log = new StructuredConsole(output, error);
        var distribution = Distribution.Open(options.Payload);
        var root = distribution.Root;
        var runtimes = new[] { "win-x86", "win-x64" };
        var diagnostics = new List<SystemTestCommand>();
        if (!options.SkipTsf)
        {
            foreach (var rid in runtimes)
            {
                diagnostics.Add(new SystemTestCommand($"tsf-abi-{rid}", distribution.TsfProbe(rid), ["abi", distribution.Tsf(rid)]));
                diagnostics.Add(new SystemTestCommand($"tsf-com-activation-{rid}", distribution.TsfProbe(rid), ["com-activation", distribution.Tsf(rid)]));
            }
        }

        if (File.Exists(distribution.TestHost))
        {
            diagnostics.Add(new SystemTestCommand("integration-tests", distribution.TestHost, []));
        }

        var missing = distribution.MissingInstallFile()
            ?? diagnostics.SelectMany(command => command.Arguments.Skip(1).Prepend(command.FileName))
                .FirstOrDefault(path => !File.Exists(path));
        if (missing is not null)
        {
            log.Error("distribution", $"MissingFile={missing}");
            return 4;
        }

        var reportPath = Path.GetFullPath
            (options.Report ?? Path.Combine(root, "results", $"integration-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json"));
        var results = new List<IntegrationStageResult>();
        var installer = createInstaller();
        var installed = false;
        try
        {
            if (!ProductUiShutdown.Stop(error))
            {
                return 16;
            }

            var uninstall = installer.UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
            results.Add
            (
                new IntegrationStageResult
                (
                    "uninstall-old",
                    uninstall.Succeeded,
                    uninstall.Succeeded ? 0 : 1,
                    uninstall.Message,
                    "",
                    "",
                    new
                    {
                        uninstall.RebootRequired,
                        PendingDeletePaths = uninstall.PendingDeletePaths ?? [],
                        RetiredFilePaths = uninstall.RetiredFilePaths ?? [],
                    }
                )
            );
            LogResult(log, results[^1]);
            if (!uninstall.Succeeded)
            {
                return await CompleteAsync(12, reportPath, results, log, installer, installed, keepInstalled: false);
            }

            if (!File.Exists(distribution.Ime("win-x64")) || !File.Exists(distribution.Ime("win-x86")))
            {
                results.Add
                (
                    new IntegrationStageResult
                    (
                        "install", false, 1, "The payload does not contain both x64 and x86 native components.", "", ""
                    )
                );
                LogResult(log, results[^1]);
                return await CompleteAsync(13, reportPath, results, log, installer, installed, keepInstalled: false);
            }

            var imePath = distribution.Ime("win-x64");
            var x86ImePath = distribution.Ime("win-x86");
            var preInstallDiagnostics = ImeInstallationDiagnostics.Collect(imePath);
            results.Add
            (
                new IntegrationStageResult
                (
                    "diagnostics-pre-install",
                    true,
                    0,
                    preInstallDiagnostics.Findings.Count == 0
                        ? "IME installation preflight diagnostics completed without findings."
                        : $"IME installation preflight diagnostics completed with {preInstallDiagnostics.Findings.Count} finding(s).",
                    "",
                    "",
                    preInstallDiagnostics
                )
            );
            LogResult(log, results[^1]);

            var nativeLoadProbe = await RunNativeImeLoadProbeAsync(imePath);
            results.Add(nativeLoadProbe);
            LogResult(log, results[^1]);

            var install = installer.InstallPair(imePath, x86ImePath, "XiaoXi IME");
            installed = install.Succeeded;
            var installMessage = install.Message;
            results.Add
            (
                new IntegrationStageResult
                (
                    "install-x64",
                    install.Succeeded,
                    install.Succeeded ? 0 : 1,
                    installMessage,
                    "",
                    "",
                    new
                    {
                        imePath,
                        install.Win32ErrorCode,
                        install.SourcePath,
                        install.InstalledPath,
                        install.CopiedToSystemDirectory,
                        install.RollbackSucceeded,
                        install.RollbackError,
                        install.FailureKind,
                    }
                )
            );
            LogResult(log, results[^1]);
            if (!install.Succeeded)
            {
                var postFailureDiagnostics = ImeInstallationDiagnostics.Collect(imePath);
                results.Add
                (
                    new IntegrationStageResult
                    (
                        "diagnostics-post-install-failure",
                        true,
                        0,
                        "IME installation state captured immediately after ImmInstallIME failed.",
                        "",
                        "",
                        postFailureDiagnostics
                    )
                );
                LogResult(log, results[^1]);

                if (install.FailureKind == ImeInstallationFailureKind.ImmInstallImeFailure)
                {
                    var variantResults = WindowsImeInstallationVariantProbe.Run(imePath, "XiaoXi IME Probe");
                    var successfulVariants = variantResults.Where(result => result.InstallSucceeded).Select
                        (result => result.Id).ToArray();
                    results.Add
                    (
                        new IntegrationStageResult
                        (
                            "imm-install-variant-probe",
                            true,
                            0,
                            successfulVariants.Length == 0
                                ? "All isolated ImmInstallIME path and filename variants failed."
                                : $"ImmInstallIME succeeded for variant(s): {string.Join(", ", successfulVariants)}.",
                            "",
                            "",
                            variantResults
                        )
                    );
                    LogResult(log, results[^1]);
                }

                return await CompleteAsync(13, reportPath, results, log, installer, installed, keepInstalled: false);
            }

            using var hostProcess = new ImeHostProcessManager();
            var hostStart = hostProcess.Start(distribution.Launcher);
            results.Add
            (
                new IntegrationStageResult
                (
                    "start-ime-host",
                    hostStart.Succeeded,
                    hostStart.Succeeded ? 0 : 1,
                    hostStart.Message,
                    hostStart.StandardOutput,
                    hostStart.StandardError,
                    new
                    {
                        hostStart.ExecutablePath,
                        hostStart.ProcessId,
                        hostStart.ChildExitCode,
                        hostStart.HostStatus?.IsRunning,
                        hostStart.HostStatus?.IsUsingFallbackDictionary,
                        hostStart.HostStatus?.DictionaryInputScheme,
                        hostStart.HostStatus?.DictionaryLoadError,
                    }
                )
            );
            LogResult(log, results[^1]);
            if (!hostStart.Succeeded)
            {
                return await CompleteAsync(16, reportPath, results, log, installer, installed, keepInstalled: false);
            }

            foreach (var command in diagnostics)
            {
                var result = await RunCommandAsync(command);
                results.Add(result);
                LogResult(log, result);
                if (!result.Succeeded)
                {
                    return await CompleteAsync
                        (14, reportPath, results, log, installer, installed, keepInstalled: false);
                }
            }

            return await CompleteAsync(0, reportPath, results, log, installer, installed, keepInstalled: false);
        }
        catch
        {
            ProductUiShutdown.Stop(error);
            if (installed)
            {
                installer.UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
            }

            throw;
        }
    }

    internal static string? VerifyHostDictionaryPackages(string hostDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostDirectory);

        var expectedSchemes = PhoneticSchemeCatalog.All.Where(item => item.ProductScheme.HasValue).Select
            (item => item.SchemeId).ToArray();
        var packages = expectedSchemes.Select
                (scheme => Path.Combine(hostDirectory, DictionaryPackageLocations.GetRelativePackageDirectory(scheme)))
            .ToArray();
        for (var index = 0; index < packages.Length; index++)
        {
            var packageDirectory = packages[index];
            var expectedScheme = expectedSchemes[index];
            try
            {
                var dictionary = DictionaryPackageLoader.Load(packageDirectory);
                if (!string.Equals(dictionary.InputScheme, expectedScheme, StringComparison.Ordinal))
                {
                    return
                        $"Dictionary package verification failed: {packageDirectory}. Expected input scheme '{expectedScheme}' but found '{dictionary.InputScheme}'.";
                }

                var probes = string.Equals
                    (expectedScheme, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, StringComparison.Ordinal)
                    ? new[] { ("nihc", "你好"), ("xnxiaimuyi", "XiaoXiIme") }
                    : expectedScheme == DictionaryPackageLocations.FullPinyinInputScheme
                        ? new[] { ("nihao", "你好"), ("xiaoxiaimuyi", "XiaoXiIme"), ("ni", "你") }
                        : new[]
                        {
                            ("ni" + "h" + PhoneticSchemeCatalog.All.Single(item => item.SchemeId == expectedScheme).Layout!.Finals["ao"],
                                "你好")
                        };
                foreach (var (probe, expectedText) in probes)
                {
                    var candidates = dictionary.Query(new ImeDictionaryQuery(probe));
                    if (candidates.Count == 0)
                    {
                        return
                            $"Dictionary package verification failed: {packageDirectory}. Expected '{probe}' to return candidates for input scheme '{expectedScheme}'.";
                    }

                    if (!string.Equals(candidates[0].Text, expectedText, StringComparison.Ordinal))
                    {
                        return
                            $"Dictionary package verification failed: {packageDirectory}. Expected '{probe}' to return '{expectedText}' for input scheme '{expectedScheme}', but found '{candidates[0].Text}'.";
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                                  or ArgumentException or DictionaryPackageException)
            {
                return $"Dictionary package verification failed: {packageDirectory}. {exception.Message}";
            }
        }

        return null;
    }

    private static async Task<IntegrationStageResult> RunCommandAsync(SystemTestCommand command)
    {
        try
        {
            var startInfo = new ProcessStartInfo(command.FileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var argument in command.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start
                (startInfo) ?? throw new InvalidOperationException($"Unable to start {command.FileName}.");
            var standardOutput = await process.StandardOutput.ReadToEndAsync();
            var standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return new IntegrationStageResult
            (
                command.Id, process.ExitCode == 0, process.ExitCode,
                process.ExitCode == 0 ? "Stage passed." : "Stage failed.", standardOutput, standardError
            );
        }
        catch (Exception exception) when
            (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new IntegrationStageResult
            (
                command.Id, false, 1, $"Unable to start stage executable '{command.FileName}'.", "",
                exception.ToString()
            );
        }
    }

    private static async Task<IntegrationStageResult> RunNativeImeLoadProbeAsync(string imePath)
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            return new IntegrationStageResult
                ("native-ime-load-probe", false, 1, "Unable to resolve the current CLI executable path.", "", "");
        }

        var startInfo = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("native-ime-load-probe");
        startInfo.ArgumentList.Add(imePath);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return new IntegrationStageResult
                ("native-ime-load-probe", false, 1, "Unable to start the isolated native IME loader probe.", "", "");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return new IntegrationStageResult
            (
                "native-ime-load-probe",
                false,
                1460,
                "The isolated native IME loader probe timed out after 30 seconds.",
                await standardOutputTask,
                await standardErrorTask
            );
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        return new IntegrationStageResult
        (
            "native-ime-load-probe",
            process.ExitCode == 0,
            process.ExitCode,
            process.ExitCode == 0
                ? "The IME loaded in an isolated process and all required exports resolved."
                : "The isolated process could not load the IME or resolve all required exports.",
            standardOutput,
            standardError
        );
    }

    private static void LogResult(StructuredConsole log, IntegrationStageResult result)
    {
        var data = new { result.ExitCode, result.StandardOutput, result.StandardError, result.Data };
        if (result.Succeeded)
        {
            log.Information(result.Id, result.Message, data);
        }
        else
        {
            log.Error(result.Id, result.Message, data);
        }
    }

    internal static int GetFinalExitCode(int exitCode, bool cleanupSucceeded)
        => exitCode == 0 && !cleanupSucceeded ? 15 : exitCode;

    private static async Task<int> CompleteAsync
    (
        int exitCode,
        string reportPath,
        List<IntegrationStageResult> results,
        StructuredConsole log,
        IImeInstaller installer,
        bool installed,
        bool keepInstalled
    )
    {
        using var shutdownErrors = new StringWriter();
        var cleanupSucceeded = ProductUiShutdown.Stop(shutdownErrors);
        if (!cleanupSucceeded)
        {
            log.Error("cleanup", shutdownErrors.ToString());
        }
        if (installed && !keepInstalled)
        {
            var cleanup = installer.UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
            cleanupSucceeded &= cleanup.Succeeded;
            results.Add
            (
                new IntegrationStageResult
                (
                    "cleanup",
                    cleanup.Succeeded,
                    cleanup.Succeeded ? 0 : 1,
                    cleanup.Message,
                    "",
                    "",
                    new
                    {
                        cleanup.RebootRequired,
                        PendingDeletePaths = cleanup.PendingDeletePaths ?? [],
                        RetiredFilePaths = cleanup.RetiredFilePaths ?? [],
                    }
                )
            );
            LogResult(log, results[^1]);
        }

        var finalExitCode = GetFinalExitCode(exitCode, cleanupSucceeded);
        await WriteReportAsync(reportPath, results);
        log.Information("report", "Integration report written.", new { reportPath, exitCode = finalExitCode });
        return finalExitCode;
    }

    private static async Task WriteReportAsync(string reportPath, IReadOnlyList<IntegrationStageResult> results)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync
        (
            reportPath, JsonSerializer.Serialize
            (
                new
                {
                    createdAtUtc = DateTimeOffset.UtcNow,
                    machine = Environment.MachineName,
                    operatingSystem = Environment.OSVersion.VersionString,
                    results,
                }, new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }

    private static string Resolve(string root, string relativePath) => Path.GetFullPath
        (Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}