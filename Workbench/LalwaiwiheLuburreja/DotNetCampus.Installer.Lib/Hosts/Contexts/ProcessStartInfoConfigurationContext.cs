using System.Diagnostics;

namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

public readonly record struct ProcessStartInfoConfigurationContext()
{
    public required ProcessStartInfo ProcessStartInfo { get; init; }
    public required IntPtr SplashScreenWindowHandler { get; init; }
    public required DirectoryInfo WorkingFolder { get; init; }
}