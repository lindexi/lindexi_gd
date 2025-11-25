using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

internal abstract class BaseClrTypeNodeParser<T> : NodeParser<T>
{
    public override string? TargetStorageName => null;
}