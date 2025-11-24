using DotNetCampus.Storage.Lib.Parsers;
using DotNetCampus.Storage.Lib.SaveInfos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Storage.Lib.Parsers.Contexts;
using DotNetCampus.Storage.Lib.Parsers.NodeParsers;
using DotNetCampus.Storage.Lib.StorageNodes;

namespace DotNetCampus.Storage.Tests.Parsers;

[TestClass]
public class SaveInfoToStorageNodeTests
{
    [TestMethod]
    public void TestMethod1()
    {
        var storableNodeParserManager = new StorableNodeParserManager();
        storableNodeParserManager.Register(new Foo1SaveInfoNodeParser());

        var foo1SaveInfo = new Foo1SaveInfo()
        {
            Foo2Property = Random.Shared.Next()
        };
        var nodeParser = storableNodeParserManager.GetNodeParser(foo1SaveInfo.GetType());
        var storageNode = nodeParser.Deparse(foo1SaveInfo, new DeparseNodeContext()
        {
            NodeName = null,
            ParserManager = storableNodeParserManager
        });

        var parsedFoo1SaveInfo = nodeParser.Parse(storageNode, new ParseNodeContext()
        {
            ParserManager = storableNodeParserManager
        }) as Foo1SaveInfo;
        Assert.IsNotNull(parsedFoo1SaveInfo);
        Assert.AreEqual(foo1SaveInfo.Foo1Property, parsedFoo1SaveInfo.Foo1Property);
        Assert.AreEqual(foo1SaveInfo.Foo2Property, parsedFoo1SaveInfo.Foo2Property);
    }

    [TestMethod]
    public void TestMethod2()
    {
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
        Assert.IsNotNull(parsedFooSaveInfo);
        Assert.AreEqual(fooSaveInfo.FooProperty, parsedFooSaveInfo.FooProperty);
        Assert.IsNotNull(parsedFooSaveInfo.Foo1);
        Assert.AreEqual(fooSaveInfo.Foo1.Foo1Property, parsedFooSaveInfo.Foo1.Foo1Property);
        Assert.AreEqual(fooSaveInfo.Foo1.Foo2Property, parsedFooSaveInfo.Foo1.Foo2Property);

        // 验证扩展属性
        Assert.AreEqual(1, parsedFooSaveInfo.Extensions.Count);
        var parsedExtension = parsedFooSaveInfo.Extensions[0] as Foo1SaveInfo;
        Assert.IsNotNull(parsedExtension);
        Assert.AreEqual(foo1SaveInfo.Foo2Property, parsedExtension.Foo2Property);

        // 验证未知属性
        Assert.IsNotNull(parsedFooSaveInfo.UnknownProperties);
        Assert.AreEqual(1, parsedFooSaveInfo.UnknownProperties.Count);
        Assert.AreEqual(unknownStorageNode.Name.ToText(), parsedFooSaveInfo.UnknownProperties[0].Name.ToText());
        Assert.AreEqual(unknownStorageNode.Value.ToText(), parsedFooSaveInfo.UnknownProperties[0].Value.ToText());

        // 继续转换为 StorageNode 可以不丢失扩展和未知属性
        var reDeparsedStorageNode = nodeParser.Deparse(parsedFooSaveInfo, new DeparseNodeContext()
        {
            NodeName = null,
            ParserManager = storableNodeParserManager
        });
        Assert.IsNotNull(reDeparsedStorageNode.Children);
        // 能够不丢失扩展属性
        var extensionNode = reDeparsedStorageNode.Children.FirstOrDefault(t => t.Name.ToText() == "Foo1");
        Assert.IsNotNull(extensionNode);
        // 能够不丢失未知属性
        var unknownNode = reDeparsedStorageNode.Children.FirstOrDefault(t => t.Name.ToText() == unknownStorageNode.Name.ToText());
        Assert.IsNotNull(unknownNode);
    }
}

