using System.Reflection;

namespace DotNetCampus.Installer.Lib.Hosts;

public static class InstallerHostBuilderExtensions
{
    public static InstallerHostBuilder UseSplashScreen(this InstallerHostBuilder builder, Assembly assembly, string manifestResourceName)
    {
        builder.UseSplashScreen(configuration =>
            configuration.FromAssemblyManifestResource(assembly, manifestResourceName));

        return builder;
    }

    public static InstallerHostBuilder UseSplashScreen(this InstallerHostBuilder builder, FileInfo splashScreenFile)
    {
        builder.UseSplashScreen(configuration =>
            configuration.FromFile(splashScreenFile));

        return builder;
    }
}