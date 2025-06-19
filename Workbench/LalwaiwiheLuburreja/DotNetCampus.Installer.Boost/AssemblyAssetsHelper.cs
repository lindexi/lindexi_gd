using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Boost;

internal static class AssemblyAssetsHelper
{
    public static FileInfo GetTempSplashScreenImageFile()
    {
        var resourceStream = GetResourceStream("DotNetCampus.Installer.Boost.SplashScreen.png");
        var tempFile = Path.Join(Path.GetTempPath(), $"DotNetCampus.Installer.Boost.SplashScreen.{Path.GetRandomFileName()}.png");

        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
        {
            resourceStream.CopyTo(fileStream);
        }

        return new FileInfo(tempFile);
    }

    public static Stream GetResourceStream(string name)
    {
        Stream? manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        return manifestResourceStream!;
    }

    public static DirectoryInfo ExtractInstallerAssetsToDirectory(string assetsFileName, DirectoryInfo workingFolder)
    {
        var manifestResourceName = $"DotNetCampus.Installer.Boost.{assetsFileName}";
        using var assetsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestResourceName);

        if (assetsStream is null)
        {
            throw new ArgumentException($"传入的 manifestResourceName={manifestResourceName} 找不到资源。可能是忘记嵌入资源，也可能是改了名字忘记改这里");
        }

        var zipOutputFolder = Directory.CreateDirectory(Path.Join(workingFolder.FullName, assetsFileName));
        DirectoryArchive.Decompress(assetsStream, zipOutputFolder);
        return zipOutputFolder;
    }
}
