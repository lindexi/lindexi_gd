using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("uninstall", Description = "Uninstall XiaoXiIme and remove its deployed files.")]
internal sealed class UninstallOptions
{
    [Option
    (
        "purge-user-data",
        Description = "Also delete the per-user learned dictionary. User data is retained by default."
    )]
    public bool PurgeUserData { get; init; }
}