using System.Globalization;
using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

internal class DoubleNodeParser : BaseClrTypeNodeParser<double>
{
    protected internal override double ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return double.Parse(node.Value.AsSpan());
    }
    protected internal override StorageNode DeparseCore(double obj, in DeparseNodeContext context)
    {
        var name = context.NodeName!;
        return new StorageNode()
        {
            Name = name,
            Value = obj.ToString(CultureInfo.InvariantCulture),
        };
    }
}