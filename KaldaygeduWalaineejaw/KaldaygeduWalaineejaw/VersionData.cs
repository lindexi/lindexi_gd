using System.IO;

namespace KaldaygeduWalaineejaw
{
    public class VersionData
    {
        public VersionData(BinaryReader reader)
        {
            Major = reader.ReadUInt16();
            Minor = reader.ReadUInt16();
        }

        public ushort Major { get; }

        public ushort Minor { get; }
    }
}