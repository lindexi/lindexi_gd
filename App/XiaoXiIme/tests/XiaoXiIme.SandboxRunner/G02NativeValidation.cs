using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;

namespace XiaoXiIme.SandboxRunner;

internal static class G02NativeValidation
{
    internal static async Task<int> RunAsync(string distribution, string evidence)
    {
        var root = distribution;
        Directory.CreateDirectory(evidence);
        var records = new List<object>();
        records.Add(new { kind = "batch", root, utc = DateTimeOffset.UtcNow });
        foreach (var name in new[] { "XiaoXiIme.IntegrationTestHost", "XiaoXiIme.ImeUi.Avalonia", "XiaoXiIme.ImeHost" })
        foreach (var stale in Process.GetProcessesByName(name))
        {
            using (stale)
            {
                stale.Kill(true);
                await stale.WaitForExitAsync();
                records.Add(new { kind = "retired-process", name, stale.Id });
            }
        }

        nint layout = 0;
        Process? ui = null;
        var installed = Path.Combine(Environment.SystemDirectory, "XiaoXiIme.ime");
        Environment.SetEnvironmentVariable("XIAOXIIME_VALIDATION_IME_FILE", Path.GetFileName(installed));
        var result = 1;
        try
        {
            if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            {
                throw new PlatformNotSupportedException("This validation requires a Windows x64 process.");
            }

            Environment.SetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG", Path.Combine(evidence, "ime-lifecycle.log"));
            Environment.SetEnvironmentVariable("XIAOXIIME_VERIFY_ANCHOR", "1");
            Environment.SetEnvironmentVariable
                ("XIAOXIIME_UI_STATUS", Path.Combine(evidence, "candidate-ui-status.txt"));
            var publish = distribution;
            var source = Path.Combine(publish, "native", "ime", "win-x64", "XiaoXiIme.ime");
            var nativeDll = Path.Combine(publish, "native", "ime", "win-x64", "XiaoXiIme.ImeModule.dll");
            if (Hash(nativeDll) != Hash(source))
            {
                throw new InvalidOperationException("Published native DLL and IME hashes differ.");
            }

            records.Add
            (
                new
                {
                    kind = "native-copy-chain", nativeDll, nativeSha256 = Hash(nativeDll), source,
                    sourceSha256 = Hash(source)
                }
            );
            foreach (var file in Directory.EnumerateFiles(publish, "*", SearchOption.AllDirectories))
            {
                records.Add(new { kind = "payload", path = file, sha256 = Hash(file) });
            }

            records.Add
            (
                new
                {
                    kind = "runner", path = Environment.ProcessPath, pid = Environment.ProcessId,
                    sha256 = Hash(Environment.ProcessPath!),
                    dllSha256 = Hash(typeof(G02NativeValidation).Assembly.Location)
                }
            );
            if (File.Exists(installed) && Hash(source) != Hash(installed))
            {
                var retired = installed + ".g02-retired-" + Guid.NewGuid().ToString("N");
                File.Move(installed, retired);
                records.Add(new { kind = "retired-ime", path = retired, sha256 = Hash(retired) });
                MoveFileEx(retired, null, 4);
            }

            if (!File.Exists(installed))
            {
                File.Copy(source, installed);
            }

            records.Add
            (
                new
                {
                    kind = "installed-ime", source, sourceSha256 = Hash(source), path = installed,
                    sha256 = Hash(installed)
                }
            );
            var registered = ImmInstallIME(installed, "XiaoXi IME");
            records.Add
                (new { kind = "ImmInstallIMEW", hkl = $"0x{registered:X}", error = Marshal.GetLastPInvokeError() });
            if (registered == 0)
            {
                using var existing = Registry.LocalMachine.OpenSubKey
                                         (@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts")
                                     ?? throw new InvalidOperationException("Keyboard layouts unavailable.");
                var templateId = existing.GetSubKeyNames().First
                (id =>
                    {
                        using var key = existing.OpenSubKey(id);
                        return string.Equals
                            (key?.GetValue("Ime File") as string, "XiaoXiIme.ime", StringComparison.OrdinalIgnoreCase);
                    }
                );
                registered = unchecked((nint)(int)Convert.ToUInt32(templateId, 16));
                records.Add(new { kind = "registration-template-fallback", templateId });
            }

            // IMM may reuse an already loaded HKL based on OriginalFilename in the version resource.
            // Give this payload a fresh registry identity as well as a fresh filename.
            using (var layouts = Registry.LocalMachine.OpenSubKey
                                     (@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts", true)
                                 ?? throw new InvalidOperationException("Keyboard layout registry is unavailable."))
            using (var template = layouts.OpenSubKey(unchecked((uint)registered).ToString("X8"))
                                  ?? throw new InvalidOperationException("Installed layout registry is unavailable."))
            {
                var id = Enumerable.Range(0x80, 0x7f).Select(value => $"E0{value:X2}0804")
                    .First(value => !layouts.GetSubKeyNames().Contains(value, StringComparer.OrdinalIgnoreCase));
                using var isolated = layouts.CreateSubKey(id);
                foreach (var name in template.GetValueNames())
                {
                    isolated.SetValue(name, template.GetValue(name)!, template.GetValueKind(name));
                }

                isolated.SetValue("Ime File", Path.GetFileName(installed));
                isolated.SetValue("Layout Text", "XiaoXi IME");
                layout = unchecked((nint)(int)Convert.ToUInt32(id, 16));
                Environment.SetEnvironmentVariable("XIAOXIIME_VALIDATION_LAYOUT", id);
                records.Add
                (
                    new
                    {
                        kind = "isolated-layout", id, imeFile = isolated.GetValue("Ime File"),
                        template = $"0x{registered:X}", sourceSha256 = Hash(source), installedSha256 = Hash(installed)
                    }
                );
            }

            await SaveAsync();
            if (layout == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "ImmInstallIMEW returned zero.");
            }

            ui = Start(Path.Combine(publish, "XiaoXiIme.exe"), "ui", "ui");
            using (var readiness = new XiaoXiIme.ImeIpc.XiaoXiImeIpcClient
                   (
                       new
                       (
                           "XiaoXiIme_ImeHost", ConnectTimeout: TimeSpan.FromSeconds(30),
                           RequestTimeout: TimeSpan.FromSeconds(30)
                       )
                   ))
            {
                var status = await readiness.WaitUntilReadyAsync(TimeSpan.FromSeconds(60));
                records.Add(new { kind = "host-ready", status });
                await SaveAsync();
            }

            result = 0;
            for (var iteration = 1; iteration <= 3; iteration++)
            {
                using var target = Start
                    (Path.Combine(publish, "Tests", "XiaoXiIme.IntegrationTestHost.exe"), $"target-{iteration}");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
                try
                {
                    await target.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    target.Kill(true);
                    await target.WaitForExitAsync();
                    throw;
                }

                target.WaitForExit();
                records.Add(new { kind = "target-exit", iteration, target.ExitCode });
                if (target.ExitCode != 0)
                {
                    result = target.ExitCode;
                }
            }
        }
        catch (Exception exception)
        {
            await File.WriteAllTextAsync(Path.Combine(evidence, "failure.txt"), exception.ToString());
            records.Add(new { kind = "failure", error = exception.ToString() });
        }
        finally
        {
            if (ui is not null)
            {
                if (!ui.HasExited)
                {
                    ui.Kill(true);
                    await ui.WaitForExitAsync();
                }

                ui.Dispose();
            }

            if (layout != 0 && OperatingSystem.IsWindows())
            {
                var unloaded = UnloadKeyboardLayout(layout);
                using var key = Registry.LocalMachine.OpenSubKey
                    (@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts", true);
                key?.DeleteSubKeyTree(unchecked((uint)layout).ToString("X8"), false);
                records.Add(new { kind = "cleanup-layout", unloaded });
            }

            try
            {
                if (File.Exists(installed))
                {
                    File.Delete(installed);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                records.Add
                (
                    new
                    {
                        kind = "cleanup-file", error = exception.ToString(),
                        scheduledForDeletion = MoveFileEx(installed, null, 4)
                    }
                );
            }

            records.Add(new { kind = "result", exitCode = result, utc = DateTimeOffset.UtcNow });
            await SaveAsync();
        }

        return result;

        Task SaveAsync() => File.WriteAllTextAsync
        (
            Path.Combine(evidence, "execution.json"),
            JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true })
        );

        Process Start(string path, string name, params string[] arguments)
        {
            var info = new ProcessStartInfo(path)
            {
                UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path)!,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var argument in arguments)
            {
                info.ArgumentList.Add(argument);
            }

            var process = Process.Start(info) ?? throw new InvalidOperationException($"Cannot start {path}");
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (records)
                    {
                        File.AppendAllText(Path.Combine(evidence, name + ".stdout.txt"), e.Data + Environment.NewLine);
                    }
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (records)
                    {
                        File.AppendAllText(Path.Combine(evidence, name + ".stderr.txt"), e.Data + Environment.NewLine);
                    }
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            records.Add
            (
                new
                {
                    kind = "process", name, path, pid = process.Id, arguments, sha256 = Hash(path),
                    dllSha256 = Hash(name == "ui"
                        ? Path.Combine(distribution, "Lib", "ImeUI", "XiaoXiIme.ImeUi.Avalonia.dll")
                        : Path.ChangeExtension(path, ".dll"))
                }
            );
            return process;
        }
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [DllImport("imm32.dll", EntryPoint = "ImmInstallIMEW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint ImmInstallIME(string file, string name);

    [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string source, string? destination, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnloadKeyboardLayout(nint layout);
}