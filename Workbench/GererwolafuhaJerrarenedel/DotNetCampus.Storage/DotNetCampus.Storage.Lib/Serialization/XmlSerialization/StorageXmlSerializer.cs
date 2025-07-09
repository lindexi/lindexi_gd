using System.Collections.Generic;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace DotNetCampus.Storage.Lib;

public class StorageXmlSerializer
{
    // Serialize

    public async Task DeserializeAsync(FileInfo file)
    {
        using var fileStream = file.OpenRead();
        XDocument document = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
        var root = document.Root!;
        var rootNode = RecursiveParseNode(root);
    }

    public XName TypeName { get; } = XName.Get("Type", "Storage");

    private StorageNode RecursiveParseNode(XElement currentElement)
    {
        List<StorageNode>? children = null;
        if (currentElement.HasElements)
        {
            children = [];
            var elements = currentElement.Elements();
            foreach (var element in elements)
            {
                children.Add(RecursiveParseNode(element));
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