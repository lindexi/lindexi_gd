using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

using System.Globalization;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

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
            Value = obj.ToString(CultureInfo.InvariantCulture),
        };
    }
}