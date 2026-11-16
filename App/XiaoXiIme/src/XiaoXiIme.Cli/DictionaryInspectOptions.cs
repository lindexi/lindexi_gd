using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("dictionary-inspect", Description = "Validate a dictionary package and report its version, path, entry counts, and SeWZC attribution.")]
internal sealed class DictionaryInspectOptions
{
    [Value(0, Description = "Dictionary package directory.")]
    public string? PackageDirectory { get; init; }

    [Option("json", Description = "Write the inspection report as JSON.")]
    public bool Json { get; init; }
}