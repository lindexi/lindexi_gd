// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using DotNetCampus.Cli;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using InstallerCreateTool;

Options option;

#if DEBUG
if (args.Length == 0)
{
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

    option = new Options
    {
        PackingFolder = packingFolder,
        InstallerOutputFolder = "SampleInstallerFolder",
        InstallerBoostProjectFolderPath = Path.GetFullPath(@"..\..\..\..\DotNetCampus.Installer.Boost\"),
        InstallerBoostProjectName = "DotNetCampus.Installer.Boost.csproj",
        InstallerIconFilePath = "不存在的图标.ico",
        SplashScreenFilePath = "不存在的欢迎界面.png",
    };
}
else
#endif
{
    option = CommandLine.Parse(args).As<Options>();
}

var installerOutputFolder = option.InstallerOutputFolder;

var installerBoostProjectFolder = option.InstallerBoostProjectFolderPath;
var installerBoostProjectName = option.InstallerBoostProjectName;

var installerBoostProjectPath = Path.Join(installerBoostProjectFolder, installerBoostProjectName);


var installerIconFilePath = option.InstallerIconFilePath;
if (File.Exists(installerIconFilePath))
{
    File.Copy(installerIconFilePath, Path.Join(installerBoostProjectFolder, "Assets", "icon.ico"));
}

var splashScreenFilePath = option.SplashScreenFilePath;
if (File.Exists(splashScreenFilePath))
{
    File.Copy(splashScreenFilePath, Path.Join(installerBoostProjectFolder, "Assets", "SplashScreen.png"));
}

Console.WriteLine($"开始制作安装包资产文件");

var resourceAssetsName = "Resource.assets";
var resourceAssetsFile = Path.Join(installerBoostProjectFolder, "Assets", resourceAssetsName);

DirectoryArchive.Compress(new DirectoryInfo(option.PackingFolder), new FileInfo(resourceAssetsFile));

Console.WriteLine($"完成制作安装包资产文件");

Console.WriteLine($"开始发布安装包 Boost 项目 {installerBoostProjectPath}");

List<string> argumentList =
[
    "publish",
    "-r", "win-x86",
    "-tl:off",
];
if (!string.IsNullOrEmpty(installerOutputFolder))
{
    argumentList.Add("-o");
    argumentList.Add(installerOutputFolder);
}
argumentList.Add(installerBoostProjectPath);

var process = Process.Start("dotnet", argumentList);
process.WaitForExit();

Console.WriteLine("打包完成");