[SaveInfoContract("Foo")]
public class FooSaveInfo : SaveInfo
{
    [SaveInfoMember("FooProperty", Description = "This is a foo property.")]
    public int FooProperty { get; set; } = 2;

    [SaveInfoMember("F1", Description = "This is a foo property.")]
    public Foo1SaveInfo? Foo1 { get; set; }
}

public class FooSaveInfoNodeParser : SaveInfoNodeParser<FooSaveInfo>
{
    public override SaveInfoContractAttribute ContractAttribute => _contractAttribute ??= new SaveInfoContractAttribute("Foo");
    private SaveInfoContractAttribute? _contractAttribute;

    protected override FooSaveInfo ParseCore(StorageNode node, in ParseNodeContext context)
    {
        StorableNodeParserManager parserManager = context.ParserManager;
        // 决定不支持 init 的情况，这样才能更好地保留默认值
        var fooSaveInfo = new FooSaveInfo();
        if (node.Children is { } children)
        {
            List<StorageNode>? unknownNodeList = null;
            foreach (var storageNode in children)
            {
                var currentName = storageNode.Name.AsSpan();
                var propertyNameForFooProperty = "FooProperty";
                if (currentName.Equals(propertyNameForFooProperty, StringComparison.Ordinal))
                {
                    var typeOfFooProperty = typeof(int);
                    var nodeParserForFooProperty = parserManager.GetNodeParser(typeOfFooProperty);
                    var valueForFooProperty = nodeParserForFooProperty.Parse(storageNode, context);
                    fooSaveInfo.FooProperty = (int) valueForFooProperty;
                    continue;
                }

                var propertyNameForFoo1 = "F1";
                if (currentName.Equals(propertyNameForFoo1, StringComparison.Ordinal))
                {
                    var typeOfFoo1 = typeof(Foo1SaveInfo);
                    var nodeParserForFoo1 = parserManager.GetNodeParser(typeOfFoo1);
                    var valueForFoo1 = nodeParserForFoo1.Parse(storageNode, context);
                    fooSaveInfo.Foo1 = (Foo1SaveInfo) valueForFoo1;
                    continue;
                }

                unknownNodeList ??= new List<StorageNode>();
                unknownNodeList.Add(storageNode);
            }

            if (unknownNodeList != null)
            {
                FillExtensionAndUnknownProperties(unknownNodeList, fooSaveInfo, in context);
            }
        }
        return fooSaveInfo;
    }

    protected override StorageNode DeparseCore(FooSaveInfo obj, in DeparseNodeContext context)
    {
        StorableNodeParserManager parserManager = context.ParserManager;
        var storageNode = new StorageNode();
        const int saveInfoMemberCount = 2;
        storageNode.Name = context.NodeName ?? TargetStorageName;
        storageNode.Children = new List<StorageNode>(saveInfoMemberCount);
        DeparseNodeContext tempContext;
        var propertyNameForFooProperty = "FooProperty";
        var typeOfFooProperty = typeof(int);
        var nodeParserForFooProperty = parserManager.GetNodeParser(typeOfFooProperty);
        object? valueForFooProperty = obj.FooProperty;
        if (valueForFooProperty is not null)
        {
            tempContext = context with
            {
                NodeName = propertyNameForFooProperty
            };
            var childNodeForFooProperty = nodeParserForFooProperty.Deparse(valueForFooProperty, tempContext);
            storageNode.Children.Add(childNodeForFooProperty);
        }

        var propertyNameForFoo1 = "F1";
        var typeOfFoo1 = typeof(Foo1SaveInfo);
        var nodeParserForFoo1 = parserManager.GetNodeParser(typeOfFoo1);
        object? valueForFoo1 = obj.Foo1;
        if (valueForFoo1 is not null)
        {
            tempContext = context with
            {
                NodeName = propertyNameForFoo1
            };
            var childNodeForFoo1 = nodeParserForFoo1.Deparse(valueForFoo1, tempContext);
            storageNode.Children.Add(childNodeForFoo1);
        }

        AppendExtensionAndUnknownProperties(storageNode, obj, in context);

        return storageNode;
    }
}

