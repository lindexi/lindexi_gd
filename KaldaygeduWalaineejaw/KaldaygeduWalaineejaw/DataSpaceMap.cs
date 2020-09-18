using System.Diagnostics;
using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    /// <summary>
    /// The DataSpaceMap structure associates protected content with data space definitions. The data space definition, in turn, describes the series of transforms that MUST be applied to that protected content to restore it to its original form. 
    /// By using a map to associate data space definitions with content, a single data space definition can be used to define the transforms applied to more than one piece of protected content. However, a given piece of protected content can be referenced only by a single data space definition. 
    /// </summary>
    public class DataSpaceMap
    {
        public DataSpaceMap(CFStream dataSpaceMapStream)
        {
            DataSpaceMapStream = dataSpaceMapStream;
            var binaryReader = dataSpaceMapStream.ToBinaryReader();
            HeaderLength = binaryReader.ReadUInt16();
            Debug.Assert(HeaderLength == 8);
            EntryCount = binaryReader.ReadUInt16();
        }

        public CFStream DataSpaceMapStream { get; }

        public static DataSpaceMap? LoadDataSpaceMap(CFStorage cfStorage)
        {
            if (cfStorage.TryGetStream("DataSpaceMap", out var cfStream))
            {
                return new DataSpaceMap(cfStream);
            }

            return null;
        }

        /// <summary>
        /// An unsigned integer that specifies the number of bytes in the DataSpaceMap structure before the first entry in the MapEntries array. It MUST be equal to 0x00000008.
        /// </summary>
        public ushort HeaderLength { get; }

        /// <summary>
        /// An unsigned integer that specifies the number of DataSpaceMapEntry items (section 2.1.6.1) in the MapEntries array. 
        /// </summary>
        public ushort EntryCount { get; }

        //todo
        public DataSpaceMapEntry[] MapEntries { get; } = new DataSpaceMapEntry[0];
    }
}