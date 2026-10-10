using System;
using System.Collections.Generic;
using System.IO;

namespace CodingChatRoom.AvaloniaShell.Abilities;

internal sealed record AbilityDefinition
(
    string Id,
    string DisplayName,
    string? Prompt,
    DirectoryInfo? SourceDirectory
)
{
    public string Expand(string input) => Prompt?.Replace("{{input}}", input, StringComparison.Ordinal) ?? input;
}

internal sealed record AbilityCollection
(
    IReadOnlyList<AbilityDefinition> Items,
    string CompressionPrompt,
    string? CompressionDisplayName
)
{
    public static AbilityCollection Empty { get; } = new([], AbilityCatalog.DefaultCompressionPrompt, null);
}