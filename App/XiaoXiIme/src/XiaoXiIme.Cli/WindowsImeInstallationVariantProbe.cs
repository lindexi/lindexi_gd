using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace XiaoXiIme.Cli;

internal sealed record ImeInstallationVariantResult(
    string Id,
    string ProbePath,
    bool Copied,
    bool InstallSucceeded,
    string? KeyboardLayout,
    int Win32ErrorCode,
    string? Win32ErrorMessage,
    bool UninstallSucceeded,
    bool CleanupSucceeded,
    IReadOnlyList<string> Notes);

internal static class WindowsImeInstallationVariantProbe
{
    internal const string ShortImeFileName = "XIAOXI.IME";

    public static IReadOnlyList<ImeInstallationVariantResult> Run(string sourceImePath, string displayName)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceImePath)!;
        return
        [
            RunVariant("payload-short-name", sourceImePath, Path.Combine(sourceDirectory, ShortImeFileName), displayName),
            RunVariant("system32-original-name", sourceImePath, Path.Combine(Environment.SystemDirectory, Path.GetFileName(sourceImePath)), displayName),
            RunVariant("system32-short-name", sourceImePath, Path.Combine(Environment.SystemDirectory, ShortImeFileName), displayName),
        ];
    }

    internal static IReadOnlyList<(string Id, string Path)> CreateVariantPaths(string sourceImePath, string systemDirectory)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceImePath)!;
        return
        [
            ("payload-short-name", Path.Combine(sourceDirectory, ShortImeFileName)),
            ("system32-original-name", Path.Combine(systemDirectory, Path.GetFileName(sourceImePath))),
            ("system32-short-name", Path.Combine(systemDirectory, ShortImeFileName)),
        ];
    }

    private static ImeInstallationVariantResult RunVariant(string id, string sourceImePath, string probePath, string displayName)
    {
        var notes = new List<string>();
        var copied = false;
        var installSucceeded = false;
        string? keyboardLayoutText = null;
        var errorCode = 0;
        string? errorMessage = null;
        var uninstallSucceeded = false;
        var cleanupSucceeded = false;

        if (File.Exists(probePath))
        {
            notes.Add("Probe skipped because the target path already exists; no existing file was overwritten or deleted.");
            return new ImeInstallationVariantResult(id, probePath, false, false, null, 80, "The target file already exists.", false, false, notes);
        }

        try
        {
            File.Copy(sourceImePath, probePath, overwrite: false);
            copied = true;
            notes.Add("A private probe copy was created.");

            var layoutText = $"{displayName} [{id}]";
            var keyboardLayout = ImmInstallIME(probePath, layoutText);
            if (keyboardLayout == 0)
            {
                errorCode = Marshal.GetLastPInvokeError();
                errorMessage = errorCode == 0 ? "Windows did not provide an error code." : new Win32Exception(errorCode).Message;
                notes.Add("ImmInstallIME returned zero.");
            }
            else
            {
                installSucceeded = true;
                keyboardLayoutText = $"0x{unchecked((ulong) keyboardLayout):X}";
                uninstallSucceeded = TryRemoveProbeLayout(keyboardLayout, layoutText, Path.GetFileName(probePath), notes);
                if (!uninstallSucceeded)
                {
                    notes.Add("The probe layout could not be safely removed from the keyboard layouts registry.");
                }
                else
                {
                    notes.Add("The probe keyboard layout was uninstalled immediately.");
                }
            }
        }
        catch (Exception ex)
        {
            errorCode = ex.HResult & 0xFFFF;
            errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            notes.Add("The variant probe raised a managed exception.");
        }
        finally
        {
            if (copied && (!installSucceeded || uninstallSucceeded))
            {
                try
                {
                    File.Delete(probePath);
                    cleanupSucceeded = !File.Exists(probePath);
                    notes.Add(cleanupSucceeded ? "The private probe copy was deleted." : "The private probe copy still exists after deletion.");
                }
                catch (Exception ex)
                {
                    notes.Add($"Probe file cleanup failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
            else if (copied)
            {
                notes.Add("The private probe copy was retained because its registered layout could not be removed.");
            }
        }

        return new ImeInstallationVariantResult(
            id,
            probePath,
            copied,
            installSucceeded,
            keyboardLayoutText,
            errorCode,
            errorMessage,
            uninstallSucceeded,
            cleanupSucceeded,
            notes);
    }

    [DllImport("imm32.dll", EntryPoint = "ImmInstallIMEW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint ImmInstallIME(string imeFileName, string layoutText);

    private static bool TryRemoveProbeLayout(nint keyboardLayout, string expectedLayoutText, string expectedImeFile, List<string> notes)
    {
        var layoutId = unchecked((uint) keyboardLayout.ToInt64()).ToString("X8");
        const string layoutsPath = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";
        using var layouts = Registry.LocalMachine.OpenSubKey(layoutsPath, writable: true);
        using var layout = layouts?.OpenSubKey(layoutId);
        var actualLayoutText = layout?.GetValue("Layout Text") as string;
        var actualImeFile = layout?.GetValue("Ime File") as string;
        if (!string.Equals(actualLayoutText, expectedLayoutText, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(actualImeFile, expectedImeFile, StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Refused to remove layout {layoutId}: expected '{expectedLayoutText}'/'{expectedImeFile}', found '{actualLayoutText}'/'{actualImeFile}'.");
            return false;
        }

        UnloadKeyboardLayout(keyboardLayout);
        layout?.Dispose();
        layouts!.DeleteSubKeyTree(layoutId, throwOnMissingSubKey: false);
        notes.Add($"Removed probe keyboard layout registry key {layoutId}.");
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnloadKeyboardLayout(nint keyboardLayout);
}
