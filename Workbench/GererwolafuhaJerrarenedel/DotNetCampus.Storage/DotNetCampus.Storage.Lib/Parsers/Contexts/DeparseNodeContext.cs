namespace DotNetCampus.Storage.Lib.Parsers.Contexts;

public readonly record struct DeparseNodeContext
{
    public required string? NodeName { get; init; }
}