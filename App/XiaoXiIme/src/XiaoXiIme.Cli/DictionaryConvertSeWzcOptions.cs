using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("dictionary-convert-sewzc", Description = "One-time conversion of a SeWZC dictionary snapshot into XiaoXiIme native TSV sources.")]
internal sealed class DictionaryConvertSeWzcOptions
{
    [Value(0, Description = "Copied SeWZC data/dictionaries snapshot directory.")]
    public string? SourceDirectory { get; init; }

    [Value(1, Description = "Target XiaoXiIme data/dictionaries directory.")]
    public string? TargetDirectory { get; init; }
}