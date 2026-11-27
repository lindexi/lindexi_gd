using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace XiaoXiIme.SandboxRunner;

internal static class Imm32Validation
{
    internal static async Task<int> RunInstallSmokeAsync(string distribution, string output, bool notepad = false)
    {
        Directory.CreateDirectory(output);
        var launcher = Path.Combine(distribution, "XiaoXiIme.exe");
        var testHost = Path.Combine(distribution, "Tests", "XiaoXiIme.IntegrationTestHost.exe");
        if (!OperatingSystem.IsWindows())
        {
            return 2;
        }

        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        var elevated = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        await File.WriteAllLinesAsync(Path.Combine(output, "sandbox-preflight.txt"),
        [
            $"Machine={Environment.MachineName}",
            $"Windows={Environment.OSVersion.Version}",
            $"User={identity.Name}",
            $"SessionId={Process.GetCurrentProcess().SessionId}",
            $"Elevated={elevated}",
        ]);
        if (!elevated)
        {
            Console.Error.WriteLine("The sandbox runner must execute in an elevated interactive Windows session to test IME installation.");
            return 2;
        }

        try
        {
            await File.WriteAllLinesAsync(Path.Combine(output, "legacy-g02-layouts.txt"), RemoveLegacyG02Layouts());
        }
        catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException)
        {
            await File.WriteAllTextAsync(Path.Combine(output, "registry-preflight-error.txt"), error.ToString());
            Console.Error.WriteLine(error.Message);
            return 2;
        }

        if (await RunStageAsync("uninstall-old", launcher, "uninstall") != 0)
        {
            return 1;
        }
        var result = 1;
        try
        {
            var installed = await RunStageAsync("install", launcher, "install");
            result = installed != 0 ? installed : notepad
                ? await NotepadKeyboardSmoke.RunAsync(output)
                : await RunStageAsync("keyboard", testHost, "--keyboard-smoke");
        }
        finally
        {
            var cleanup = await RunStageAsync("uninstall", launcher, "uninstall");
            if (result == 0)
            {
                result = cleanup;
            }
        }
        return result;

