namespace DotNetCampus.Storage.Lib.Parsers.Contexts;

public readonly record struct ParseNodeContext
{
    public required StorableNodeParserManager ParserManager { get; init; }
}