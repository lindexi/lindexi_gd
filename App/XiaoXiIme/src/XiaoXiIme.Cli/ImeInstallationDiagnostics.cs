using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace XiaoXiIme.Cli;

internal sealed record ImeDiagnosticReport(
    ImeEnvironmentDiagnostic Environment,
    ImeFileDiagnostic SourceFile,
    ImePeDiagnostic? PeImage,
    IReadOnlyList<ImeImportDiagnostic> Imports,
    ImeNativeProbeDiagnostic NativeProbe,
    ImeInstallationStateDiagnostic InstallationState,
    IReadOnlyList<string> Findings);

internal sealed record ImeEnvironmentDiagnostic(
    string OperatingSystem,
    string OsArchitecture,
    string ProcessArchitecture,
    bool Is64BitOperatingSystem,
    bool Is64BitProcess,
    string ProcessPath,
    string CurrentDirectory,
    string WindowsDirectory,
    string SystemDirectory,
    string SystemX86Directory);

internal sealed record ImeFileDiagnostic(
    string FullPath,
    bool Exists,
    long? Length,
    string? Sha256,
    string? Attributes,
    bool CanOpenForRead,
    string? ReadError,
    bool HasMarkOfTheWeb,
    string? FileVersion,
    string? ProductVersion);

internal sealed record ImePeDiagnostic(
    string Machine,
    string Magic,
    string Subsystem,
    bool IsDll,
    bool HasImportTable,
    int ImportCount,
    string? ParseError);

internal sealed record ImeImportDiagnostic(
    string Module,
    string Kind,
    bool Resolved,
    string? ResolvedPath,
    int ErrorCode,
    string? ErrorMessage);

internal sealed record ImeNativeProbeDiagnostic(
    bool ImageMappingSucceeded,
    int ImageMappingErrorCode,
    string? ImageMappingErrorMessage);

internal sealed record ImeInstallationStateDiagnostic(
    string ExpectedSystemImePath,
    bool ExpectedSystemImeExists,
    long? ExpectedSystemImeLength,
    bool SystemDirectoryWriteProbeSucceeded,
    int SystemDirectoryWriteProbeErrorCode,
    string? SystemDirectoryWriteProbeErrorMessage,
    IReadOnlyList<ImeKeyboardLayoutDiagnostic> MatchingKeyboardLayouts);

internal sealed record ImeKeyboardLayoutDiagnostic(string LayoutId, string? LayoutText, string? ImeFile, string? LayoutFile);

internal static class ImeInstallationDiagnostics
{
    private const uint LoadLibraryAsDatafile = 0x00000002;
    private const uint DontResolveDllReferences = 0x00000001;
    private const uint LoadLibrarySearchSystem32 = 0x00000800;

    public static ImeDiagnosticReport Collect(string imeFilePath)
    {
        var fullPath = Path.GetFullPath(imeFilePath);
        var environment = CollectEnvironment();
        var sourceFile = CollectFile(fullPath);
        var imports = new List<ImeImportDiagnostic>();
        ImePeDiagnostic? peImage = null;
        var findings = new List<string>();

        if (sourceFile.Exists && sourceFile.CanOpenForRead)
        {
            (peImage, var importNames) = ReadPe(fullPath);
            foreach (var importName in importNames)
            {
                imports.Add(ResolveImport(importName));
            }
        }

        var nativeProbe = ProbeNativeImage(fullPath, sourceFile.Exists);
        var installationState = CollectInstallationState(Path.GetFileName(fullPath));

        if (!sourceFile.Exists)
        {
            findings.Add("The source IME file does not exist at the resolved absolute path.");
        }
        else if (!sourceFile.CanOpenForRead)
        {
            findings.Add("The source IME exists but cannot be opened for read access.");
        }
        if (sourceFile.HasMarkOfTheWeb)
        {
            findings.Add("The source IME has a Zone.Identifier stream (Mark of the Web), which may affect Windows security policy.");
        }
        if (peImage?.ParseError is not null)
        {
            findings.Add($"The PE image could not be fully parsed: {peImage.ParseError}");
        }
        if (peImage is not null && !MachineMatchesProcess(peImage.Machine, environment.ProcessArchitecture))
        {
            findings.Add($"The IME PE machine '{peImage.Machine}' does not match the CLI process architecture '{environment.ProcessArchitecture}'.");
        }
        foreach (var import in imports.Where(import => import.Kind == "native" && !import.Resolved))
        {
            findings.Add($"Imported native module '{import.Module}' could not be resolved from the Windows system directory (error {import.ErrorCode}: {import.ErrorMessage}).");
        }
        if (sourceFile.Exists && !nativeProbe.ImageMappingSucceeded)
        {
            findings.Add($"Windows could not map the IME as an image without resolving imports (error {nativeProbe.ImageMappingErrorCode}: {nativeProbe.ImageMappingErrorMessage}).");
        }
        if (!installationState.SystemDirectoryWriteProbeSucceeded)
        {
            findings.Add($"The elevated CLI could not create and delete a temporary probe file in the Windows system directory (error {installationState.SystemDirectoryWriteProbeErrorCode}: {installationState.SystemDirectoryWriteProbeErrorMessage}).");
        }
        if (installationState.ExpectedSystemImeExists)
        {
            findings.Add($"A same-named IME file already exists at '{installationState.ExpectedSystemImePath}'.");
        }

        return new ImeDiagnosticReport(environment, sourceFile, peImage, imports, nativeProbe, installationState, findings);
    }

