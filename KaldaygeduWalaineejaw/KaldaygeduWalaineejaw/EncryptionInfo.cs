using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    public class EncryptionInfo
    {
        public CFStream EncryptionInfoStream { get; }

        private EncryptionInfo(CFStream stream)
        {

        }

        protected EncryptionInfo(VersionData encryptionVersionInfo)
        {
            EncryptionVersionInfo = encryptionVersionInfo;
        }

        public VersionData EncryptionVersionInfo { get; }

        public static EncryptionInfo? Load(CompoundFile compoundFile)
        {
            if (compoundFile.RootStorage.TryGetStream("EncryptionInfo", out var stream))
            {
                var binaryReader = stream.ToBinaryReader();

                var encryptionVersionInfo = new VersionData(binaryReader);

                if (encryptionVersionInfo.Minor == 0x0002)
                {
                    // Standard Encryption
                }
                else if (encryptionVersionInfo.Minor == 0x0003)
                {
                    // Extensible Encryption
                }
                else if (encryptionVersionInfo.Minor == 0x0004)
                {
                    // Agile Encryption
                    return new AgileEncryptionInfo(encryptionVersionInfo, binaryReader);
                }

                return new EncryptionInfo(stream);
            }

            return null;
        }
    }
}