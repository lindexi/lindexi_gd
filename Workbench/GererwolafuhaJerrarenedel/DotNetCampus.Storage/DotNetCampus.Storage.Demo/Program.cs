// See https://aka.ms/new-console-template for more information

using DotNetCampus.Storage;
using DotNetCampus.Storage.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Demo;
using DotNetCampus.Storage.Demo.SaveInfos;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.StorageNodes;

var testFile = @"C:\lindexi\Test.opc";

if (!File.Exists(testFile))
{
    return;
}

var builder = CompoundStorageDocumentManager.CreateBuilder();
builder.UseStorageModelToCompoundDocumentConverter(provider =>
    new FakeStorageModelToCompoundDocumentConverter(provider));

var compoundStorageDocumentManager = builder.Build();

var storageModel = await compoundStorageDocumentManager.ReadStorageModelFromOpcFile<FakeStorageModel>(new FileInfo(testFile));

var parserManager = compoundStorageDocumentManager.ParserManager;
StorageNodeParserManagerCollection.RegisterSaveInfoNodeParser(parserManager);

var fooSaveInfo = new FooSaveInfo()
{
    FooProperty = Random.Shared.Next(),
    Foo1 = new Foo1SaveInfo()
    {
        Foo1Property = true,
        Foo2Property = Random.Shared.Next()
    }
};

var nodeParser = parserManager.GetNodeParser(fooSaveInfo.GetType());
var storageNode = nodeParser.Deparse(fooSaveInfo, new DeparseNodeContext()
{
    NodeName = null,
    DocumentManager = compoundStorageDocumentManager,
});

var foo1SaveInfo = new Foo1SaveInfo()
{
    Foo2Property = Random.Shared.Next(),
};
var extensionStorageNode = parserManager.GetNodeParser(foo1SaveInfo.GetType()).Deparse(foo1SaveInfo, new DeparseNodeContext()
{
    NodeName = null,
    DocumentManager = compoundStorageDocumentManager,
});
storageNode.Children ??= new List<StorageNode>();
storageNode.Children.Add(extensionStorageNode);

// 再添加一些未知属性
var unknownStorageNode = new StorageNode()
{
    Name = "UnknownProperty1",
    Value = "SomeValue",
};
storageNode.Children.Add(unknownStorageNode);

var parsedFooSaveInfo = nodeParser.Parse(storageNode, new ParseNodeContext()
{
    DocumentManager = compoundStorageDocumentManager,
}) as FooSaveInfo;

Console.WriteLine("Hello, World!");



class FakeStorageModel : StorageModel
{
    public TestDocumentSaveInfo? Document { get; set; }
}

[SaveInfoContract("Document")]
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