using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed record IntegrationStageResult(string Id, bool Succeeded, int ExitCode, string Message, string StandardOutput, string StandardError);

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
        if (string.IsNullOrWhiteSpace(options.Payload))
        {
            log.Error("manifest", "integration-run requires a payload directory or manifest path.");
            return 2;
        }

        var manifestPath = Directory.Exists(options.Payload)
            ? Path.Combine(Path.GetFullPath(options.Payload), IntegrationPayloadManifest.FileName)
            : Path.GetFullPath(options.Payload);
        if (!File.Exists(manifestPath))
        {
            log.Error("manifest", "Payload manifest was not found.", new { manifestPath });
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

            var imePath = Resolve(root, manifest.ImeFile);
            var install = installer.Install(imePath, "XiaoXi IME");
            installed = install.Succeeded;
            results.Add(new IntegrationStageResult("install", install.Succeeded, install.Succeeded ? 0 : 1, install.Message, "", ""));
            LogResult(log, results[^1]);
            if (!install.Succeeded)
            {
                return await CompleteAsync(13, reportPath, results, log);
            }

            var commands = new List<SystemTestCommand>
            {
                new("tsf-abi", Resolve(root, manifest.TsfAbiHostExecutable), ["abi", Resolve(root, manifest.TsfModule)]),
                new("tsf-com-activation", Resolve(root, manifest.TsfAbiHostExecutable), ["com-activation", Resolve(root, manifest.TsfModule)]),
            };
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

    private static string? VerifyPayload(string root, IntegrationPayloadManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
        {
            return $"Unsupported payload schema version: {manifest.SchemaVersion}.";
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

    private static void LogResult(StructuredConsole log, IntegrationStageResult result)
    {
        var data = new { result.ExitCode, result.StandardOutput, result.StandardError };
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
