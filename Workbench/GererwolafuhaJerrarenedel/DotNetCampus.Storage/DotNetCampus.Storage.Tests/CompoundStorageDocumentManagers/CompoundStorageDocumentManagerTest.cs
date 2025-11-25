using DotNetCampus.Storage.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;

namespace DotNetCampus.Storage.Tests.CompoundStorageDocumentManagers;

public class CompoundStorageDocumentManagerTest
{
    public static CompoundStorageDocumentManager GetTestManager()
    {
        var builder = CompoundStorageDocumentManager.CreateBuilder();
        builder.UseStorageModelToCompoundDocumentConverter(provider => new FakeStorageModelToCompoundDocumentConverter(provider));

        var compoundStorageDocumentManager = builder.Build();

        return compoundStorageDocumentManager;
    }
}

class FakeStorageModel : StorageModel
{

}

class FakeStorageModelToCompoundDocumentConverter : StorageModelToCompoundDocumentConverter
{
    public FakeStorageModelToCompoundDocumentConverter(CompoundStorageDocumentManagerProvider provider) : base(provider)
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