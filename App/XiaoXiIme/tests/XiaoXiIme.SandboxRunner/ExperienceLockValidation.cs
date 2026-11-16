using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace XiaoXiIme.SandboxRunner;

internal static class ExperienceLockValidation
{
    internal static async Task<int> RunAsync(string uiPath, string directory)
    {
        uiPath = Path.GetFullPath(uiPath, AppContext.BaseDirectory);
        directory = Path.GetFullPath(directory, AppContext.BaseDirectory);
        Directory.CreateDirectory(directory);
        var bounds = Path.Combine(directory, "bounds.json");
        var results = new List<object>();
        var identities = new List<object>();
        Process? first = null;
        Process? reopened = null;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));

        Process Start()
        {
            var info = new ProcessStartInfo(uiPath) { UseShellExecute = false, WorkingDirectory = Path.GetTempPath() };
            info.Environment["XIAOXIIME_VALIDATION_BOUNDS"] = bounds;
            var process = Process.Start(info) ?? throw new InvalidOperationException("UI did not start.");
            identities.Add
            (
                new
                {
                    pid = process.Id, cwd = info.WorkingDirectory, arguments = info.ArgumentList.ToArray(),
                    files = new[]
                        {
                            uiPath, Path.ChangeExtension(uiPath, ".dll"),
                            typeof(ExperienceLockValidation).Assembly.Location
                        }.Select
                        (file => new
                            { file, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) }
                        )
                        .ToArray()
                }
            );
            return process;
        }

        async Task<JsonDocument> WaitAsync(Process process, Func<JsonElement, bool> predicate)
        {
            while (true)
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (process.HasExited)
                {
                    throw new InvalidOperationException($"UI exited: {process.ExitCode}");
                }

                try
                {
                    var document = JsonDocument.Parse(await File.ReadAllTextAsync(bounds, timeout.Token));
                    if (document.RootElement.GetProperty("pid").GetInt32() == process.Id && predicate
                            (document.RootElement))
                    {
                        return document;
                    }

                    document.Dispose();
                }
                catch (IOException)
                {
                }
                catch (JsonException)
                {
                }

                await Task.Delay(100, timeout.Token);
            }
        }

        async Task InputAsync(Process process)
        {
            using var loaded = await WaitAsync
            (
                process,
                root => root.GetProperty("debug").EnumerateArray().Any(d => d.GetProperty("loaded").GetBoolean())
            );
            var root = loaded.RootElement;
            var handle = new IntPtr(root.GetProperty("windows")[0].GetProperty("handle").GetInt64());
            if (!SetForegroundWindow(handle))
            {
                throw new InvalidOperationException("Cannot activate UI.");
            }

            var control = root.GetProperty("controls").EnumerateArray().Single
                (c => c.GetProperty("name").GetString() == "DebugOutput");
            SetCursorPos(control.GetProperty("x").GetInt32(), control.GetProperty("y").GetInt32());
            mouse_event(2, 0, 0, 0, UIntPtr.Zero);
            mouse_event(4, 0, 0, 0, UIntPtr.Zero);
            keybd_event(0x4E, 0, 0, UIntPtr.Zero);
            keybd_event(0x4E, 0, 2, UIntPtr.Zero);
            using var input = await WaitAsync
                (process, state => state.GetProperty("debug")[0].GetProperty("reading").GetString() == "n");
            await File.WriteAllTextAsync
                (Path.Combine(directory, $"input-{process.Id}.json"), input.RootElement.GetRawText(), timeout.Token);
        }

        async Task CloseAsync(Process process)
        {
            process.Refresh();
            if (!process.CloseMainWindow())
            {
                throw new InvalidOperationException("Normal close request failed.");
            }

            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Normal exit failed: {process.ExitCode}");
            }

            results.Add(new { name = "normal-exit", pid = process.Id, passed = true, process.ExitCode });
        }

        try
        {
            first = Start();
            await InputAsync(first);
            results.Add(new { name = "first-noargs-input", passed = true });
            using (var second = Start())
            {
                await second.WaitForExitAsync(timeout.Token);
                if (second.ExitCode != 0 || first.HasExited)
                {
                    throw new InvalidOperationException("Second entry did not transfer normally.");
                }

                using var alive = await WaitAsync
                (
                    first,
                    root => root.GetProperty("windows").EnumerateArray().Any
                        (w => w.GetProperty("IsVisible").GetBoolean())
                );
                results.Add(new { name = "second-noargs-transfer-first-alive", passed = true, second.ExitCode });
            }

            await CloseAsync(first);
            reopened = Start();
            await InputAsync(reopened);
            results.Add(new { name = "reopen-noargs-input-no-lock-crash", passed = true });
            await CloseAsync(reopened);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException
                                              or OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            results.Add(new { name = "failure", passed = false, error = exception.ToString() });
            return 1;
        }
        finally
        {
            foreach (var process in new[] { first, reopened })
            {
                if (process is not null && !process.HasExited)
                {
                    process.CloseMainWindow();
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    try
                    {
                        await process.WaitForExitAsync(cleanup.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        results.Add(new { name = "cleanup-timeout-no-kill", passed = false, process.Id });
                    }
                }

                process?.Dispose();
            }

            await File.WriteAllTextAsync
            (
                Path.Combine(directory, "report.json"),
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })
            );
            await File.WriteAllTextAsync
            (
                Path.Combine(directory, "actual.json"),
                JsonSerializer.Serialize(identities, new JsonSerializerOptions { WriteIndented = true })
            );
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}