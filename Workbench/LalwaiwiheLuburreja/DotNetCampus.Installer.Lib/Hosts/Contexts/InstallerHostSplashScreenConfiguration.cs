using System.Reflection;

namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

/// <summary>
/// 欢迎界面的配置
/// </summary>
public class InstallerHostSplashScreenConfiguration
{
    public void FromAssemblyManifestResource(Assembly assembly, string manifestResourceName)
    {
        ResourceInfo = new AssemblyManifestResourceInfo(assembly, manifestResourceName);
    }

    public void FromFile(FileInfo file)
    {
        SplashScreenFile = file;
    }

    internal FileInfo? SplashScreenFile { get; private set; }

    internal AssemblyManifestResourceInfo? ResourceInfo { get; private set; }
}