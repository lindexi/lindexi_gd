namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

/// <summary>
/// 安装器主机的配置
/// </summary>
public readonly record struct InstallerHostConfiguration
{
    /// <summary>
    /// 工作路径
    /// </summary>
    public required DirectoryInfo WorkingFolder { get; init; }

    /// <summary>
    /// 欢迎界面的图片文件。如果是空表示不要使用欢迎界面
    /// </summary>
    public required FileInfo? SplashScreenFile { get; init; }

    /// <summary>
    /// 里层带界面的安装器所在的嵌入程序集的压缩包资源
    /// </summary>
    public required AssemblyManifestResourceInfo InstallerResourceAssetsInfo { get; init; }

    /// <summary>
    /// 里层带界面的安装器所在相对于压缩包里面的路径
    /// </summary>
    public required string InstallerRelativePath { get; init; }

    /// <summary>
    /// 启动里层带界面的安装器时的配置
    /// </summary>
    public Action<ProcessStartInfoConfigurationContext>? InstallerProcessStartConfigAction { get; init; }
}