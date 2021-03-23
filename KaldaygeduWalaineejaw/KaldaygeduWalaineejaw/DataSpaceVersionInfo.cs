using System.Diagnostics;
using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    public class DataSpaceVersionInfo
    {
        public DataSpaceVersionInfo(CFStream versionStream)
        {
            VersionStream = versionStream;

            var binaryReader = versionStream.ToBinaryReader();

            FeatureIdentifier = new LengthPrefixPaddedUnicodeString(binaryReader);
            Debug.Assert(FeatureIdentifier.Text == "Microsoft.Container.DataSpaces");

            ReaderVersion = new VersionData(binaryReader);
            UpdaterVersion = new VersionData(binaryReader);
            WriterVersion = new VersionData(binaryReader);
        }

        public CFStream VersionStream { get; }

        public static DataSpaceVersionInfo? Load(CFStorage cfStorage)
        {
            if (cfStorage.TryGetStream("Version", out var stream))
            {
                return new DataSpaceVersionInfo(stream);
            }

            return null;
        }

        // It MUST be "Microsoft.Container.DataSpaces". 
        public LengthPrefixPaddedUnicodeString FeatureIdentifier { get; }

        // ReaderVersion.vMajor MUST be 1. ReaderVersion.vMinor MUST be 0
        public VersionData ReaderVersion { get; }

        // UpdaterVersion.vMajor MUST be 1. UpdaterVersion.vMinor MUST be 0
        public VersionData UpdaterVersion { get; }

        // WriterVersion.vMajor MUST be 1. WriterVersion.vMinor MUST be 0
        public VersionData WriterVersion { get; }
    }
}