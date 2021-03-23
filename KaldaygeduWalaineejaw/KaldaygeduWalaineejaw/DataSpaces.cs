using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    public class DataSpaces
    {
        public DataSpaces(CFStorage dataSpacesStorage)
        {
            DataSpacesStorage = dataSpacesStorage;

            DataSpaceMap = DataSpaceMap.LoadDataSpaceMap(dataSpacesStorage);
            DataSpaceInfo = DataSpaceInfo.Load(dataSpacesStorage);
            TransformInfo = TransformInfo.Load(dataSpacesStorage);
            Version = DataSpaceVersionInfo.Load(dataSpacesStorage);
        }

        public DataSpaceMap? DataSpaceMap { get; }

        public DataSpaceInfo? DataSpaceInfo { get; }

        public TransformInfo? TransformInfo { get; }

        public DataSpaceVersionInfo? Version { get; }

        public CFStorage DataSpacesStorage { get; }

        public static DataSpaces? Load(CompoundFile compoundFile)
        {
            if (compoundFile.RootStorage.TryGetStorage("\u0006DataSpaces", out CFStorage storage))
            {
                return new DataSpaces(storage);
            }

            return null;
        }
    }
}