using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Cli.Compiler;

namespace InstallerCreateTool;

internal class Options
{
    [Option()]
    public required string PackingFolder { get; init; }

    /// <summary>
    /// 安装包最终输出的文件夹
    /// </summary>
    [Option()]
    public string? InstallerOutputFolder { get; init; }

    /// <summary>
    /// 安装启动器项目的文件夹路径
    /// </summary>
    [Option()]
    public required string InstallerBoostProjectFolderPath { get; init; }

    /// <summary>
    /// 项目名，如 Installer.Boost.csproj
    /// </summary>
    [Option()]
    public required string InstallerBoostProjectName { get; init; }

    /// <summary>
    /// 图标文件的路径，安装包的图标文件
    /// </summary>
    [Option()]
    public string? InstallerIconFilePath { get; init; }

    /// <summary>
    /// 欢迎界面图片的路径，安装包的欢迎界面
    /// </summary>
    [Option()]
    public string? SplashScreenFilePath { get; init; }
}
