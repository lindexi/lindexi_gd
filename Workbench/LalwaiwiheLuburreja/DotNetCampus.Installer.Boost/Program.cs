// See https://aka.ms/new-console-template for more information

using System.Reflection;

using DotNetCampus.Installer.Lib;
using DotNetCampus.Installer.Lib.Hosts;

return InstallerHost.CreateBuilder()
    .ConfigWorkingFolder(Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}")))
    .UseSplashScreen(Assembly.GetExecutingAssembly(), "DotNetCampus.Installer.Boost.SplashScreen.png")
    .ConfigInstallerResourceAssets(Assembly.GetExecutingAssembly(), "DotNetCampus.Installer.Boost.Resource.assets")
    .BuildAndRun();