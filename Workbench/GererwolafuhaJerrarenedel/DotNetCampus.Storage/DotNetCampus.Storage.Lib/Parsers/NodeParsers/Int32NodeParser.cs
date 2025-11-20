using DotNetCampus.Storage.Lib.Parsers.Contexts;
using DotNetCampus.Storage.Lib.StorageNodes;

namespace DotNetCampus.Storage.Lib.Parsers.NodeParsers;

internal class Int32NodeParser : BaseClrTypeNodeParser<int>
{
    protected internal override int ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return int.Parse(node.Value.AsSpan());
    }

    protected internal override StorageNode DeparseCore(int obj, in DeparseNodeContext context)
    {
        var name = context.NodeName!;
        return new StorageNode()
        {
            Name = name,
            Value = obj.ToString(),
        };
    }
}