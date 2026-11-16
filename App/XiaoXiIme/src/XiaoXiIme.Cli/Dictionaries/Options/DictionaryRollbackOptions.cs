using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("dictionary-rollback", Description = "Exchange a dictionary package with its retained previous version.")]
internal sealed class DictionaryRollbackOptions
{
    [Value(0, Description = "Target dictionary package directory.")]
    public string? PackageDirectory { get; init; }
}