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
}
