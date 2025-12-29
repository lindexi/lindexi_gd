using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

internal class StringNodeParser : BaseClrTypeNodeParser<string>
{
    protected internal override string ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return node.Value.ToText();
    }

    protected internal override StorageNode DeparseCore(string obj, in DeparseNodeContext context)
    {
        var name = context.NodeName;
        return new StorageNode()
        {
            Name = name,
            Value = obj,
        };
    }
}