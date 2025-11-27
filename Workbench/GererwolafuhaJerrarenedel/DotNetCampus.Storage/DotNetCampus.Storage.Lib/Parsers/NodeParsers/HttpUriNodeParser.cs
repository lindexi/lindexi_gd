using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.Standard;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

internal class HttpUriNodeParser : NodeParser<HttpUri>
{
    public override string? TargetStorageName => null;
    protected internal override HttpUri ParseCore(StorageNode node, in ParseNodeContext context)
    {
        return new HttpUri(node.Value.ToText());
    }

    protected internal override StorageNode DeparseCore(HttpUri obj, in DeparseNodeContext context)
    {
        var name = context.NodeName;
        return new StorageNode()
        {
            Name = name,
            Value = obj.Value,
        };
    }
}