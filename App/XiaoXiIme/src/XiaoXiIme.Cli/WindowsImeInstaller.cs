using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32;

namespace XiaoXiIme.Cli;

internal interface IImeInstaller
{
    ImeInstallationResult Install(string imeFilePath, string displayName);

    ImeUninstallationResult UninstallExisting(string displayName, string imeFileName);
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsImeInstaller : IImeInstaller
{
    public ImeInstallationResult Install(string imeFilePath, string displayName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ImeInstallationResult(false, "IME installation requires Windows.");
        }
        if (!IsAdministrator())
        {
            return new ImeInstallationResult(false, "IME installation requires an elevated administrator process.");
        }

        var keyboardLayout = ImmInstallIME(imeFilePath, displayName);
        if (keyboardLayout == 0)
        {
            var errorCode = Marshal.GetLastPInvokeError();
            var errorMessage = errorCode == 0
                ? "Windows did not provide an error code. Verify the IME exports, architecture, signature, and file location."
                : new Win32Exception(errorCode).Message;
            return new ImeInstallationResult(false, $"ImmInstallIME failed with Win32 error {errorCode}: {errorMessage}");
        }

        return new ImeInstallationResult(true, $"XiaoXi IME installed. HKL/layout id: 0x{unchecked((ulong) keyboardLayout):X}");
    }

    public ImeUninstallationResult UninstallExisting(string displayName, string imeFileName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ImeUninstallationResult(false, [], "IME uninstall requires Windows.");
        }
        if (!IsAdministrator())
        {
            return new ImeUninstallationResult(false, [], "IME uninstall requires an elevated administrator process.");
        }

        const string layoutsPath = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";
        using var layouts = Registry.LocalMachine.OpenSubKey(layoutsPath, writable: true);
        if (layouts is null)
        {
            return new ImeUninstallationResult(false, [], $"Unable to open HKLM\\{layoutsPath}.");
        }

        var removed = new List<string>();
        foreach (var layoutId in layouts.GetSubKeyNames())
        {
            using var layout = layouts.OpenSubKey(layoutId);
            var layoutText = layout?.GetValue("Layout Text") as string;
            var imeFile = layout?.GetValue("Ime File") as string;
            if (!string.Equals(layoutText, displayName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(imeFile, imeFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsXiaoXiIme(layoutText, imeFile))
            {
                return new ImeUninstallationResult(false, removed, $"Refusing to remove ambiguous keyboard layout {layoutId}.");
            }

            if (uint.TryParse(layoutId, System.Globalization.NumberStyles.HexNumber, null, out var layoutValue))
            {
                UnloadKeyboardLayout((nint) layoutValue);
            }
            layouts.DeleteSubKeyTree(layoutId, throwOnMissingSubKey: false);
            removed.Add(layoutId);
        }

        RemovePreloadReferences(Registry.CurrentUser, removed);
        using var users = Registry.Users;
        RemovePreloadReferences(users, removed, @".DEFAULT\Keyboard Layout\Preload");
        return new ImeUninstallationResult(true, removed, removed.Count == 0
            ? "No existing XiaoXi IME keyboard layout was found."
            : $"Removed XiaoXi IME keyboard layouts: {string.Join(", ", removed)}.");
    }

    private static bool IsXiaoXiIme(string? layoutText, string? imeFile) =>
        string.Equals(layoutText, "XiaoXi IME", StringComparison.OrdinalIgnoreCase)
        || string.Equals(imeFile, "XiaoXiIme.ime", StringComparison.OrdinalIgnoreCase);

    private static void RemovePreloadReferences(RegistryKey root, IReadOnlyCollection<string> removedLayoutIds, string path = @"Keyboard Layout\Preload")
    {
        if (removedLayoutIds.Count == 0)
        {
            return;
        }
        using var preload = root.OpenSubKey(path, writable: true);
        if (preload is null)
        {
            return;
        }
        foreach (var valueName in preload.GetValueNames())
        {
            var value = preload.GetValue(valueName)?.ToString();
            if (value is not null && removedLayoutIds.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                preload.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    [DllImport("imm32.dll", EntryPoint = "ImmInstallIMEW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint ImmInstallIME(string imeFileName, string layoutText);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnloadKeyboardLayout(nint keyboardLayout);
}

internal readonly record struct ImeInstallationResult(bool Succeeded, string Message);

internal readonly record struct ImeUninstallationResult(bool Succeeded, IReadOnlyList<string> RemovedLayoutIds, string Message);
