using System.Reflection;

namespace DotNetCampus.Installer.Lib.Hosts;

public static class InstallerHostBuilderExtensions
{
    /// <summary>
    /// 使用欢迎界面
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assembly"></param>
    /// <param name="manifestResourceName"></param>
    /// <returns></returns>
    public static InstallerHostBuilder UseSplashScreen(this InstallerHostBuilder builder, Assembly assembly, string manifestResourceName)
    {
        builder.UseSplashScreen(configuration =>
            configuration.FromAssemblyManifestResource(assembly, manifestResourceName));

        return builder;
    }

    /// <summary>
    /// 使用欢迎界面
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="splashScreenFile">欢迎界面的文件，一般是正常的 png 等格式图片</param>
    /// <returns></returns>
    public static InstallerHostBuilder UseSplashScreen(this InstallerHostBuilder builder, FileInfo splashScreenFile)
    {
        builder.UseSplashScreen(configuration =>
            configuration.FromFile(splashScreenFile));

        return builder;
    }
}