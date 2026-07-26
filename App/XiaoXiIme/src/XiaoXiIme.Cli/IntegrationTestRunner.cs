using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed record IntegrationStageResult(
    string Id,
    bool Succeeded,
    int ExitCode,
    string Message,
    string StandardOutput,
    string StandardError,
    object? Data = null);

internal static class IntegrationTestRunner
{
    public static async Task<int> RunAsync(
        IntegrationRunOptions options,
        TextWriter output,
        TextWriter error,
        Func<IImeInstaller> createInstaller)
    {
        var log = new StructuredConsole(output, error);
        if (!string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal))
        {
            log.Error("safety", $"Execution blocked. Pass --confirm {SystemTestRunner.VmConfirmation} only inside a disposable VM.");
            return 6;
        }
        var manifestPath = ResolveManifestPath(options.Payload, AppContext.BaseDirectory, Environment.CurrentDirectory);
        if (manifestPath is null)
        {
            log.Error("manifest", "Payload manifest was not found. Pass a payload directory or manifest path, or run the CLI from inside a payload directory.");
            return 4;
        }

        var root = Path.GetDirectoryName(manifestPath)!;
        var manifest = IntegrationPayloadManifest.Load(manifestPath);
        var verificationError = VerifyPayload(root, manifest);
        if (verificationError is not null)
        {
            log.Error("manifest", verificationError);
            return 8;
        }

        var reportPath = Path.GetFullPath(options.Report ?? Path.Combine(root, "results", $"integration-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json"));
        var results = new List<IntegrationStageResult>();
        var installer = createInstaller();
        var installed = false;
        try
        {
            var uninstall = installer.UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
            results.Add(new IntegrationStageResult("uninstall-old", uninstall.Succeeded, uninstall.Succeeded ? 0 : 1, uninstall.Message, "", ""));
            LogResult(log, results[^1]);
            if (!uninstall.Succeeded)
            {
                return await CompleteAsync(12, reportPath, results, log);
            }

            if (!manifest.NativeComponents.TryGetValue("x64", out var x64Components))
            {
                results.Add(new IntegrationStageResult("install", false, 1, "The payload does not contain x64 native components.", "", ""));
                LogResult(log, results[^1]);
                return await CompleteAsync(13, reportPath, results, log);
            }

            var imePath = Resolve(root, x64Components.ImeFile);
            var preInstallDiagnostics = ImeInstallationDiagnostics.Collect(imePath);
            results.Add(new IntegrationStageResult(
                "diagnostics-pre-install",
                true,
                0,
                preInstallDiagnostics.Findings.Count == 0
                    ? "IME installation preflight diagnostics completed without findings."
                    : $"IME installation preflight diagnostics completed with {preInstallDiagnostics.Findings.Count} finding(s).",
                "",
                "",
                preInstallDiagnostics));
            LogResult(log, results[^1]);

            var nativeLoadProbe = await RunNativeImeLoadProbeAsync(imePath);
            results.Add(nativeLoadProbe);
            LogResult(log, results[^1]);

            var install = installer.Install(imePath, "XiaoXi IME");
            installed = install.Succeeded;
            var installMessage = $"{install.Message} This stage registers the x64 IME only; x86 registration remains a separate VM validation requirement.";
            results.Add(new IntegrationStageResult(
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
                }));
            LogResult(log, results[^1]);
            if (!install.Succeeded)
            {
                var postFailureDiagnostics = ImeInstallationDiagnostics.Collect(imePath);
                results.Add(new IntegrationStageResult(
                    "diagnostics-post-install-failure",
                    true,
                    0,
                    "IME installation state captured immediately after ImmInstallIME failed.",
                    "",
                    "",
                    postFailureDiagnostics));
                LogResult(log, results[^1]);

                var variantResults = WindowsImeInstallationVariantProbe.Run(imePath, "XiaoXi IME Probe");
                var successfulVariants = variantResults.Where(result => result.InstallSucceeded).Select(result => result.Id).ToArray();
                results.Add(new IntegrationStageResult(
                    "imm-install-variant-probe",
                    true,
                    0,
                    successfulVariants.Length == 0
                        ? "All isolated ImmInstallIME path and filename variants failed."
                        : $"ImmInstallIME succeeded for variant(s): {string.Join(", ", successfulVariants)}.",
                    "",
                    "",
                    variantResults));
                LogResult(log, results[^1]);
                return await CompleteAsync(13, reportPath, results, log);
            }

