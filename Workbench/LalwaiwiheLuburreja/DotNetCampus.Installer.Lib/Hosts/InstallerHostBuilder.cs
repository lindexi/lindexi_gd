using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotNetCampus.Installer.Lib.Hosts;

public class InstallerHostBuilder
{
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

    public int BuildAndRun()
    {
        var installerHost = Build();
        return installerHost.Run();
    }

    public InstallerHostBuilder ConfigWorkingFolder(DirectoryInfo workingFolder)
    {
        _workingFolder = workingFolder;
        return this;
    }

    private DirectoryInfo? _workingFolder;

    public InstallerHostBuilder UseSplashScreen(Action<InstallerHostSplashScreenConfiguration> config)
    {
        _splashScreenConfiguration = new InstallerHostSplashScreenConfiguration();
        config(_splashScreenConfiguration);

        return this;
    }

    private InstallerHostSplashScreenConfiguration? _splashScreenConfiguration;

    public InstallerHostBuilder ConfigInstallerResourceAssets(Assembly assembly, string manifestResourceName)
    {
        _installerResourceAssetsInfo = new AssemblyManifestResourceInfo(assembly, manifestResourceName);
        return this;
    }

    private AssemblyManifestResourceInfo? _installerResourceAssetsInfo;

    public InstallerHostBuilder ConfigInstallerFilePathInResourceAssets(string relativePath)
    {
        _installerRelativePath = relativePath;
        return this;
    }

    private string _installerRelativePath = "Installer.exe";

    public InstallerHostBuilder ConfigInstallerProcessStart(Action<ProcessStartInfo> config)
    {
        _installerProcessStartConfigAction = config;
        return this;
    }

    private Action<ProcessStartInfo>? _installerProcessStartConfigAction;
}