    internal static (ImePeDiagnostic Diagnostic, IReadOnlyList<string> Imports) ReadPe(string path)
    {
        try
        {
            var image = File.ReadAllBytes(path);
            using var stream = new MemoryStream(image, writable: false);
            using var reader = new PEReader(stream);
            var headers = reader.PEHeaders;
            var peHeader = headers.PEHeader ?? throw new BadImageFormatException("The file does not contain a PE optional header.");
            var imports = ReadImportNames(image, headers, peHeader.ImportTableDirectory.RelativeVirtualAddress);
            return (new ImePeDiagnostic(
                headers.CoffHeader.Machine.ToString(),
                peHeader.Magic.ToString(),
                peHeader.Subsystem.ToString(),
                (headers.CoffHeader.Characteristics & Characteristics.Dll) != 0,
                peHeader.ImportTableDirectory.RelativeVirtualAddress != 0,
                imports.Count,
                null), imports);
        }
        catch (Exception ex)
        {
            return (new ImePeDiagnostic("Unknown", "Unknown", "Unknown", false, false, 0, ex.Message), []);
        }
    }

    private static ImeEnvironmentDiagnostic CollectEnvironment()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return new ImeEnvironmentDiagnostic(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.Is64BitOperatingSystem,
            Environment.Is64BitProcess,
            Environment.ProcessPath ?? string.Empty,
            Environment.CurrentDirectory,
            windowsDirectory,
            Environment.SystemDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
    }

    private static ImeFileDiagnostic CollectFile(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return new ImeFileDiagnostic(fullPath, false, null, null, null, false, "File does not exist.", false, null, null);
        }

        var file = new FileInfo(fullPath);
        var canRead = false;
        string? readError = null;
        string? sha256 = null;
        try
        {
            using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
            canRead = true;
        }
        catch (Exception ex)
        {
            readError = $"{ex.GetType().Name}: {ex.Message}";
        }

        FileVersionInfo? version = null;
        try
        {
            version = FileVersionInfo.GetVersionInfo(fullPath);
        }
        catch
        {
        }