        async Task<int> RunStageAsync(string stage, string executable, string argument)
        {
            var info = new ProcessStartInfo(executable)
            {
                WorkingDirectory = distribution,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add(argument);
            info.Environment["XIAOXIIME_LIFECYCLE_LOG"] = Path.Combine(output, "ime-lifecycle.log");
            if (notepad)
            {
                info.Environment["XIAOXIIME_UI_STATUS"] = Path.Combine(output, "notepad-candidate-status.txt");
            }
            using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {stage}.");
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            process.OutputDataReceived += (_, line) => { if (line.Data is not null) stdout.WriteLine(line.Data); };
            process.ErrorDataReceived += (_, line) => { if (line.Data is not null) stderr.WriteLine(line.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var exited = await Task.Run(() => process.WaitForExit(45_000));
            if (!exited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
            // Detached UI can outlive the CLI's inherited stream handles.
            await Task.Delay(200);
            await File.WriteAllTextAsync(Path.Combine(output, stage + ".stdout.txt"), stdout.ToString());
            await File.WriteAllTextAsync(Path.Combine(output, stage + ".stderr.txt"), stderr.ToString());
            var exitCode = exited ? process.ExitCode : 124;
            await File.WriteAllTextAsync(Path.Combine(output, stage + ".exit.txt"), exitCode.ToString());
            const string logPrefix = "[InstallLog] ";
            if (executable == launcher)
            {
                var logPath = stdout.ToString().Split('\n').FirstOrDefault(line => line.StartsWith(logPrefix, StringComparison.Ordinal));
                if (logPath is null || !File.Exists(logPath[logPrefix.Length..].Trim()))
                {
                    throw new IOException($"{stage}: ProgramData installation log was not written.");
                }
                File.Copy(logPath[logPrefix.Length..].Trim(), Path.Combine(output, stage + ".programdata.log"), overwrite: true);
            }
            return exitCode;
        }
    }

    internal static async Task<int> RunAsync(string distribution, string output)
    {
        Directory.CreateDirectory(output);
        var launcher = Path.Combine(distribution, "XiaoXiIme.exe");
        var testHost = Path.Combine(distribution, "Tests", "XiaoXiIme.IntegrationTestHost.exe");
        foreach (var requiredFile in new[] { launcher, testHost })
        {
            if (!File.Exists(requiredFile))
            {
                var message = $"[Imm32Validation][LocateDistribution] MissingFile={requiredFile}";
                Console.Error.WriteLine(message);
                await File.WriteAllTextAsync(Path.Combine(output, "integration-run.stderr.txt"), message);
                return 2;
            }
        }

        var report = Path.Combine(output, "integration.json");
        File.Delete(report);
        var info = new ProcessStartInfo(launcher)
        {
            WorkingDirectory = distribution,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "integration-run", distribution, "--skip-tsf", "--report", report })
        {
            info.ArgumentList.Add(argument);
        }
        info.Environment["XIAOXIIME_LIFECYCLE_LOG"] = Path.Combine(output, "ime-lifecycle.log");
        info.Environment["XIAOXIIME_VERIFY_ANCHOR"] = "1";
        info.Environment["XIAOXIIME_UI_STATUS"] = Path.Combine(output, "candidate-ui-status.txt");

        try
        {
            if (OperatingSystem.IsWindows())
            {
                await File.WriteAllLinesAsync
                    (Path.Combine(output, "legacy-g02-layouts.txt"), RemoveLegacyG02Layouts());
            }

            using var process = Process.Start(info);
            if (process is null)
            {
                Console.Error.WriteLine($"[Imm32Validation][Start] Launcher={launcher}; Error=Process did not start");
                return 1;
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                timedOut = true;
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                await process.WaitForExitAsync();
            }

            await File.WriteAllTextAsync(Path.Combine(output, "integration-run.stdout.txt"), await stdout);
            await File.WriteAllTextAsync(Path.Combine(output, "integration-run.stderr.txt"), await stderr);
            var exitCode = timedOut ? 124 : process.ExitCode;
            if (File.Exists(report))
            {
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(report));
                foreach (var stage in document.RootElement.GetProperty("results").EnumerateArray())
                {
                    var id = stage.GetProperty("Id").GetString();
                    await File.WriteAllTextAsync(Path.Combine(output, id + ".expanded.stdout.txt"), stage.GetProperty("StandardOutput").GetString());
                    await File.WriteAllTextAsync(Path.Combine(output, id + ".expanded.stderr.txt"), stage.GetProperty("StandardError").GetString());
                }
            }
            else if (exitCode == 0)
            {
                Console.Error.WriteLine($"[Imm32Validation][Report] Path={report}; Error=Integration report was not generated");
                return 1;
            }

            return exitCode != 0 ? exitCode : ProjectTermSmoke.Run(Path.Combine(output, "project-term"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or JsonException)
        {
            Console.Error.WriteLine($"[Imm32Validation][Run] Launcher={launcher}; Report={report}; Error={exception}");
            return 1;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static List<string> RemoveLegacyG02Layouts()
    {
        using var layouts = Registry.LocalMachine.OpenSubKey
            (@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts", writable: true);
        var removed = new List<string>();
        if (layouts is null)
        {
            return removed;
        }

        foreach (var id in layouts.GetSubKeyNames())
        {
            string? file;
            using (var layout = layouts.OpenSubKey(id))
            {
                if (!string.Equals(layout?.GetValue("Layout Text") as string, "XiaoXi IME", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                file = layout?.GetValue("Ime File") as string;
            }

            if (file is not null && Regex.IsMatch
                (file, @"\AXIAOXIG02_[0-9a-f]{32}\.IME\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                layouts.DeleteSubKeyTree(id, throwOnMissingSubKey: false);
                removed.Add($"{id}: {file}");
            }
        }

        return removed;
    }
}
