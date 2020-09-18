using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    public class DRMEncryptedDataSpace
    {
        public DRMEncryptedDataSpace(CFStream drmEncryptedDataSpaceStream)
        {
            DrmEncryptedDataSpaceStream = drmEncryptedDataSpaceStream;
        }

        public CFStream DrmEncryptedDataSpaceStream { get; }

        public static DRMEncryptedDataSpace? Load(CFStorage cfStorage)
        {
            if (cfStorage.TryGetStream("DRMEncryptedDataSpace", out var stream))
            {
                return new DRMEncryptedDataSpace(stream);
            }

            return null;
        }
    }
}