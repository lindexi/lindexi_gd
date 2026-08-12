using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace XiaoXiIme.Cli;

internal interface IImeInstaller
{
    ImeInstallationResult Install(string imeFilePath, string displayName);

    ImeInstallationResult InstallPair(string x64ImeFilePath, string x86ImeFilePath, string displayName);

    ImeUninstallationResult UninstallExisting(string displayName, string imeFileName);
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsImeInstaller : IImeInstaller
{
    private const uint InstallLayoutOrTipUninstall = 0x00000001;
    private const uint MoveFileDelayUntilReboot = 0x00000004;
    private static readonly Regex RetiredImeFileNamePattern = new(
        @"\AXiaoXiIme\.retired-\d{8}T\d{6}Z-[0-9a-f]{32}\.ime\z",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

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
                return ImeInstallationResult.Failure(
                    $"Refusing to overwrite an existing different file: {installedPath}",
                    installedPath: installedPath,
                    failureKind: ImeInstallationFailureKind.ExistingFileConflict);
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
            var layoutId = unchecked((uint) keyboardLayout).ToString("X8");
            var layoutTip = CreateLayoutTip(layoutId);
            if (!InstallLayoutOrTip(layoutTip, 0))
            {
                var registrationErrorCode = Marshal.GetLastPInvokeError();
                var registrationError = registrationErrorCode == 0
                    ? "Windows did not provide an error code."
                    : new Win32Exception(registrationErrorCode).Message;
                return ImeInstallationResult.Failure(
                    $"XiaoXi IME was registered as layout {layoutId}, but adding '{layoutTip}' to the current user's input methods failed with Win32 error {registrationErrorCode}: {registrationError}",
                    registrationErrorCode,
                    installedPath,
                    copied);
            }

            return new ImeInstallationResult(
                true,
                $"XiaoXi IME installed from '{installedPath}'. HKL/layout id: 0x{unchecked((ulong) keyboardLayout):X}. Added '{layoutTip}' to the current user's input methods.",
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
            rollbackError,
            ImeInstallationFailureKind.ImmInstallImeFailure);
    }

    public ImeInstallationResult InstallPair(string x64ImeFilePath, string x86ImeFilePath, string displayName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ImeInstallationResult.Failure("IME installation requires Windows.");
        }
        if (!IsAdministrator())
        {
            return ImeInstallationResult.Failure("IME installation requires an elevated administrator process.");
        }

        var x86ValidationError = ImeBinaryValidator.Validate(x86ImeFilePath);
        if (x86ValidationError is not null)
        {
            return ImeInstallationResult.Failure($"Invalid x86 IME: {x86ValidationError}");
        }

        var x86SourcePath = Path.GetFullPath(x86ImeFilePath);
        var x86SystemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        if (string.IsNullOrWhiteSpace(x86SystemDirectory))
        {
            return ImeInstallationResult.Failure("Unable to resolve the SysWOW64 system directory for the x86 IME.");
        }

        var x86InstalledPath = Path.Combine(x86SystemDirectory, Path.GetFileName(x86SourcePath));
        var x86Copied = false;
        try
        {
            if (File.Exists(x86InstalledPath))
            {
                if (!FilesMatch(x86SourcePath, x86InstalledPath))
                {
                    return ImeInstallationResult.Failure(
                        $"Refusing to overwrite an existing different x86 IME file: {x86InstalledPath}",
                        installedPath: x86InstalledPath,
                        failureKind: ImeInstallationFailureKind.ExistingFileConflict);
                }
            }
            else
            {
                File.Copy(x86SourcePath, x86InstalledPath, overwrite: false);
                x86Copied = true;
                if (!FilesMatch(x86SourcePath, x86InstalledPath))
                {
                    File.Delete(x86InstalledPath);
                    return ImeInstallationResult.Failure($"The SysWOW64 IME copy did not match the source file: {x86InstalledPath}");
                }
            }

            var result = Install(x64ImeFilePath, displayName);
            if (result.Succeeded)
            {
                return result with { Message = $"{result.Message} Installed x86 companion at '{x86InstalledPath}'." };
            }

            if (x86Copied)
            {
                File.Delete(x86InstalledPath);
            }
            return result;
        }
        catch (Exception ex)
        {
            if (x86Copied && File.Exists(x86InstalledPath))
            {
                try
                {
                    File.Delete(x86InstalledPath);
                }
                catch (Exception cleanupException)
                {
                    return ImeInstallationResult.Failure(
                        $"Unable to install the x86 IME companion: {ex.GetType().Name}: {ex.Message} Rollback failed: {cleanupException.GetType().Name}: {cleanupException.Message}",
                        cleanupException.HResult & 0xFFFF,
                        x86InstalledPath,
                        copiedToSystemDirectory: true,
                        rollbackSucceeded: false,
                        rollbackError: cleanupException.Message);
                }
            }

            return ImeInstallationResult.Failure(
                $"Unable to install the x86 IME companion at '{x86InstalledPath}': {ex.GetType().Name}: {ex.Message}",
                ex.HResult & 0xFFFF,
                x86InstalledPath,
                x86Copied,
                rollbackSucceeded: x86Copied ? true : null);
        }
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

            InstallLayoutOrTip(CreateLayoutTip(layoutId), InstallLayoutOrTipUninstall);
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
        var system32Cleanup = CleanupUnreferencedSystemImeFiles(layouts, removedImeFiles, imeFileName, Environment.SystemDirectory, "System32");
        var systemX86Directory = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        var sysWow64Cleanup = string.IsNullOrWhiteSpace(systemX86Directory)
            ? new ImeFileCleanupResult(true, [], false, [], [])
            : CleanupUnreferencedSystemImeFiles(layouts, removedImeFiles, imeFileName, systemX86Directory, "SysWOW64");
        var layoutMessage = removed.Count == 0
            ? "No existing XiaoXi IME keyboard layout was found."
            : $"Removed XiaoXi IME keyboard layouts: {string.Join(", ", removed)}.";
        return new ImeUninstallationResult(
            system32Cleanup.Succeeded && sysWow64Cleanup.Succeeded,
            removed,
            string.Join(" ", new[] { layoutMessage }.Concat(system32Cleanup.Messages).Concat(sysWow64Cleanup.Messages)),
            system32Cleanup.RebootRequired || sysWow64Cleanup.RebootRequired,
            system32Cleanup.PendingDeletePaths.Concat(sysWow64Cleanup.PendingDeletePaths).ToArray(),
            system32Cleanup.RetiredFilePaths.Concat(sysWow64Cleanup.RetiredFilePaths).ToArray());
    }

