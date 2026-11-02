using System.Buffers.Binary;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace Builder;

internal sealed record PeReport(string Path, string Sha256, string Machine, bool HasClr, int EntryPoint,
    string Subsystem, string SubsystemVersion, string OsVersion, bool HeaderMatchesTarget, string ImportValidation);

internal static class BinaryInspection
{
    internal static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static void ValidateObject(string path, string arch)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[20];
        stream.ReadExactly(header);
        ushort machine = BinaryPrimitives.ReadUInt16LittleEndian(header);
        ushort sections = BinaryPrimitives.ReadUInt16LittleEndian(header[2..]);
        ushort optional = BinaryPrimitives.ReadUInt16LittleEndian(header[16..]);
        if (machine != (arch == "x64" ? 0x8664 : 0x14c) || sections == 0 || optional != 0 || stream.Length < 20L + sections * 40L)
            throw new InvalidDataException(Text.Get("Object"));
    }

    internal static PeReport Inspect(string path, string arch)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var h = pe.PEHeaders;
        var p = h.PEHeader ?? throw new BadImageFormatException(Text.Get("Pe"));
        bool matches = h.CoffHeader.Machine == (arch == "x64" ? Machine.Amd64 : Machine.I386) &&
            h.CorHeader is null && p.Magic == (arch == "x64" ? PEMagic.PE32Plus : PEMagic.PE32) &&
            p.Subsystem is Subsystem.WindowsCui or Subsystem.WindowsGui && p.MajorSubsystemVersion <= 6 &&
            (p.MajorSubsystemVersion < 6 || p.MinorSubsystemVersion <= 1) &&
            (p.MajorOperatingSystemVersion < 6 || p.MajorOperatingSystemVersion == 6 && p.MinorOperatingSystemVersion <= 1);
        return new(path, Hash(path), h.CoffHeader.Machine.ToString(), h.CorHeader is not null,
            p.AddressOfEntryPoint, p.Subsystem.ToString(), $"{p.MajorSubsystemVersion}.{p.MinorSubsystemVersion}",
            $"{p.MajorOperatingSystemVersion}.{p.MinorOperatingSystemVersion}", matches, "NotImplemented");
    }
}
