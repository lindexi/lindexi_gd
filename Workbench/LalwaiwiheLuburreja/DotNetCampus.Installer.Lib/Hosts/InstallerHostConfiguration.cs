using System.Diagnostics;

namespace DotNetCampus.Installer.Lib.Hosts;

public readonly record struct InstallerHostConfiguration
{
    public required DirectoryInfo WorkingFolder { get; init; }

    public required FileInfo? SplashScreenFile { get; init; }
    public required AssemblyManifestResourceInfo InstallerResourceAssetsInfo { get; init; }
    public required string InstallerRelativePath { get; init; }

    public Action<ProcessStartInfo>? InstallerProcessStartConfigAction { get; init; }
}