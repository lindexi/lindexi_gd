namespace DotNetCampus.Installer.Lib.Hosts;

public readonly record struct InstallerHostConfiguration
{
    public required DirectoryInfo WorkingFolder { get; init; }

    public readonly FileInfo? SplashScreenFile { get; init; }
}