using System.IO;
using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    static class CFStreamExtension
    {
        public static Stream ToStream(this CFStream stream) => new DataStream(stream);

        public static BinaryReader ToBinaryReader(this CFStream stream) => new BinaryReader(stream.ToStream());
    }
}