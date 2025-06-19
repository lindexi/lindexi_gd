// See https://aka.ms/new-console-template for more information

using DotNetCampus.Cli;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using InstallerCreateTool;

var packingFolder =
    @"..\..\..\..\DotNetCampus.Installer.Sample\bin\Debug\net9.0-windows\";
packingFolder = Path.GetFullPath(packingFolder);

if (!Directory.Exists(packingFolder))
{
    Console.WriteLine($"打包目录不存在：{packingFolder}");
    return;
}

var sampleFilePath = Path.Join(packingFolder, "DotNetCampus.Installer.Sample.exe");
var installerFilePath = Path.Join(packingFolder, "Installer.exe");
if (File.Exists(sampleFilePath))
{
    File.Move(sampleFilePath, installerFilePath, overwrite: true);
}

var installerOutputFile = "SampleInstaller.exe";

var installerBoostProjectFolder = Path.GetFullPath(@"..\..\..\..\DotNetCampus.Installer.Boost\");
var installerBoostProjectName = "DotNetCampus.Installer.Boost.csproj";
var installerBoostProjectPath = Path.Join(installerBoostProjectFolder, installerBoostProjectName);

Console.WriteLine($"开始制作安装包资产文件");

var installerIconFilePath = "不存在的图标.ico";
if (File.Exists(installerIconFilePath))
{
    File.Copy(installerIconFilePath,Path.Join(installerBoostProjectFolder, "Assets", "icon.ico"));
}

var splashScreenFilePath = "不存在的欢迎界面.png";
if (File.Exists(splashScreenFilePath))
{
    File.Copy(splashScreenFilePath, Path.Join(installerBoostProjectFolder, "Assets", "SplashScreen.png"));
}

var resourceAssetsName = "Resource.assets";
var resourceAssetsFile = Path.Join(installerBoostProjectFolder, "Assets", resourceAssetsName);

DirectoryArchive.Compress(new DirectoryInfo(packingFolder), new FileInfo(resourceAssetsFile));

var option = CommandLine.Parse(args).As<Option>();
// DotNetCampus.Installer.Sample\bin\Release\net9.0-windows\publish\win-x86
Console.WriteLine("Hello, World!");
