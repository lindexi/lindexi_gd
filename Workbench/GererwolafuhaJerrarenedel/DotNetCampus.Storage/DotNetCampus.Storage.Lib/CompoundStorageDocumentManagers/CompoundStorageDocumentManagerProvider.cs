namespace DotNetCampus.Storage.CompoundStorageDocumentManagers;

public class CompoundStorageDocumentManagerProvider
{
    internal CompoundStorageDocumentManager? Manager { get; set; }

    public CompoundStorageDocumentManager GetManager()
    {
        if (Manager == null)
        {
            throw new InvalidOperationException($"Manager is null");
        }

        return Manager;
    }
}