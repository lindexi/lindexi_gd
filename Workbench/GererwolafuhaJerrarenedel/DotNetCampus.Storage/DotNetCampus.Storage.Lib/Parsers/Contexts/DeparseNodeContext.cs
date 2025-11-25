namespace DotNetCampus.Storage.Parsers.Contexts;

public readonly record struct DeparseNodeContext
{
    public string? NodeName { get; init; }
    public required StorageNodeParserManager ParserManager { get; init; }
}