using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

internal class BoolNodeParser : BaseClrTypeNodeParser<bool>
{
    protected internal override bool ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return bool.Parse(node.Value.AsSpan());
    }

    protected internal override StorageNode DeparseCore(bool obj, in DeparseNodeContext context)
    {
        var name = context.NodeName!;
        return new StorageNode()
        {
            Name = name,
            Value = obj.ToString(),
        };
    }
}