namespace DotNetCampus.Storage.Documents.StorageDocuments;

public class StorageReferenceNotFoundException : Exception
{
    public StorageReferenceNotFoundException(StorageReferenceId referenceId, IReferencedManager referencedManager)
        : base($"Storage reference not found: {referenceId}")
    {
        ReferencedManager = referencedManager;
    }

    public IReferencedManager ReferencedManager { get; }
}