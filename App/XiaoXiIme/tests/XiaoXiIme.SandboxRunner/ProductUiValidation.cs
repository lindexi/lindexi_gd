using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.SandboxRunner;

internal static class ProductUiValidation
{
    internal static async Task<int> RunAsync(string uiPath, string packageRoot, string outputDirectory)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        Directory.CreateDirectory(outputDirectory);
        var results = new List<object>();
        var settingsPath = Path.Combine(Path.GetFullPath(outputDirectory), "product-settings.json");
        var options = new XiaoXiImeIpcOptions("ProductUiSandbox_" + Guid.NewGuid().ToString("N"));
        using var client = new XiaoXiImeIpcClient(options);
        var boundsPath = Path.Combine(Path.GetFullPath(outputDirectory), "control-bounds.json");
        var info = CreateUiStartInfo();

        ProcessStartInfo CreateUiStartInfo()
        {
            var startInfo = new ProcessStartInfo(Path.GetFullPath(uiPath)) { UseShellExecute = false };
            startInfo.Environment["XIAOXIIME_VALIDATION_BOUNDS"] = boundsPath;
            startInfo.Environment["XIAOXIIME_VALIDATION_PRODUCT_SETTINGS"] = settingsPath;
            startInfo.ArgumentList.Add("ui");
            startInfo.ArgumentList.Add(options.ServerName);
            return startInfo;
        }
        var publishHashes = new List<string>();
        foreach (var file in Directory.EnumerateFiles
                     (AppContext.BaseDirectory, "*", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal))
        {
            await using var stream = File.OpenRead(file);
            publishHashes.Add
            (
                $"{Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream))}  {Path.GetFileName(file)}"
            );
        }

