using DotNetCampus.Storage.Lib.SaveInfos;

namespace DotNetCampus.Storage.Lib.Parsers.NodeParsers;

internal abstract class BaseClrTypeNodeParser<T> : NodeParser<T>
{
    public override string? TargetStorageName => null;
}