    internal static bool IsXiaoXiIme(string? layoutText, string? imeFile) =>
        string.Equals(layoutText, "XiaoXi IME", StringComparison.OrdinalIgnoreCase)
        || layoutText?.StartsWith("XiaoXi IME Probe [", StringComparison.OrdinalIgnoreCase) == true
        || IsExpectedXiaoXiImeFile(imeFile);

    internal static bool IsExpectedXiaoXiImeFile(string? imeFile) =>
        string.Equals(imeFile, "XiaoXiIme.ime", StringComparison.OrdinalIgnoreCase)
        || string.Equals(imeFile, WindowsImeInstallationVariantProbe.ShortImeFileName, StringComparison.OrdinalIgnoreCase);

    internal static bool IsRetiredXiaoXiImeFile(string? fileName) =>
        fileName is not null && RetiredImeFileNamePattern.IsMatch(fileName);

    internal static string CreateRetiredImeFileName(DateTimeOffset timestamp, Guid id) =>
        $"XiaoXiIme.retired-{timestamp.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}-{id:N}.ime";

    private static ImeFileCleanupResult CleanupUnreferencedSystemImeFiles(
        RegistryKey layouts,
        IReadOnlyCollection<string> removedImeFiles,
        string primaryImeFileName,
        string systemDirectory,
        string systemDirectoryName)
    {
        var candidates = removedImeFiles
            .Append(primaryImeFileName)
            .Append(WindowsImeInstallationVariantProbe.ShortImeFileName)
            .Concat(Directory.EnumerateFiles(systemDirectory, "XiaoXiIme.retired-*.ime")
                .Select(Path.GetFileName)
                .Where(IsRetiredXiaoXiImeFile)!)
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
        var pendingDeletePaths = new List<string>();
        var retiredFilePaths = new List<string>();
        var succeeded = true;
        foreach (var candidate in candidates)
        {
            if (referencedFiles.Contains(candidate))
            {
                messages.Add($"Retained {systemDirectoryName}\\{candidate} because another keyboard layout still references it.");
                continue;
            }

            var path = Path.Combine(systemDirectory, candidate);
            if (!File.Exists(path))
            {
                continue;
            }
            try
            {
                File.Delete(path);
                if (File.Exists(path))
                {
                    succeeded = false;
                    messages.Add($"{systemDirectoryName} IME file still exists after deletion: {path}.");
                }
                else
                {
                    messages.Add($"Removed unreferenced {systemDirectoryName} IME file: {path}.");
                }
            }
            catch (Exception ex)
            {
                var retiredPath = Path.Combine(
                    systemDirectory,
                    CreateRetiredImeFileName(DateTimeOffset.UtcNow, Guid.NewGuid()));
                if (TryMoveToRetiredPath(path, retiredPath, out var moveError))
                {
                    retiredFilePaths.Add(retiredPath);
                    messages.Add($"Unable to remove unreferenced System32 IME file '{path}' immediately: {ex.GetType().Name}: {ex.Message} Moved it to isolated path '{retiredPath}' so the canonical IME path can be reused in this sandbox run.");
                }
                else if (TryScheduleDeleteOnReboot(path, out var scheduleError))
                {
                    succeeded = false;
                    pendingDeletePaths.Add(path);
                    messages.Add($"Unable to remove unreferenced System32 IME file '{path}' immediately: {ex.GetType().Name}: {ex.Message} Moving it to '{retiredPath}' failed with Win32 error {moveError}: {new Win32Exception(moveError).Message} Scheduled the original file for deletion at the next Windows restart.");
                }
                else
                {
                    succeeded = false;
                    messages.Add($"Unable to remove unreferenced System32 IME file '{path}': {ex.GetType().Name}: {ex.Message} Moving it to '{retiredPath}' failed with Win32 error {moveError}: {new Win32Exception(moveError).Message} Scheduling deletion at restart also failed with Win32 error {scheduleError}: {new Win32Exception(scheduleError).Message}");
                }
            }
        }
        return new ImeFileCleanupResult(succeeded, messages, pendingDeletePaths.Count > 0, pendingDeletePaths, retiredFilePaths);
    }

