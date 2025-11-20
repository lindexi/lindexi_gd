using DotNetCampus.Storage.Lib.Parsers.Contexts;
using DotNetCampus.Storage.Lib.StorageNodes;

namespace DotNetCampus.Storage.Lib.Parsers.NodeParsers;

internal abstract class BaseClrTypeNodeParser<T> : NodeParser<T>
 where T : IParsable<T>
{
    public override string? TargetStorageName => null;

    protected internal override T ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return T.Parse(node.Value.AsSpan().ToString(),null);
    }

    protected internal override StorageNode DeparseCore(T obj, in DeparseNodeContext context)
    {
        var name = context.NodeName!;
        return new StorageNode()
        {
            Name = name,
            Value = obj.ToString(),
        };
    }
}

internal class BoolNodeParser : BaseClrTypeNodeParser<bool>
{
   
}