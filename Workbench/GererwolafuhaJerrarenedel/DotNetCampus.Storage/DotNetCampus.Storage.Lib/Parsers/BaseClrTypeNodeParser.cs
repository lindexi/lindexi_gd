namespace DotNetCampus.Storage.Lib.Parsers;

internal abstract class BaseClrTypeNodeParser<T> : NodeParser<T>
{
    public override string? TargetStorageName => null;
}