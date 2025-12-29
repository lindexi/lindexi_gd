using System.IO;
using System.Threading.Tasks;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.Serialization;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Tests.CompoundStorageDocumentManagers;

[TestClass]
public class CompoundStorageDocumentManagerTest
{
    [TestMethod]
    public void TestCompoundStorageDocumentManager()
    {
        var testFile = @"C:\lindexi\Test.opc";

        if (!File.Exists(testFile))
        {
            return;
        }

        var compoundStorageDocumentManager = GetTestManager();
    }

    public static CompoundStorageDocumentManager GetTestManager()
    {
        return new FakeCompoundStorageDocumentManager();
    }
}

class FakeCompoundStorageDocumentManager : CompoundStorageDocumentManager
{
    public FakeCompoundStorageDocumentManager()
    {
        StorageModelToCompoundDocumentConverter = new FakeStorageModelToCompoundDocumentConverter(this);
        CompoundStorageDocumentSerializer = new FakeCompoundStorageDocumentSerializer(this);
    }

    public override IStorageModelToCompoundDocumentConverter StorageModelToCompoundDocumentConverter { get; }
    public override ICompoundStorageDocumentSerializer CompoundStorageDocumentSerializer { get; }
}

class FakeStorageModel : StorageModel
{
    public TestDocumentSaveInfo? Document { get; set; }
}

class TestDocumentSaveInfo : SaveInfo
{
    [SaveInfoMember("Name")]
    public string? Name { get; set; }

    [SaveInfoMember("Creator")]
    public string? Creator { get; set; }

    [SaveInfoMember("DocumentVersion")]
    public string? DocumentVersion { get; set; }
}

class FakeStorageModelToCompoundDocumentConverter : StorageModelToCompoundDocumentConverter
{
    public FakeStorageModelToCompoundDocumentConverter(CompoundStorageDocumentManager manager) : base(manager)
    {
    }


    public override Task<StorageModel> ToStorageModel(CompoundStorageDocument document)
    {
        throw new System.NotImplementedException();
    }

    public override Task<CompoundStorageDocument> ToCompoundDocument(StorageModel model)
    {
        throw new System.NotImplementedException();
    }
}

public class FakeCompoundStorageDocumentSerializer : CompoundStorageDocumentSerializer
{
    public FakeCompoundStorageDocumentSerializer(CompoundStorageDocumentManager manager) : base(manager)
    {
    }

    protected override Task AddResourceReferenceAsync(StorageNode referenceStorageNode, IReferencedManager referencedManager)
    {
        return Task.CompletedTask;
    }

    protected override async Task<StorageNode?> CreateReferenceStorageNodeAsync(IReferencedManager referencedManager)
    {
        await Task.CompletedTask;
        return null;
    }
}