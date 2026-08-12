using System.Buffers.Binary;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace XiaoXiIme.Cli;

[SupportedOSPlatform("windows")]
internal static class ImeBinaryValidator
{
    private const uint VftDrv = 0x00000003;
    private const uint Vft2DrvInputMethod = 0x0000000B;
    private static readonly string[] RequiredExports =
    [
        "ImeInquire",
        "ImeConfigure",
        "ImeConversionList",
        "ImeDestroy",
        "ImeEscape",
        "ImeProcessKey",
        "ImeRegisterWord",
        "ImeUnregisterWord",
        "ImeGetRegisterWordStyle",
        "ImeEnumRegisterWord",
        "ImeSelect",
        "ImeSetActiveContext",
        "ImeSetCompositionString",
        "ImeToAsciiEx",
        "NotifyIME",
    ];

    public static string? Validate(string imeFilePath)
    {
        if (!File.Exists(imeFilePath))
        {
            return $"IME file was not found: {imeFilePath}";
        }
        if (!TryReadVersionInfo(imeFilePath, out var versionInfo))
        {
            return "IME file does not contain readable VERSIONINFO.";
        }
        if (versionInfo.FileType != VftDrv || versionInfo.FileSubtype != Vft2DrvInputMethod)
        {
            return $"IME VERSIONINFO must use VFT_DRV/VFT2_DRV_INPUTMETHOD, but found file type 0x{versionInfo.FileType:X8} and subtype 0x{versionInfo.FileSubtype:X8}.";
        }

        try
        {
            var exports = ReadExportNames(imeFilePath);
            var missingExports = RequiredExports.Where(export => !exports.Contains(export)).ToArray();
            return missingExports.Length == 0
                ? null
                : $"IME module is missing required exports: {string.Join(", ", missingExports)}.";
        }
        catch (Exception ex)
        {
            return $"IME export table could not be read: {ex.Message}";
        }
    }

    private static HashSet<string> ReadExportNames(string path)
    {
        var image = File.ReadAllBytes(path);
        using var stream = new MemoryStream(image, writable: false);
        using var peReader = new PEReader(stream);
        var headers = peReader.PEHeaders;
        var exportDirectory = headers.PEHeader?.ExportTableDirectory
            ?? throw new BadImageFormatException("The file does not contain a PE header.");
        if (exportDirectory.RelativeVirtualAddress == 0)
        {
            return [];
        }

        var exportOffset = RvaToOffset(headers, exportDirectory.RelativeVirtualAddress);
        var numberOfNames = ReadUInt32(image, exportOffset + 24);
        var addressOfNames = ReadUInt32(image, exportOffset + 32);
        var namesOffset = RvaToOffset(headers, checked((int)addressOfNames));
        var exports = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < numberOfNames; index++)
        {
            var nameRva = ReadUInt32(image, checked(namesOffset + (index * sizeof(uint))));
            var nameOffset = RvaToOffset(headers, checked((int)nameRva));
            var terminator = Array.IndexOf(image, (byte)0, nameOffset);
            if (terminator < 0)
            {
                throw new BadImageFormatException("The PE export name is not null-terminated.");
            }
            exports.Add(Encoding.ASCII.GetString(image, nameOffset, terminator - nameOffset));
        }
        return exports;
    }

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
            throw new BadImageFormatException("The PE export table is outside the file.");
        }
        return BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset, sizeof(uint)));
    }

    private static bool TryReadVersionInfo(string path, out VsFixedFileInfo versionInfo)
    {
        versionInfo = default;
        var ignored = 0u;
        var size = GetFileVersionInfoSize(path, ref ignored);
        if (size == 0)
        {
            return false;
        }

        var buffer = new byte[size];
        if (!GetFileVersionInfo(path, 0, size, buffer)
            || !VerQueryValue(buffer, "\\", out var value, out var valueLength)
            || valueLength < Marshal.SizeOf<VsFixedFileInfo>())
        {
            return false;
        }

        versionInfo = Marshal.PtrToStructure<VsFixedFileInfo>(value);
        return versionInfo.Signature == 0xFEEF04BD;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VsFixedFileInfo
    {
        public uint Signature;
        public uint StructureVersion;
        public uint FileVersionMs;
        public uint FileVersionLs;
        public uint ProductVersionMs;
        public uint ProductVersionLs;
        public uint FileFlagsMask;
        public uint FileFlags;
        public uint FileOs;
        public uint FileType;
        public uint FileSubtype;
        public uint FileDateMs;
        public uint FileDateLs;
    }

    [DllImport("version.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFileVersionInfoSize(string fileName, ref uint handle);

    [DllImport("version.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetFileVersionInfo(string fileName, uint handle, uint length, byte[] data);

    [DllImport("version.dll", CharSet = CharSet.Unicode)]
    private static extern bool VerQueryValue(byte[] block, string subBlock, out nint value, out uint length);
}
