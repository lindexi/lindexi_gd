using System.Diagnostics;
using System.Text.Json;

namespace XiaoXiIme.SandboxRunner;

internal static class Imm32Validation
{
    internal static async Task<int> RunAsync()
    {
        var staging = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".."));
        var root = Path.GetFullPath(Path.Combine(staging, ".."));
        var output = Path.Combine(root, "sandbox-output");
        Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG", Path.Combine(output, "ime-lifecycle.log"));
        var cli = Path.Combine(root, "publish", "app", "cli", "XiaoXiIme.Cli.exe");
        var payload = Path.Combine(output, "payload");
        var build = await RunAsync
        (
            "payload-build", cli, root, output,
            [
                "payload-build", "--no-build", "--staging-directory", Path.Combine(root, "publish"),
                "--dictionary-source", Path.Combine(AppContext.BaseDirectory, "data", "dictionaries"), "--output",
                payload
            ]
        );
        if (build != 0)
        {
            return build;
        }

        Environment.SetEnvironmentVariable("XIAOXIIME_VERIFY_ANCHOR", "1");
        var uiStatus = Path.Combine(output, "candidate-ui-status.txt");
        Environment.SetEnvironmentVariable("XIAOXIIME_UI_STATUS", uiStatus);
        var uiInfo = new ProcessStartInfo(Path.Combine(payload, "app", "ui", "XiaoXiIme.ImeUi.Avalonia.exe"))
        {
            UseShellExecute = false,
            WorkingDirectory = root,
        };
        uiInfo.ArgumentList.Add("XiaoXiIme_ImeHost");
        uiInfo.ArgumentList.Add(uiStatus);
        using var ui = Process.Start
            (uiInfo) ?? throw new InvalidOperationException("Could not start actual candidate UI.");
        int result;
        try
        {
            result = await RunAsync
            (
                "integration-run", Path.Combine(payload, "app", "cli", "XiaoXiIme.Cli.exe"), root, output,
                [
                    "integration-run", payload, "--confirm", "I-UNDERSTAND-THIS-MODIFIES-WINDOWS", "--skip-tsf",
                    "--report", Path.Combine(output, "integration.json")
                ]
            );
        }
        finally
        {
            if (!ui.HasExited)
            {
                var exitInfo = new ProcessStartInfo(uiInfo.FileName) { UseShellExecute = false };
                exitInfo.ArgumentList.Add("XiaoXiIme_ImeHost");
                exitInfo.ArgumentList.Add("--exit");
                using var exit = Process.Start
                    (exitInfo) ?? throw new InvalidOperationException("Could not request normal UI exit.");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await exit.WaitForExitAsync(timeout.Token);
                await ui.WaitForExitAsync(timeout.Token);
            }

            await File.WriteAllTextAsync
                (Path.Combine(output, "ui-cleanup.txt"), $"Exited={ui.HasExited}; ExitCode={ui.ExitCode}");
        }

        var evidence = Path.Combine(root, "final-evidence");
        Directory.CreateDirectory(evidence);
        var smokeResult = ProjectTermSmoke.Run(Path.Combine(evidence, "project-term"));
        foreach (var file in Directory.EnumerateFiles(output))
        {
            File.Copy(file, Path.Combine(evidence, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var file in Directory.EnumerateFiles(root, "*-sha256.txt"))
        {
            File.Copy(file, Path.Combine(evidence, Path.GetFileName(file)), overwrite: true);
        }

        File.Copy
        (
            Path.Combine(payload, "xiaoxiime-payload.json"), Path.Combine(evidence, "xiaoxiime-payload.json"),
            overwrite: true
        );
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "integration.json")));
        foreach (var stage in report.RootElement.GetProperty("results").EnumerateArray())
        {
            var id = stage.GetProperty("Id").GetString();
            await File.WriteAllTextAsync
            (
                Path.Combine(evidence, id + ".expanded.stdout.txt"),
                stage.GetProperty("StandardOutput").GetString()
            );
            await File.WriteAllTextAsync
            (
                Path.Combine(evidence, id + ".expanded.stderr.txt"),
                stage.GetProperty("StandardError").GetString()
            );
        }

        foreach (var (directory, name) in new[]
                 {
                     (Path.Combine(root, "publish"), "publish-sha256.txt"),
                     (Path.Combine(AppContext.BaseDirectory, "source-freeze"), "source-sha256.txt"),
                 })
        {
            var hashes = new List<string>();
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Order
                         (StringComparer.Ordinal))
            {
                await using var stream = File.OpenRead(file);
                var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream);
                hashes.Add($"{Convert.ToHexString(hash)}  {Path.GetRelativePath(directory, file)}");
            }

            await File.WriteAllLinesAsync(Path.Combine(evidence, name), hashes);
        }

        using var summary = new StreamWriter(Path.Combine(evidence, "evidence-summary.txt"));
        foreach (var stage in report.RootElement.GetProperty("results").EnumerateArray())
        {
            await summary.WriteLineAsync(stage.ToString());
        }

        return result != 0 ? result : smokeResult;
    }

    private static async Task<int> RunAsync
        (string stage, string executable, string root, string output, string[] arguments)
    {
        var started = DateTimeOffset.UtcNow;
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start
            (info) ?? throw new InvalidOperationException("Could not start validation process.");
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
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        await File.WriteAllTextAsync(Path.Combine(output, stage + ".stdout.txt"), await stdout);
        await File.WriteAllTextAsync(Path.Combine(output, stage + ".stderr.txt"), await stderr);
        var exitCode = timedOut ? 124 : process.ExitCode;
        await File.WriteAllTextAsync
        (
            Path.Combine(output, stage + ".execution.json"), JsonSerializer.Serialize
            (
                new
                {
                    stage, executable, arguments, started, ended = DateTimeOffset.UtcNow, exitCode, timedOut,
                }, new JsonSerializerOptions { WriteIndented = true }
            )
        );
        return exitCode;
    }
}