    internal static bool TryMoveToRetiredPath(string sourcePath, string retiredPath, out int errorCode)
    {
        if (MoveFileEx(sourcePath, retiredPath, 0))
        {
            errorCode = 0;
            return true;
        }

        errorCode = Marshal.GetLastPInvokeError();
        return false;
    }

    internal static bool TryScheduleDeleteOnReboot(string path, out int errorCode)
    {
        if (MoveFileEx(path, null, MoveFileDelayUntilReboot))
        {
            errorCode = 0;
            return true;
        }

        errorCode = Marshal.GetLastPInvokeError();
        return false;
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

    internal static string CreateLayoutTip(string layoutId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        if (layoutId.Length != 8 || !uint.TryParse(layoutId, System.Globalization.NumberStyles.HexNumber, null, out var layoutValue))
        {
            throw new ArgumentException("The keyboard layout id must contain exactly eight hexadecimal characters.", nameof(layoutId));
        }

        return $"{layoutValue & 0xFFFF:X4}:{layoutValue:X8}";
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    [DllImport("input.dll", EntryPoint = "InstallLayoutOrTip", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InstallLayoutOrTip(string layoutOrTip, uint flags);

    [DllImport("imm32.dll", EntryPoint = "ImmInstallIMEW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint ImmInstallIME(string imeFileName, string layoutText);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnloadKeyboardLayout(nint keyboardLayout);

    [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, uint flags);
}

internal enum ImeInstallationFailureKind
{
    None,
    ExistingFileConflict,
    ImmInstallImeFailure,
    Other,
}

internal readonly record struct ImeInstallationResult(
    bool Succeeded,
    string Message,
    int? Win32ErrorCode,
    string? SourcePath,
    string? InstalledPath,
    bool CopiedToSystemDirectory,
    bool? RollbackSucceeded,
    string? RollbackError,
    ImeInstallationFailureKind FailureKind = ImeInstallationFailureKind.None)
{
    public static ImeInstallationResult Failure(
        string message,
        int? win32ErrorCode = null,
        string? installedPath = null,
        bool copiedToSystemDirectory = false,
        bool? rollbackSucceeded = null,
        string? rollbackError = null,
        ImeInstallationFailureKind failureKind = ImeInstallationFailureKind.Other) =>
        new(false, message, win32ErrorCode, null, installedPath, copiedToSystemDirectory, rollbackSucceeded, rollbackError, failureKind);
}

internal readonly record struct ImeUninstallationResult(
    bool Succeeded,
    IReadOnlyList<string> RemovedLayoutIds,
    string Message,
    bool RebootRequired = false,
    IReadOnlyList<string>? PendingDeletePaths = null,
    IReadOnlyList<string>? RetiredFilePaths = null);

internal readonly record struct ImeFileCleanupResult(
    bool Succeeded,
    IReadOnlyList<string> Messages,
    bool RebootRequired,
    IReadOnlyList<string> PendingDeletePaths,
    IReadOnlyList<string> RetiredFilePaths);
