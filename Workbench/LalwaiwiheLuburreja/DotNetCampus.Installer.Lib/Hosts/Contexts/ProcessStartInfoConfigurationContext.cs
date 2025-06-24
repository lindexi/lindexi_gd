using System.Diagnostics;

namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

/// <summary>
/// 启动里层带界面的进程时的配置信息
/// </summary>
public readonly record struct ProcessStartInfoConfigurationContext
{
    public required ProcessStartInfo ProcessStartInfo { get; init; }
    public required IntPtr SplashScreenWindowHandler { get; init; }
    public required DirectoryInfo WorkingFolder { get; init; }
}