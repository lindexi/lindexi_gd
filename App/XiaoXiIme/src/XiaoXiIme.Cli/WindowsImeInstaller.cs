using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Security.Cryptography;
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
            return ImeInstallationResult.Failure("IME installation requires Windows.");
        }
        if (!IsAdministrator())
        {
            return ImeInstallationResult.Failure("IME installation requires an elevated administrator process.");
        }

        var validationError = ImeBinaryValidator.Validate(imeFilePath);
        if (validationError is not null)
        {
            return ImeInstallationResult.Failure(validationError);
        }

        var sourcePath = Path.GetFullPath(imeFilePath);
        var installedPath = Path.Combine(Environment.SystemDirectory, Path.GetFileName(sourcePath));
        var copied = false;
        if (File.Exists(installedPath))
        {
            if (!FilesMatch(sourcePath, installedPath))
            {
                return ImeInstallationResult.Failure($"Refusing to overwrite an existing different file: {installedPath}", installedPath: installedPath);
            }
        }
        else
        {
            try
            {
                File.Copy(sourcePath, installedPath, overwrite: false);
                copied = true;
                if (!FilesMatch(sourcePath, installedPath))
                {
                    File.Delete(installedPath);
                    return ImeInstallationResult.Failure($"The System32 IME copy did not match the source file: {installedPath}", installedPath: installedPath, copiedToSystemDirectory: true, rollbackSucceeded: true);
                }
            }
            catch (Exception ex)
            {
                return ImeInstallationResult.Failure($"Unable to copy the IME to '{installedPath}': {ex.GetType().Name}: {ex.Message}", ex.HResult & 0xFFFF, installedPath, copied);
            }
        }

        var keyboardLayout = ImmInstallIME(installedPath, displayName);
        if (keyboardLayout != 0)
        {
            return new ImeInstallationResult(
                true,
                $"XiaoXi IME installed from '{installedPath}'. HKL/layout id: 0x{unchecked((ulong) keyboardLayout):X}",
                0,
                sourcePath,
                installedPath,
                copied,
                false,
                null);
        }

        var errorCode = Marshal.GetLastPInvokeError();
        var errorMessage = errorCode == 0
            ? "Windows did not provide an error code. Verify the IME exports, architecture, signature, and file location."
            : new Win32Exception(errorCode).Message;
        bool? rollbackSucceeded = null;
        string? rollbackError = null;
        if (copied)
        {
            try
            {
                File.Delete(installedPath);
                rollbackSucceeded = !File.Exists(installedPath);
            }
            catch (Exception ex)
            {
                rollbackSucceeded = false;
                rollbackError = $"{ex.GetType().Name}: {ex.Message}";
            }
        }
        return new ImeInstallationResult(
            false,
            $"ImmInstallIME failed for System32 copy '{installedPath}' with Win32 error {errorCode}: {errorMessage}",
            errorCode,
            sourcePath,
            installedPath,
            copied,
            rollbackSucceeded,
            rollbackError);
    }

    private static bool FilesMatch(string firstPath, string secondPath)
    {
        var first = new FileInfo(firstPath);
        var second = new FileInfo(secondPath);
        if (first.Length != second.Length)
        {
            return false;
        }
        using var firstStream = File.OpenRead(firstPath);
        using var secondStream = File.OpenRead(secondPath);
        return SHA256.HashData(firstStream).AsSpan().SequenceEqual(SHA256.HashData(secondStream));
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
        var removedImeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layoutId in layouts.GetSubKeyNames())
        {
            using var layout = layouts.OpenSubKey(layoutId);
            var layoutText = layout?.GetValue("Layout Text") as string;
            var imeFile = layout?.GetValue("Ime File") as string;
            if (!IsXiaoXiIme(layoutText, imeFile))
            {
                continue;
            }

            if (!IsExpectedXiaoXiImeFile(imeFile))
            {
                return new ImeUninstallationResult(false, removed, $"Refusing to remove ambiguous keyboard layout {layoutId}.");
            }

            if (uint.TryParse(layoutId, System.Globalization.NumberStyles.HexNumber, null, out var layoutValue))
            {
                UnloadKeyboardLayout((nint) layoutValue);
            }
            layouts.DeleteSubKeyTree(layoutId, throwOnMissingSubKey: false);
            removed.Add(layoutId);
            if (imeFile is not null)
            {
                removedImeFiles.Add(imeFile);
            }
        }

        RemovePreloadReferences(Registry.CurrentUser, removed);
        using var users = Registry.Users;
        RemovePreloadReferences(users, removed, @".DEFAULT\Keyboard Layout\Preload");
        var cleanupMessages = CleanupUnreferencedSystemImeFiles(layouts, removedImeFiles, imeFileName);
        var layoutMessage = removed.Count == 0
            ? "No existing XiaoXi IME keyboard layout was found."
            : $"Removed XiaoXi IME keyboard layouts: {string.Join(", ", removed)}.";
        return new ImeUninstallationResult(true, removed, string.Join(" ", new[] { layoutMessage }.Concat(cleanupMessages)));
    }

    internal static bool IsXiaoXiIme(string? layoutText, string? imeFile) =>
        string.Equals(layoutText, "XiaoXi IME", StringComparison.OrdinalIgnoreCase)
        || layoutText?.StartsWith("XiaoXi IME Probe [", StringComparison.OrdinalIgnoreCase) == true
        || IsExpectedXiaoXiImeFile(imeFile);

    internal static bool IsExpectedXiaoXiImeFile(string? imeFile) =>
        string.Equals(imeFile, "XiaoXiIme.ime", StringComparison.OrdinalIgnoreCase)
        || string.Equals(imeFile, WindowsImeInstallationVariantProbe.ShortImeFileName, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> CleanupUnreferencedSystemImeFiles(RegistryKey layouts, IReadOnlyCollection<string> removedImeFiles, string primaryImeFileName)
    {
        var candidates = removedImeFiles
            .Append(primaryImeFileName)
            .Append(WindowsImeInstallationVariantProbe.ShortImeFileName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var referencedFiles = layouts.GetSubKeyNames()
            .Select(layoutId =>
            {
                using var layout = layouts.OpenSubKey(layoutId);
                return layout?.GetValue("Ime File") as string;
            })
            .Where(value => value is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var messages = new List<string>();
        foreach (var candidate in candidates)
        {
            if (referencedFiles.Contains(candidate))
            {
                messages.Add($"Retained System32\\{candidate} because another keyboard layout still references it.");
                continue;
            }

            var path = Path.Combine(Environment.SystemDirectory, candidate);
            if (!File.Exists(path))
            {
                continue;
            }
            try
            {
                File.Delete(path);
                messages.Add(File.Exists(path)
                    ? $"System32 IME file still exists after deletion: {path}."
                    : $"Removed unreferenced System32 IME file: {path}.");
            }
            catch (Exception ex)
            {
                messages.Add($"Unable to remove unreferenced System32 IME file '{path}': {ex.GetType().Name}: {ex.Message}");
            }
        }
        return messages;
    }

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

internal readonly record struct ImeInstallationResult(
    bool Succeeded,
    string Message,
    int? Win32ErrorCode,
    string? SourcePath,
    string? InstalledPath,
    bool CopiedToSystemDirectory,
    bool? RollbackSucceeded,
    string? RollbackError)
{
    public static ImeInstallationResult Failure(
        string message,
        int? win32ErrorCode = null,
        string? installedPath = null,
        bool copiedToSystemDirectory = false,
        bool? rollbackSucceeded = null,
        string? rollbackError = null) =>
        new(false, message, win32ErrorCode, null, installedPath, copiedToSystemDirectory, rollbackSucceeded, rollbackError);
}

internal readonly record struct ImeUninstallationResult(bool Succeeded, IReadOnlyList<string> RemovedLayoutIds, string Message);