        await File.WriteAllLinesAsync(Path.Combine(outputDirectory, "publish-sha256.txt"), publishHashes);
        var targetPath = Path.Combine
            (Path.GetFullPath(outputDirectory), "edit-target-" + Guid.NewGuid().ToString("N") + ".json");
        var targetInfo = new ProcessStartInfo
            (Path.Combine(packageRoot, "Tests", "XiaoXiIme.IntegrationTestHost.exe")) { UseShellExecute = false };
        targetInfo.ArgumentList.Add("--product-ui-target");
        targetInfo.ArgumentList.Add(targetPath);
        using var target = Process.Start(targetInfo) ?? throw new InvalidOperationException("EDIT host did not start.");
        using var ui = Process.Start(info) ?? throw new InvalidOperationException("UI process did not start.");
        try
        {
            await Task.Delay(2500);
            var modules = new List<string>();
            foreach (ProcessModule module in ui.Modules)
            {
                if (module.FileName is not { } file || !File.Exists(file))
                {
                    continue;
                }

                await using var stream = File.OpenRead(file);
                modules.Add
                (
                    $"PID={ui.Id} {Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream))}  {file}"
                );
            }

            await File.WriteAllLinesAsync(Path.Combine(outputDirectory, "loaded-modules-sha256.txt"), modules);
            var identities = new List<string>();
            foreach (var process in new[] { Process.GetCurrentProcess(), target, ui })
            {
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.FileName is not { } file || !File.Exists(file))
                    {
                        continue;
                    }

                    await using var stream = File.OpenRead(file);
                    identities.Add
                    (
                        $"PID={process.Id} {Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream))}  {file}"
                    );
                }
            }

            await File.WriteAllLinesAsync(Path.Combine(outputDirectory, "actual-processes-sha256.txt"), identities);
            var bar = FindOwnedWindow(ui.Id, "小希输入法");
            results.Add(new { check = "real-bar", passed = bar != 0, hwnd = bar.ToInt64() });
            results.Add(new { check = "tested-pid", passed = !ui.HasExited, pid = ui.Id });
            await ClickControlAsync(boundsPath, ui.Id, "小希输入法", "关于小希输入法");
            await Task.Delay(500);
            var about = FindOwnedWindow(ui.Id, "关于小希输入法");
            results.Add(new { check = "about-real-button", passed = about != 0, pid = ui.Id, hwnd = about.ToInt64() });
            if (about != 0)
            {
                PostMessage(about, 0x10, 0, 0);
            }

            var before = await RestoreTargetAsync(targetPath, about);
            await ClickControlAsync(boundsPath, ui.Id, "小希输入法", "小希输入法");
            var after = await ReadTargetAsync(targetPath);
            results.Add
            (
                new
                {
                    check = "bar-title-click-foreground",
                    passed = bar != 0 && before.Valid && after.Valid && before.Foreground == after.Foreground &&
                             before.Focus == after.Focus && before.Himc == after.Himc &&
                             before.Composition == after.Composition,
                    before, after,
                    boundary = "Visible native EDIT focus/HIMC; empty composition is not active IMM composition proof"
                }
            );
            info.ArgumentList.Add("--settings");
            using (var second = Process.Start
                       (info) ?? throw new InvalidOperationException("Second entry did not start."))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await second.WaitForExitAsync(timeout.Token);
                await Task.Delay(500);
                results.Add
                (
                    new
                    {
                        check = "settings-single-instance-entry",
                        passed = second.ExitCode == 0 && FindOwnedWindow(ui.Id, "小希输入法设置") != 0 && !ui.HasExited
                    }
                );
            }

            var settingsWindow = FindOwnedWindow(ui.Id, "小希输入法设置");
            if (GetWindowRect(settingsWindow, out var settingsRect))
            {
                // Single-screen 96 DPI: click the actual checkbox and Apply/Cancel buttons.
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "显示状态栏");
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "应用");
                await Task.Delay(1500);
                var applied = await client.GetProductSettingsAsync();
                results.Add
                (
                    new
                    {
                        check = "settings-real-apply",
                        passed = applied.Settings.Revision > 0 && !applied.Settings.StatusBarVisible, applied.Settings
                    }
                );
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "显示状态栏");
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "取消");
                await Task.Delay(500);
                results.Add
                (
                    new
                    {
                        check = "settings-real-cancel",
                        passed = (await client.GetProductSettingsAsync()).Settings == applied.Settings &&
                                 !IsWindow(settingsWindow)
                    }
                );
            }

            {
                await Task.Delay(1500);
                results.Add
                    (new { check = "hidden-bar", passed = !ui.HasExited && IsWindow(bar) && !IsWindowVisible(bar) });
                var recovery = CreateUiStartInfo();
                using var entry = Process.Start
                    (recovery) ?? throw new InvalidOperationException("Recovery entry did not start.");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await entry.WaitForExitAsync(timeout.Token);
                await Task.Delay(1500);
                results.Add
                (
                    new
                    {
                        check = "entry-restores-hidden-bar",
                        passed = !ui.HasExited && IsWindow(bar) && IsWindowVisible(bar)
                    }
                );
                using var settingsEntry = Process.Start
                    (info) ?? throw new InvalidOperationException("Settings entry did not start.");
                await settingsEntry.WaitForExitAsync(timeout.Token);
                var restored = await client.GetProductSettingsAsync();
                await File.WriteAllTextAsync
                    (Path.Combine(outputDirectory, "defaults-restored.json"), JsonSerializer.Serialize(restored));
                // Restore is a separate save; establish a non-default baseline through the real controls.
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "显示状态栏");
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "应用");
                using var defaultsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var baseline = await client.GetProductSettingsAsync();
                while (baseline.Settings.Revision != restored.Settings.Revision + 1 ||
                       baseline.Settings.StatusBarVisible)
                {
                    await Task.Delay(20, defaultsTimeout.Token);
                    baseline = await client.GetProductSettingsAsync();
                }

                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "恢复默认");
                await File.WriteAllTextAsync
                    (Path.Combine(outputDirectory, "defaults-draft.json"), await File.ReadAllTextAsync(boundsPath));
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "应用");
                var defaults = await client.GetProductSettingsAsync();
                while (defaults.Settings.Revision == baseline.Settings.Revision)
                {
                    await Task.Delay(20, defaultsTimeout.Token);
                    defaults = await client.GetProductSettingsAsync();
                }

                await Task.Delay(400, defaultsTimeout.Token);
                var persisted = await File.ReadAllTextAsync(settingsPath);
                using var persistedDocument = JsonDocument.Parse(persisted);
                var saved = persistedDocument.RootElement;
                results.Add
                (
                    new
                    {
                        check = "defaults-real-apply",
                        passed = defaults.Settings.StatusBarVisible &&
                                 defaults.Settings.InputScheme == ImeProductInputScheme.FullPinyin &&
                                 defaults.Settings.StatusBarPosition is null &&
                                 defaults.Settings.Revision == baseline.Settings.Revision + 1 &&
                                 saved.GetProperty("revision").GetInt64() == defaults.Settings.Revision &&
                                 saved.GetProperty("statusBarVisible").GetBoolean() &&
                                 saved.GetProperty("inputScheme").GetString() == "FullPinyin" &&
                                 saved.GetProperty("statusBarPosition").ValueKind == JsonValueKind.Null &&
                                 IsWindowVisible(bar),
                        restored = restored.Settings, baseline = baseline.Settings, actual = defaults.Settings,
                        persisted, barVisible = IsWindowVisible(bar)
                    }
                );
                await ClickControlAsync(boundsPath, ui.Id, "小希输入法设置", "取消");
            }
        }
        catch (Exception exception) when (exception is TimeoutException or InvalidOperationException or IOException)
        {
            results.Add(new { check = "execution-error", passed = false, error = exception.ToString() });
        }
        finally
        {
            if (!ui.HasExited)
            {
                var exitInfo = CreateUiStartInfo();
                exitInfo.ArgumentList.Add("--exit");
                using var exit = Process.Start
                    (exitInfo) ?? throw new InvalidOperationException("Exit entry did not start.");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await exit.WaitForExitAsync(timeout.Token);
                await ui.WaitForExitAsync(timeout.Token);
                results.Add
                (
                    new
                    {
                        check = "normal-ui-exit",
                        passed = exit.ExitCode == 0 && ui.ExitCode == 0 && FindOwnedWindow(ui.Id, "小希输入法") == 0
                    }
                );
            }

            var reopenInfo = CreateUiStartInfo();
            using (var reopened = Process.Start
                       (reopenInfo) ?? throw new InvalidOperationException("Reopen did not start."))
            {
                await Task.Delay(2000);
                var reopenedBar = FindOwnedWindow(reopened.Id, "小希输入法");
                results.Add
                    (new { check = "entry-lock-reacquisition", passed = !reopened.HasExited && reopenedBar != 0 });
                reopenInfo.ArgumentList.Add("--exit");
                using var exit = Process.Start
                    (reopenInfo) ?? throw new InvalidOperationException("Reopened exit did not start.");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await exit.WaitForExitAsync(timeout.Token);
                await reopened.WaitForExitAsync(timeout.Token);
                results.Add
                    (new { check = "reopened-normal-exit", passed = reopened.ExitCode == 0 && exit.ExitCode == 0 });
            }

            await File.WriteAllTextAsync(targetPath + ".stop", "stop");
            using (var targetTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                await target.WaitForExitAsync(targetTimeout.Token);
            }

            results.Add(new { check = "native-target-normal-exit", passed = target.ExitCode == 0 });
            await File.WriteAllTextAsync
            (
                Path.Combine(outputDirectory, "product-ui-report.json"), JsonSerializer.Serialize
                (
                    new
                    {
                        results,
                        cleanup = "single-instance-exit-request-no-kill",
                        pending = new[]
                        {
                            "xx-single-multi-HIMC", "bar-drag-menu", "active-native-composition-save",
                            "scheme-save-restart", "DPI-not-expanded"
                        }
                    }, new JsonSerializerOptions { WriteIndented = true }
                )
            );
        }

        using var report = JsonDocument.Parse(JsonSerializer.Serialize(results));
        return report.RootElement.EnumerateArray().Any(result => !result.GetProperty("passed").GetBoolean()) ? 1 : 0;
    }

    private sealed record TargetSnapshot(long Foreground, long Focus, long Himc, string Composition, bool Valid);

    private static async Task<TargetSnapshot> ReadTargetAsync(string path)
    {
        await File.WriteAllTextAsync(path + ".snapshot", "snapshot");
        for (var attempt = 0; attempt < 100 && File.Exists(path + ".snapshot"); attempt++)
        {
            await Task.Delay(10);
        }

        if (File.Exists(path + ".snapshot"))
        {
            throw new TimeoutException("EDIT owner did not acknowledge snapshot.");
        }

        return ReadTarget(path);
    }

    private static TargetSnapshot ReadTarget(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var window = (nint)root.GetProperty("window").GetInt64();
        var edit = (nint)root.GetProperty("edit").GetInt64();
        var gui = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        var success = GetGUIThreadInfo(root.GetProperty("thread").GetUInt32(), ref gui);
        var foreground = GetForegroundWindow();
        var himc = (nint)root.GetProperty("himc").GetInt64();
        var composition = root.GetProperty("composition").GetString() ?? string.Empty;
        return new
        (
            foreground.ToInt64(), gui.Focus.ToInt64(), himc.ToInt64(), composition,
            success && foreground == window && gui.Focus == edit && IsWindowVisible(window) && IsWindowVisible(edit)
            && himc != 0 && himc.ToInt64() == root.GetProperty("ownedHimc").GetInt64()
        );
    }

    private static async Task<TargetSnapshot> RestoreTargetAsync(string path, nint closedWindow)
    {
        using (var target = JsonDocument.Parse(await File.ReadAllTextAsync(path)))
        {
            var pid = target.RootElement.GetProperty("pid").GetUInt32();
            Marshal.SetLastPInvokeError(0);
            var allowed = AllowSetForegroundWindow(pid);
            var lastError = Marshal.GetLastPInvokeError();
            await File.WriteAllTextAsync
                (path + ".authorization.json", JsonSerializer.Serialize(new { pid, allowed, lastError }));
        }

        await File.WriteAllTextAsync(path + ".restore", "restore");
        TargetSnapshot? previous = null;
        var stable = 0;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (File.Exists(path) && (closedWindow == 0 || !IsWindow(closedWindow)))
            {
                var current = await ReadTargetAsync(path);
                await File.AppendAllTextAsync
                    (path + ".focus.jsonl", JsonSerializer.Serialize(new { attempt, current }) + Environment.NewLine);
                stable = current.Valid && current == previous ? stable + 1 : 0;
                if (stable >= 3)
                {
                    return current;
                }

                previous = current;
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException
            ("Visible EDIT target did not regain stable foreground/focus/HIMC after dialog close.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        internal uint Size, Flags;
        internal nint Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        internal Rect CaretRect;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AllowSetForegroundWindow(uint pid);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);

    [DllImport("imm32.dll")]
    private static extern nint ImmGetContext(nint hwnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(nint hwnd, nint himc);

    [DllImport("imm32.dll")]
    private static extern int ImmGetCompositionStringW(nint himc, uint index, byte[]? buffer, uint length);

    private static nint FindOwnedWindow(int pid, string title)
    {
        nint found = 0;
        EnumWindows
        (
            (hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out var owner);
                var text = new System.Text.StringBuilder(256);
                GetWindowText(hwnd, text, text.Capacity);
                if (owner == pid && IsWindow(hwnd) && text.ToString() == title)
                {
                    found = hwnd;
                    return false;
                }

                return true;
            }, 0
        );
        return found;
    }

    private static async Task ClickControlAsync(string path, int pid, string title, string text)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                if (document.RootElement.GetProperty("pid").GetInt32() == pid)
                {
                    foreach (var control in document.RootElement.GetProperty("controls").EnumerateArray())
                    {
                        if (control.GetProperty("title").GetString() == title &&
                            control.GetProperty("text").GetString() == text &&
                            control.GetProperty("enabled").GetBoolean())
                        {
                            Click(control.GetProperty("x").GetInt32(), control.GetProperty("y").GetInt32());
                            await Task.Delay(400);
                            return;
                        }
                    }
                }
            }

            await Task.Delay(200);
        }

        throw new InvalidOperationException($"Actual enabled control not found: {title}/{text}, PID={pid}");
    }

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hwnd, uint message, nint wparam, nint lparam);

    private static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(2, 0, 0, 0, 0);
        mouse_event(4, 0, 0, 0, 0);
    }

    private delegate bool EnumWindowCallback(nint hwnd, nint parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowCallback callback, nint parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hwnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        internal int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hwnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);
}