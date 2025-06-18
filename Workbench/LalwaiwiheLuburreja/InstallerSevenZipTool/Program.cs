// See https://aka.ms/new-console-template for more information

using DotNetCampus.Cli;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using InstallerSevenZipTool;

if (args.Length == 0)
{
    Console.WriteLine("""
                      安装包资产制作工具
                      
                      InstallerSevenZipTool -f <输入文件夹> -o <输出文件>
                      """);
}

var options = CommandLine.Parse(args).As<Options>();
Console.WriteLine($"开始制作安装包资产文件。输入文件夹： '{options.InputDirectory}' 输出文件： '{options.OutputFile}'");

DirectoryArchive.Compress(new DirectoryInfo(options.InputDirectory), new FileInfo(options.OutputFile));

Console.WriteLine("Hello, World!");
