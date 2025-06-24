using System.Reflection;

using DotNetCampus.Installer.Lib.Hosts.Contexts;

namespace DotNetCampus.Installer.Lib.Hosts;

/// <summary>
/// 安装器主机的构建器
/// </summary>
public class InstallerHostBuilder
{
    /// <summary>
    /// 构建出 <see cref="InstallerHost"/> 安装器主机
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public InstallerHost Build()
    {
        var workingFolder = _workingFolder;
        if (workingFolder is null)
        {
            workingFolder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}"));
        }
        else if (!workingFolder.Exists)
        {
            workingFolder.Create();
        }

        FileInfo? splashScreenFile = _splashScreenConfiguration?.SplashScreenFile;
        if (splashScreenFile is null && _splashScreenConfiguration?.ResourceInfo is { } resourceInfo)
        {
            using Stream assetsStream = resourceInfo.GetManifestResourceStream();

            var tempFile = Path.Join(workingFolder.FullName, $"{resourceInfo.ManifestResourceName}");
            using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                assetsStream.CopyTo(fileStream);
            }

            splashScreenFile = new FileInfo(tempFile);
        }

        var assemblyManifestResourceInfo = _installerResourceAssetsInfo;
        if (assemblyManifestResourceInfo is null)
        {
            throw new InvalidOperationException();
        }

        var configuration = new InstallerHostConfiguration()
        {
            WorkingFolder = workingFolder,
            SplashScreenFile = splashScreenFile,
            InstallerResourceAssetsInfo = assemblyManifestResourceInfo.Value,
            InstallerRelativePath = _installerRelativePath,
            InstallerProcessStartConfigAction = _installerProcessStartConfigAction,
        };

        return new InstallerHost(configuration);
    }

    /// <summary>
    /// 构建出 <see cref="InstallerHost"/> 且运行
    /// </summary>
    /// <returns></returns>
    public int BuildAndRun()
    {
        InstallerHost installerHost = Build();
        return installerHost.Run();
    }

    /// <summary>
    /// 配置安装过程中的工作路径
    /// </summary>
    /// <param name="workingFolder"></param>
    /// <returns></returns>
    public InstallerHostBuilder ConfigWorkingFolder(DirectoryInfo workingFolder)
    {
        _workingFolder = workingFolder;
        return this;
    }

    private DirectoryInfo? _workingFolder;

    /// <summary>
    /// 使用欢迎界面
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public InstallerHostBuilder UseSplashScreen(Action<InstallerHostSplashScreenConfiguration> config)
    {
        _splashScreenConfiguration = new InstallerHostSplashScreenConfiguration();
        config(_splashScreenConfiguration);

        return this;
    }

    private InstallerHostSplashScreenConfiguration? _splashScreenConfiguration;

    /// <summary>
    /// 配置里层带界面的安装器的所在资源包
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="manifestResourceName"></param>
    /// <returns></returns>
    public InstallerHostBuilder ConfigInstallerResourceAssets(Assembly assembly, string manifestResourceName)
    {
        _installerResourceAssetsInfo = new AssemblyManifestResourceInfo(assembly, manifestResourceName);
        return this;
    }

    private AssemblyManifestResourceInfo? _installerResourceAssetsInfo;

    /// <summary>
    /// 配置安装器的 Installer.exe 文件的相对路径，可用于自定义里层带界面的安装器的文件名
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public InstallerHostBuilder ConfigInstallerFilePathInResourceAssets(string relativePath)
    {
        _installerRelativePath = relativePath;
        return this;
    }

    private string _installerRelativePath = "Installer.exe";

    /// <summary>
    /// 配置里层带界面的安装器的进程启动
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public InstallerHostBuilder ConfigInstallerProcessStart(Action<ProcessStartInfoConfigurationContext> config)
    {
        _installerProcessStartConfigAction = config;
        return this;
    }

    private Action<ProcessStartInfoConfigurationContext>? _installerProcessStartConfigAction;
}