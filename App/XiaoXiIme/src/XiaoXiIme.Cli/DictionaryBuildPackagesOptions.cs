using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("dictionary-build-packages", Description = "Compile full Pinyin and Xiaohe packages from native TSV sources without Native AOT publish.")]
internal sealed class DictionaryBuildPackagesOptions
{
    [Value(0, Description = "XiaoXiIme native TSV source directory.")]
    public string? SourceDirectory { get; init; }

    [Value(1, Description = "Host output directory that receives both compiled packages.")]
    public string? HostOutputDirectory { get; init; }
}