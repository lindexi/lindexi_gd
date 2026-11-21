using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace XiaoXiIme.SandboxRunner;

internal static class Imm32Validation
{
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
