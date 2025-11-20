using DotNetCampus.Storage.Lib.Parsers.Contexts;
using DotNetCampus.Storage.Lib.StorageNodes;

namespace DotNetCampus.Storage.Lib.Parsers.NodeParsers;

internal class Int64NodeParser : BaseClrTypeNodeParser<long>
{
    protected internal override long ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return long.Parse(node.Value.AsSpan());
    }
    protected internal override StorageNode DeparseCore(long obj, in DeparseNodeContext context)
    {
        var name = context.NodeName!;
        return new StorageNode()
        {
            Name = name,
            Value = obj.ToString(),
        };
    }
}