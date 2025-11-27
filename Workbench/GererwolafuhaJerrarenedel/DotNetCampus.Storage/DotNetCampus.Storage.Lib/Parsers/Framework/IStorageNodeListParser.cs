using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers;

public interface IStorageNodeListParser
{
    IEnumerable<TElement> ParseElementOfList<TElement>(IReadOnlyList<StorageNode>? storageNodeChildren,
        ParseNodeContext context);

    List<StorageNode> DeparseElementOfList(IEnumerable<object> children, DeparseNodeContext context);
}