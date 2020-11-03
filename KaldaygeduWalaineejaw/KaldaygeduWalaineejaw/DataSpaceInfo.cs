using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    public class DataSpaceInfo
    {
        public DataSpaceInfo(CFStorage dataSpaceInfoStorage)
        {
            DataSpaceInfoStorage = dataSpaceInfoStorage;
            DrmEncryptedDataSpace = DRMEncryptedDataSpace.Load(dataSpaceInfoStorage);
        }

        public CFStorage DataSpaceInfoStorage { get; }

        public DRMEncryptedDataSpace? DrmEncryptedDataSpace { get; }

        public static DataSpaceInfo? Load(CFStorage cfStorage)
        {
            if (cfStorage.TryGetStorage("DataSpaceInfo", out var storage))
            {
                return new DataSpaceInfo(storage);
            }

            return null;
        }
    }
}