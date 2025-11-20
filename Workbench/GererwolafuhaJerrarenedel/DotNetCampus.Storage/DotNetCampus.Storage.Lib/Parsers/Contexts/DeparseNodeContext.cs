namespace DotNetCampus.Storage.Lib.Parsers.Contexts;

public readonly record struct DeparseNodeContext
{
    public string? NodeName { get; init; }
    public required StorableNodeParserManager ParserManager { get; init; }
}