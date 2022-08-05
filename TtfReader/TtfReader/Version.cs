namespace TtfReader;

public readonly record struct Version(ushort Major, ushort Minor)
{
    public static Version Read(BinaryReader reader)
    {
        var major = reader.ReadUInt16();
        var minor = reader.ReadUInt16();
        return new Version(major, minor);
    }

    public override string ToString() => $"{Major}.{Minor}";
}