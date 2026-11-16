using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("native-ime-load-probe", Description = "Internal isolated native IME loader probe.")]
internal sealed class NativeImeLoadProbeOptions
{
    [Value(0, Description = "Full path to the IME binary.")]
    public string? ImeFile { get; init; }
}