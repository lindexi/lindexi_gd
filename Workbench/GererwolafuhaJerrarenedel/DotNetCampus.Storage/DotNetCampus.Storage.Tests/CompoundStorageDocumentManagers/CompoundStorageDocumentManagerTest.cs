using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;

namespace DotNetCampus.Storage.Tests.CompoundStorageDocumentManagers;

public class CompoundStorageDocumentManagerTest
{
    public static CompoundStorageDocumentManager GetTestManager()
    {
        var compoundStorageDocumentManager = new CompoundStorageDocumentManager()
        {
            ReferencedFileManager = null!,
            StorageFileManager = null!,
            StorageModelToCompoundDocumentConverter = null!,
        };

        return compoundStorageDocumentManager;
    }
}

class FakeStorageModel : StorageModel
{

}

class FakeStorageModelToCompoundDocumentConverter: StorageModelToCompoundDocumentConverter
{
    public FakeStorageModelToCompoundDocumentConverter(CompoundStorageDocumentManager manager) : base(manager)
    {
    }

    public override StorageModel ToStorageModel(CompoundStorageDocument document)
    {
        throw new System.NotImplementedException();
    }

    public override CompoundStorageDocument ToCompoundDocument(StorageModel model)
    {
        throw new System.NotImplementedException();
    }
}