            var commands = new List<SystemTestCommand>();
            foreach (var (architecture, components) in manifest.NativeComponents.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                commands.Add(new SystemTestCommand($"tsf-abi-{architecture}", Resolve(root, components.TsfAbiHostExecutable), ["abi", Resolve(root, components.TsfModule)]));
                commands.Add(new SystemTestCommand($"tsf-com-activation-{architecture}", Resolve(root, components.TsfAbiHostExecutable), ["com-activation", Resolve(root, components.TsfModule)]));
            }
            foreach (var assembly in manifest.TestAssemblies)
            {
                commands.Add(new SystemTestCommand("integration-tests", "dotnet", ["vstest", Resolve(root, assembly), "--logger:Console;Verbosity=normal"]));
            }

            foreach (var command in commands)
            {
                var result = await RunCommandAsync(command);
                results.Add(result);
                LogResult(log, result);
                if (!result.Succeeded)
                {
                    return await CompleteAsync(14, reportPath, results, log);
                }
            }

            return await CompleteAsync(0, reportPath, results, log);
        }
        finally
        {
            if (installed && !options.KeepInstalled)
            {
                var cleanup = installer.UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
                results.Add(new IntegrationStageResult("cleanup", cleanup.Succeeded, cleanup.Succeeded ? 0 : 1, cleanup.Message, "", ""));
                LogResult(log, results[^1]);
                await WriteReportAsync(reportPath, results);
            }
        }
    }

    internal static string? ResolveManifestPath(string? payload, params string[] searchStartDirectories)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            var explicitPath = Directory.Exists(payload)
                ? Path.Combine(Path.GetFullPath(payload), IntegrationPayloadManifest.FileName)
                : Path.GetFullPath(payload);
            return File.Exists(explicitPath) ? explicitPath : null;
        }

        foreach (var searchStartDirectory in searchStartDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var directory = new DirectoryInfo(Path.GetFullPath(searchStartDirectory));
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, IntegrationPayloadManifest.FileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string? VerifyPayload(string root, IntegrationPayloadManifest manifest)
    {
        if (manifest.SchemaVersion != 2)
        {
            return $"Unsupported payload schema version: {manifest.SchemaVersion}.";
        }
        if (!manifest.NativeComponents.ContainsKey("x86") || !manifest.NativeComponents.ContainsKey("x64"))
        {
            return "Payload must contain both x86 and x64 native components.";
        }
        foreach (var file in manifest.Files)
        {
            var path = Resolve(root, file.Path);
            if (!File.Exists(path))
            {
                return $"Payload file is missing: {file.Path}.";
            }
            using var stream = File.OpenRead(path);
            if (stream.Length != file.Length || !string.Equals(Convert.ToHexString(SHA256.HashData(stream)), file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return $"Payload file verification failed: {file.Path}.";
            }
        }
        return null;
    }

    private static async Task<IntegrationStageResult> RunCommandAsync(SystemTestCommand command)
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
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start {command.FileName}.");
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new IntegrationStageResult(command.Id, process.ExitCode == 0, process.ExitCode, process.ExitCode == 0 ? "Stage passed." : "Stage failed.", standardOutput, standardError);
    }

    private static async Task<IntegrationStageResult> RunNativeImeLoadProbeAsync(string imePath)
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            return new IntegrationStageResult("native-ime-load-probe", false, 1, "Unable to resolve the current CLI executable path.", "", "");
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
            return new IntegrationStageResult("native-ime-load-probe", false, 1, "Unable to start the isolated native IME loader probe.", "", "");
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
            return new IntegrationStageResult(
                "native-ime-load-probe",
                false,
                1460,
                "The isolated native IME loader probe timed out after 30 seconds.",
                await standardOutputTask,
                await standardErrorTask);
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        return new IntegrationStageResult(
            "native-ime-load-probe",
            process.ExitCode == 0,
            process.ExitCode,
            process.ExitCode == 0
                ? "The IME loaded in an isolated process and all required exports resolved."
                : "The isolated process could not load the IME or resolve all required exports.",
            standardOutput,
            standardError);
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

    private static async Task<int> CompleteAsync(int exitCode, string reportPath, IReadOnlyList<IntegrationStageResult> results, StructuredConsole log)
    {
        await WriteReportAsync(reportPath, results);
        log.Information("report", "Integration report written.", new { reportPath, exitCode });
        return exitCode;
    }

    private static async Task WriteReportAsync(string reportPath, IReadOnlyList<IntegrationStageResult> results)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
        {
            createdAtUtc = DateTimeOffset.UtcNow,
            machine = Environment.MachineName,
            operatingSystem = Environment.OSVersion.VersionString,
            results,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Resolve(string root, string relativePath) => Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
