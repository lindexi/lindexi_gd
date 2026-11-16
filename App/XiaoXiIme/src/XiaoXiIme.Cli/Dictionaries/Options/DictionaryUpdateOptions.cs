using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("dictionary-update", Description = "Compile local XiaoXiIme TSV sources and update a dictionary package.")]
internal sealed class DictionaryUpdateOptions
{
    [Value(0, Description = "Directory containing *.phonetic.tsv and optional *.shape.tsv/*.symbols.tsv files.")]
    public string? SourceDirectory { get; init; }

    [Value(1, Description = "Target dictionary package directory.")]
    public string? PackageDirectory { get; init; }

    [Option("scheme", Description = "Input scheme: fullPinyin or xiaoheDoublePinyin.", ValueName = "name")]
    public string Scheme { get; init; } = "fullPinyin";
}