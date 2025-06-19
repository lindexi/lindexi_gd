using System;
using System.Collections.Generic;
using System.Linq;
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
            using Stream? assetsStream = resourceInfo.Assembly.GetManifestResourceStream(resourceInfo.ManifestResourceName);
            if (assetsStream is null)
            {
                throw new ArgumentException($"传入的 ManifestResourceName={resourceInfo.ManifestResourceName} 找不到资源。可能是忘记嵌入资源，也可能是改了名字忘记改这里，也可能传错程序集。 Assembly={resourceInfo.Assembly.FullName}");
            }

            var tempFile = Path.Join(workingFolder.FullName, $"{resourceInfo.ManifestResourceName}");
            using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                assetsStream.CopyTo(fileStream);
            }

            splashScreenFile = new FileInfo(tempFile);
        }

        var configuration = new InstallerHostConfiguration()
        {
            WorkingFolder = workingFolder,
            SplashScreenFile = splashScreenFile
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
}