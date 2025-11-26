// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
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
using DotNetCampus.Storage.Serialization;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

var testFile = @"C:\lindexi\Test.opc";

if (!File.Exists(testFile))
{
    return;
}

var builder = CompoundStorageDocumentManager.CreateBuilder();
builder
    .UseStorageModelToCompoundDocumentConverter(provider =>
    new FakeStorageModelToCompoundDocumentConverter(provider))
    .UseParserManager(StorageNodeParserManagerCollection.RegisterSaveInfoNodeParser)
    ;

builder.CompoundStorageDocumentSerializer = new FakeCompoundStorageDocumentSerializer(builder.ManagerProvider);

var compoundStorageDocumentManager = builder.Build();

var storageModel = await compoundStorageDocumentManager.ReadStorageModelFromOpcFile<FakeStorageModel>(new FileInfo(testFile));

var parserManager = compoundStorageDocumentManager.ParserManager;

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






class FakeStorageModelToCompoundDocumentConverter : StorageModelToCompoundDocumentConverter
{
    public FakeStorageModelToCompoundDocumentConverter(CompoundStorageDocumentManagerProvider provider) : base(provider)
    {
    }

    public override StorageModel ToStorageModel(CompoundStorageDocument document)
    {
        var fakeStorageModel = new FakeStorageModel()
        {
            Document = ReadRootSaveInfoProperty<TestDocumentSaveInfo>(document,"Document.xml"),
            Presentation = ReadRootSaveInfoProperty<PresentationSaveInfo>(document, "Presentation.xml"),
            SlideList = ReadRootSaveInfoPropertyList<SlideSaveInfo>(document, path =>
            {
                var relativePath = path.RelativePath;
                if (relativePath.Contains('\\') || relativePath.Contains('/'))
                {
                    if (Path.GetDirectoryName(relativePath.AsSpan()) is "Slides")
                    {
                        var fileName = Path.GetFileName(relativePath.AsSpan());
                        return Regex.IsMatch(fileName, @"Slide\d+\.xml");
                    }
                }

                return false;
            }).ToList()
        };
        return fakeStorageModel;
    }

    public override CompoundStorageDocument ToCompoundDocument(StorageModel model)
    {
        throw new System.NotImplementedException();
    }
}

public class FakeCompoundStorageDocumentSerializer : CompoundStorageDocumentSerializer
{
    public FakeCompoundStorageDocumentSerializer(CompoundStorageDocumentManagerProvider provider) : base(provider)
    {
    }

    protected override void AddResourceReference(StorageNode referenceStorageNode)
    {
        var referencedManager = Manager.ReferencedManager;

        List<ReferenceInfo>? referenceInfoList = null;
        var storageReferenceSaveInfo = ReadAsPropertyValue<StorageReferenceSaveInfo>(referenceStorageNode);
        if (storageReferenceSaveInfo.Relationships is {} list)
        {
            referenceInfoList = new List<ReferenceInfo>(list.Count);

            foreach (StorageRelationshipsSaveInfo relationshipsSaveInfo in list)
            {
                var id = relationshipsSaveInfo.Id;
                var target = relationshipsSaveInfo.Target;

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(target))
                {
                    continue;
                }

                referenceInfoList.Add(new ReferenceInfo()
                {
                    ReferenceId = new StorageReferenceId(id),
                    FilePath = new StorageFileRelativePath(target)
                });
            }
        }

        referencedManager.Reset(referenceInfoList);
    }

    private T ReadAsPropertyValue<T>(StorageNode storageNode)
    {
        var parserManager = Manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));
        var value = nodeParser.Parse(storageNode, new ParseNodeContext()
        {
            DocumentManager = Manager
        });
        return (T) value;
    }
}