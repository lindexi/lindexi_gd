using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace DotNetCampus.Storage.Lib;

public class StorageXmlSerializer
{
    public async Task SerializeAsync(StorageNode node, FileInfo outputFile)
    {
        var element = RecursiveSerializeNode(node);
        XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), element);

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
            element.Value = node.Value.ToText();
        }

        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                element.Add(RecursiveSerializeNode(child));
            }
        }

        return element;
    }

    public async Task<StorageNode> DeserializeAsync(FileInfo file)
    {
        await using var fileStream = file.OpenRead();
        XDocument document = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
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
        var type = currentElement.Attributes(TypeName).FirstOrDefault();
        if (type is not null && Enum.TryParse(type.Value, out storageNodeType))
        {
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