        return new ImeFileDiagnostic(
            fullPath,
            true,
            file.Length,
            sha256,
            file.Attributes.ToString(),
            canRead,
            readError,
            File.Exists(fullPath + ":Zone.Identifier"),
            version?.FileVersion,
            version?.ProductVersion);
    }

    private static IReadOnlyList<string> ReadImportNames(byte[] image, PEHeaders headers, int importRva)
    {
        if (importRva == 0)
        {
            return [];
        }

        var imports = new List<string>();
        var descriptorOffset = RvaToOffset(headers, importRva);
        while (descriptorOffset <= image.Length - 20)
        {
            var originalFirstThunk = ReadUInt32(image, descriptorOffset);
            var timeDateStamp = ReadUInt32(image, descriptorOffset + 4);
            var forwarderChain = ReadUInt32(image, descriptorOffset + 8);
            var nameRva = ReadUInt32(image, descriptorOffset + 12);
            var firstThunk = ReadUInt32(image, descriptorOffset + 16);
            if (originalFirstThunk == 0 && timeDateStamp == 0 && forwarderChain == 0 && nameRva == 0 && firstThunk == 0)
            {
                break;
            }
            if (nameRva == 0)
            {
                throw new BadImageFormatException("An import descriptor does not contain a module name RVA.");
            }
            imports.Add(ReadNullTerminatedAscii(image, RvaToOffset(headers, checked((int)nameRva))));
            descriptorOffset = checked(descriptorOffset + 20);
        }
        return imports.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static ImeImportDiagnostic ResolveImport(string module)
    {
        if (module.StartsWith("api-ms-win-", StringComparison.OrdinalIgnoreCase)
            || module.StartsWith("ext-ms-win-", StringComparison.OrdinalIgnoreCase))
        {
            return new ImeImportDiagnostic(module, "api-set", true, null, 0, null);
        }

        var handle = LoadLibraryEx(module, 0, LoadLibraryAsDatafile | LoadLibrarySearchSystem32);
        if (handle == 0)
        {
            var error = Marshal.GetLastPInvokeError();
            return new ImeImportDiagnostic(module, "native", false, null, error, GetErrorMessage(error));
        }

        try
        {
            var buffer = new char[32768];
            var length = GetModuleFileName(handle, buffer, buffer.Length);
            var path = length > 0 ? new string(buffer, 0, length) : null;
            return new ImeImportDiagnostic(module, "native", true, path, 0, null);
        }
        finally
        {
            FreeLibrary(handle);
        }
    }

    private static ImeNativeProbeDiagnostic ProbeNativeImage(string fullPath, bool exists)
    {
        if (!exists)
        {
            return new ImeNativeProbeDiagnostic(false, 2, GetErrorMessage(2));
        }

        var handle = LoadLibraryEx(fullPath, 0, DontResolveDllReferences);
        var mappingError = handle == 0 ? Marshal.GetLastPInvokeError() : 0;
        if (handle != 0)
        {
            FreeLibrary(handle);
        }

        return new ImeNativeProbeDiagnostic(
            handle != 0,
            mappingError,
            handle != 0 ? null : GetErrorMessage(mappingError));
    }

    private static ImeInstallationStateDiagnostic CollectInstallationState(string imeFileName)
    {
        var expectedPath = Path.Combine(Environment.SystemDirectory, imeFileName);
        var (writeProbeSucceeded, writeProbeErrorCode, writeProbeErrorMessage) = ProbeSystemDirectoryWrite();
        var matches = new List<ImeKeyboardLayoutDiagnostic>();
        if (OperatingSystem.IsWindows())
        {
            using var layouts = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts");
            if (layouts is not null)
            {
                foreach (var layoutId in layouts.GetSubKeyNames())
                {
                    using var layout = layouts.OpenSubKey(layoutId);
                    var imeFile = layout?.GetValue("Ime File") as string;
                    var layoutText = layout?.GetValue("Layout Text") as string;
                    if (string.Equals(imeFile, imeFileName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(layoutText, "XiaoXi IME", StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new ImeKeyboardLayoutDiagnostic(layoutId, layoutText, imeFile, layout?.GetValue("Layout File") as string));
                    }
                }
            }
        }

        return new ImeInstallationStateDiagnostic(
            expectedPath,
            File.Exists(expectedPath),
            File.Exists(expectedPath) ? new FileInfo(expectedPath).Length : null,
            writeProbeSucceeded,
            writeProbeErrorCode,
            writeProbeErrorMessage,
            matches);
    }

    private static (bool Succeeded, int ErrorCode, string? ErrorMessage) ProbeSystemDirectoryWrite()
    {
        var probePath = Path.Combine(Environment.SystemDirectory, $"XiaoXiIme.install-probe.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(probePath, [0x58, 0x49]);
            File.Delete(probePath);
            return (true, 0, null);
        }
        catch (Exception ex)
        {
            var errorCode = ex.HResult & 0xFFFF;
            return (false, errorCode, $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                File.Delete(probePath);
            }
            catch
            {
            }
        }
    }

    private static bool MachineMatchesProcess(string machine, string processArchitecture) =>
        (machine.Equals("Amd64", StringComparison.OrdinalIgnoreCase) && processArchitecture.Equals("X64", StringComparison.OrdinalIgnoreCase))
        || (machine.Equals("I386", StringComparison.OrdinalIgnoreCase) && processArchitecture.Equals("X86", StringComparison.OrdinalIgnoreCase))
        || (machine.Equals("Arm64", StringComparison.OrdinalIgnoreCase) && processArchitecture.Equals("Arm64", StringComparison.OrdinalIgnoreCase));

    private static int RvaToOffset(PEHeaders headers, int rva)
    {
        foreach (var section in headers.SectionHeaders)
        {
            var sectionSize = Math.Max(section.VirtualSize, section.SizeOfRawData);
            if (rva >= section.VirtualAddress && rva < section.VirtualAddress + sectionSize)
            {
                return checked(rva - section.VirtualAddress + section.PointerToRawData);
            }
        }
        throw new BadImageFormatException($"PE RVA 0x{rva:X8} is outside all sections.");
    }

    private static uint ReadUInt32(byte[] image, int offset)
    {
        if (offset < 0 || offset > image.Length - sizeof(uint))
        {
            throw new BadImageFormatException("The PE import table is outside the file.");
        }
        return BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset, sizeof(uint)));
    }

    private static string ReadNullTerminatedAscii(byte[] image, int offset)
    {
        var terminator = Array.IndexOf(image, (byte)0, offset);
        if (terminator < 0)
        {
            throw new BadImageFormatException("A PE import module name is not null-terminated.");
        }
        return System.Text.Encoding.ASCII.GetString(image, offset, terminator - offset);
    }

    private static string? GetErrorMessage(int errorCode) => errorCode == 0 ? null : new Win32Exception(errorCode).Message;

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string fileName, nint file, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetModuleFileName(nint module, char[] fileName, int size);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(nint module);
}
