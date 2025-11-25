using DotNetCampus.Storage.StorageFiles;
using DotNetCampus.Storage.StorageNodes;

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DotNetCampus.Storage.Serialization.XmlSerialization;

/// <summary>
/// 存储的 XML 序列化器
/// </summary>
public class StorageXmlSerializer : IStorageNodeSerializer
{
    public XDocument Serialize(StorageNode node)
    {
        var rootElement = RecursiveSerializeNode(node);

        // 如果有任何的节点包含 Type 属性，则添加命名空间声明
        var includeTypeName = rootElement.DescendantNodesAndSelf()
            .OfType<XElement>()
            .Any(t => t.HasAttributes
                      && t.Attribute(TypeName) != null);
        if (includeTypeName)
        {
            rootElement.SetAttributeValue(XNamespace.Xmlns + "s", TypeName.NamespaceName);
        }

        XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rootElement);
        return document;
    }

    public Task SerializeAsync(StorageNode node, FileInfo outputFile)
    {
        var localStorageFileInfo = new LocalStorageFileInfo()
        {
            RelativePath = outputFile.FullName,
            FileInfo = outputFile
        };

        return SerializeAsync(node, localStorageFileInfo);
    }

    public async Task SerializeAsync(StorageNode node, IStorageFileInfo outputFile)
    {
        var document = Serialize(node);

        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            NewLineHandling = NewLineHandling.Entitize,
            Encoding = Encoding.UTF8,
            Async = true,
        };

        await using var fileStream = outputFile.OpenWrite();

        await using var writer = System.Xml.XmlWriter.Create(fileStream, settings);
        await document.WriteToAsync(writer, CancellationToken.None);
    }

    private XElement RecursiveSerializeNode(StorageNode node)
    {
        var element = new XElement(node.Name.Text);
        if (!node.Value.IsNull)
        {
            // 考虑 xml 合法字符 [dotnet OpenXML 已知问题 设置 0x0001 等 XML 不合法字符给到标题将在保存时抛出异常 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18730520 )
            // 考虑 QainurbeweLekaynula 的实现代码
            element.Value = node.Value.ToText();
        }

        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                element.Add(RecursiveSerializeNode(child));
            }
        }

        if (node.StorageNodeType != StorageNodeType.Unknown)
        {
            element.SetAttributeValue(TypeName, node.StorageNodeType.ToString());
        }

        return element;
    }

    public Task<StorageNode> DeserializeAsync(FileInfo file)
    {
        var localStorageFileInfo = new LocalStorageFileInfo()
        {
            RelativePath = file.FullName,
            FileInfo = file
        };

        return DeserializeAsync(localStorageFileInfo);
    }

    public async Task<StorageNode> DeserializeAsync(IReadOnlyStorageFileInfo file)
    {
        await using var fileStream = file.OpenRead();
        XDocument document = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
        var rootNode = Deserialize(document);
        return rootNode;
    }

    public StorageNode Deserialize(XDocument document)
    {
        var root = document.Root!;
        var rootNode = RecursiveDeserializeNode(root);
        return rootNode;
    }

    public XName TypeName { get; } = XName.Get("Type", "Storage");

    private StorageNode RecursiveDeserializeNode(XElement currentElement)
    {
        List<StorageNode>? children = null;
        if (currentElement.HasElements)
        {
            children = [];
            var elements = currentElement.Elements();
            foreach (var element in elements)
            {
                children.Add(RecursiveDeserializeNode(element));
            }
        }

        StorageNodeType storageNodeType = StorageNodeType.Unknown;

        if (currentElement.HasAttributes)
        {
            var type = currentElement.Attributes(TypeName).FirstOrDefault();
            if (type is not null && Enum.TryParse(type.Value, out StorageNodeType storageType))
            {
                storageNodeType = storageType;
            }

            foreach (XAttribute attribute in currentElement.Attributes())
            {
                if (attribute.Name == TypeName)
                {
                    continue;
                }

                // 处理属性节点
                var attributeNode = new StorageNode()
                {
                    Name = attribute.Name.LocalName,
                    Value = attribute.Value,
                    StorageNodeType = StorageNodeType.Property,
                };
                children ??= [];
                children.Add(attributeNode);
            }
        }

        var storageNode = new StorageNode()
        {
            Name = currentElement.Name.LocalName,
            Value = currentElement.Nodes().All(static x => x is XText) ?
                currentElement.Value
                : StorageTextSpan.NullValue,
            StorageNodeType = storageNodeType,
            Children = children,
        };
        return storageNode;
    }
}