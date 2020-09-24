using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    class EncryptedPackage
    {
        public EncryptedPackage(CFStream encryptedPackageStream)
        {
            EncryptedPackageStream = encryptedPackageStream;
        }

        private CFStream EncryptedPackageStream { get; }

        public static EncryptedPackage? Load(CompoundFile compoundFile)
        {
            if (compoundFile.RootStorage.TryGetStream(Name, out var stream))
            {
                return new EncryptedPackage(stream);
            }

            return null;
        }

        private const string Name = "EncryptedPackage";
    }
}