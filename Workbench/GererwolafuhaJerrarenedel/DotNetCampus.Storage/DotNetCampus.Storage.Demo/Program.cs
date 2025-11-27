// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using System.Xml.Linq;

using DotNetCampus.Storage;
using DotNetCampus.Storage.Demo;
using DotNetCampus.Storage.Demo.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Demo.SaveInfos;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.Serialization;
using DotNetCampus.Storage.Serialization.XmlSerialization;
using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

var testFile = Path.Join(AppContext.BaseDirectory, "Asserts", "TestFiles", "Test.opc");

var compoundStorageDocumentManager = new FakeCompoundStorageDocumentManager();

var storageModel = await compoundStorageDocumentManager.ReadStorageModelFromOpcFile<FakeStorageModel>(new FileInfo(testFile));

if (storageModel != null)
{
    var testOutputFile = Path.Join(AppContext.BaseDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss}.opc");
    await compoundStorageDocumentManager.SaveToOpcFileAsync(storageModel, new FileInfo(testOutputFile));
}

var parserManager = compoundStorageDocumentManager.ParserManager;

var fooSaveInfo = new FooSaveInfo()
{
    FooProperty = Random.Shared.Next(),
    Foo1 = new Foo1SaveInfo()
    {
        Foo1Property = true,
        Foo2Property = Random.Shared.Next()
    },
    CountList = ["123", "abc"]
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

var storageXmlSerializer = new StorageXmlSerializer();
var xDocument = storageXmlSerializer.Serialize(storageNode);
var xml = xDocument.ToString(SaveOptions.None);

Console.WriteLine("Hello, World!");



