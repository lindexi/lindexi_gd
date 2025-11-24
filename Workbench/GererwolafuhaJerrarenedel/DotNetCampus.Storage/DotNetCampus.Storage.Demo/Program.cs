// See https://aka.ms/new-console-template for more information

using DotNetCampus.Storage.Demo;
using DotNetCampus.Storage.Demo.SaveInfos;
using DotNetCampus.Storage.Lib.Parsers;
using DotNetCampus.Storage.Lib.Parsers.Contexts;
using DotNetCampus.Storage.Lib.StorageNodes;

var storableNodeParserManager = new StorableNodeParserManager();
storableNodeParserManager.Register(new Foo1SaveInfoNodeParser());
storableNodeParserManager.Register(new FooSaveInfoNodeParser());

var fooSaveInfo = new FooSaveInfo()
{
    FooProperty = Random.Shared.Next(),
    Foo1 = new Foo1SaveInfo()
    {
        Foo1Property = true,
        Foo2Property = Random.Shared.Next()
    }
};

var nodeParser = storableNodeParserManager.GetNodeParser(fooSaveInfo.GetType());
var storageNode = nodeParser.Deparse(fooSaveInfo, new DeparseNodeContext()
{
    NodeName = null,
    ParserManager = storableNodeParserManager
});

var foo1SaveInfo = new Foo1SaveInfo()
{
    Foo2Property = Random.Shared.Next(),
};
var extensionStorageNode = storableNodeParserManager.GetNodeParser(foo1SaveInfo.GetType()).Deparse(foo1SaveInfo, new DeparseNodeContext()
{
    NodeName = null,
    ParserManager = storableNodeParserManager
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
    ParserManager = storableNodeParserManager
}) as FooSaveInfo;

Console.WriteLine("Hello, World!");

internal static partial class StorableNodeParserManagerCollection
{
    public static partial void RegisterSaveInfoNodeParser(StorableNodeParserManager parserManager);

    public static partial void RegisterSaveInfoNodeParser(StorableNodeParserManager parserManager)
    {

    }
}