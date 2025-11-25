using System.IO;
using DotNetCampus.Storage.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.SaveInfos;

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
        var builder = CompoundStorageDocumentManager.CreateBuilder();
        builder.UseStorageModelToCompoundDocumentConverter(provider => new FakeStorageModelToCompoundDocumentConverter(provider));

        var compoundStorageDocumentManager = builder.Build();

        return compoundStorageDocumentManager;
    }
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