using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.Hosts;

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