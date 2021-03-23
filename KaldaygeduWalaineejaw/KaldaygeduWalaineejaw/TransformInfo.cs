using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    public class TransformInfo
    {
        public TransformInfo(CFStorage transformInfoStorage)
        {
            TransformInfoStorage = transformInfoStorage;
        }

        public CFStorage TransformInfoStorage { get; }

        public static TransformInfo? Load(CFStorage cfStorage)
        {
            if (cfStorage.TryGetStorage("TransformInfo", out var storage))
            {
                return new TransformInfo(storage);
            }

            return null;
        }
    }
}