[SaveInfoContract("Foo1")]
public class Foo1SaveInfo : SaveInfo
{
    [SaveInfoMember("Foo1Property", Description = "This is a foo1 property.")]
    public bool Foo1Property { get; set; } = false;

    [SaveInfoMember("Foo2Property", Description = "This is a foo2 property.")]
    public int Foo2Property { get; set; } = 3;
}

public class Foo1SaveInfoNodeParser : SaveInfoNodeParser<Foo1SaveInfo>
{
    public override SaveInfoContractAttribute ContractAttribute { get; } = new SaveInfoContractAttribute("Foo1");

    protected override Foo1SaveInfo ParseCore(StorageNode node, in ParseNodeContext context)
    {
        StorableNodeParserManager parserManager = context.ParserManager;

        // 决定不支持 init 的情况，这样才能更好地保留默认值
        var foo1SaveInfo = new Foo1SaveInfo();

        if (node.Children is { } children)
        {
            foreach (var storageNode in children)
            {
                var currentName = storageNode.Name.AsSpan();

                var propertyNameForFoo1Property = "Foo1Property";
                if (currentName.Equals(propertyNameForFoo1Property, StringComparison.Ordinal))
                {
                    var typeOfFoo1Property = typeof(bool);
                    var nodeParserForFoo1Property = parserManager.GetNodeParser(typeOfFoo1Property);
                    var valueForFoo1Property = nodeParserForFoo1Property.Parse(storageNode, context);
                    foo1SaveInfo.Foo1Property = (bool) valueForFoo1Property;
                    continue;
                }

                var propertyNameForFoo2Property = "Foo2Property";
                if (currentName.Equals(propertyNameForFoo2Property, StringComparison.Ordinal))
                {
                    var typeOfFoo2Property = typeof(int);
                    var nodeParserForFoo2Property = parserManager.GetNodeParser(typeOfFoo2Property);
                    var valueForFoo2Property = nodeParserForFoo2Property.Parse(storageNode, context);
                    foo1SaveInfo.Foo2Property = (int) valueForFoo2Property;
                    continue;
                }
            }
        }

        return foo1SaveInfo;
    }

    protected override StorageNode DeparseCore(Foo1SaveInfo obj, in DeparseNodeContext context)
    {
        StorableNodeParserManager parserManager = context.ParserManager;

        var storageNode = new StorageNode();
        const int saveInfoMemberCount = 2;
        storageNode.Name = context.NodeName ?? TargetStorageName;
        storageNode.Children = new List<StorageNode>(saveInfoMemberCount);

        DeparseNodeContext tempContext;

        var propertyNameForFoo1Property = "Foo1Property";
        var typeOfFoo1Property = typeof(bool);
        var nodeParserForFoo1Property = parserManager.GetNodeParser(typeOfFoo1Property);
        object? valueForFoo1Property = obj.Foo1Property;
        if (valueForFoo1Property is not null)
        {
            tempContext = context with
            {
                NodeName = propertyNameForFoo1Property
            };
            var childNodeForFoo1Property = nodeParserForFoo1Property.Deparse(valueForFoo1Property, tempContext);
            storageNode.Children.Add(childNodeForFoo1Property);
        }

        var propertyNameForFoo2Property = "Foo2Property";
        var typeOfFoo2Property = typeof(int);
        var nodeParserForFoo2Property = parserManager.GetNodeParser(typeOfFoo2Property);
        object? valueForFoo2Property = obj.Foo2Property;
        if (valueForFoo2Property is not null)
        {
            tempContext = context with
            {
                NodeName = propertyNameForFoo2Property
            };
            var childNodeForFoo2Property = nodeParserForFoo2Property.Deparse(valueForFoo2Property, tempContext);
            storageNode.Children.Add(childNodeForFoo2Property);
        }

        return storageNode;
    }
}