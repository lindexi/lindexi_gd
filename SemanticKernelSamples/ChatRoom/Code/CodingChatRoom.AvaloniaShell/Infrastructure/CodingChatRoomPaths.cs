using System;
using System.IO;

namespace CodingChatRoom.AvaloniaShell.Infrastructure;

/// <summary>
/// 定义 CodingChatRoom 唯一允许使用的本地数据路径。
/// </summary>
public sealed class CodingChatRoomPaths
{
    private const string ApplicationFolderName = "CodingChatRoom";
    private const string ConfigurationFileName = "AgentConfiguration.json";
    private const string ShellSettingsFileName = "ShellSettings.json";

    private CodingChatRoomPaths(string rootDirectory)
    {
        RootDirectory = Path.GetFullPath(rootDirectory);
        ConfigurationFile = new FileInfo(Path.Join(RootDirectory, ConfigurationFileName));
        ShellSettingsFile = new FileInfo(Path.Join(RootDirectory, ShellSettingsFileName));
        LogDirectory = Path.Join(RootDirectory, "Logs");
        SessionDirectory = Path.Join(RootDirectory, "Sessions");
    }

    /// <summary>
    /// 获取应用本地数据根目录。
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// 获取固定模型配置文件。
    /// </summary>
    public FileInfo ConfigurationFile { get; }

    /// <summary>
    /// 获取 Shell 设置文件。
    /// </summary>
    public FileInfo ShellSettingsFile { get; }

    /// <summary>
    /// 获取文本日志目录。
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    /// 获取可恢复会话目录。
    /// </summary>
    public string SessionDirectory { get; }

    /// <summary>
    /// 为当前用户创建生产路径对象。
    /// </summary>
    public static CodingChatRoomPaths CreateForCurrentUser()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Create(Path.Join(localApplicationData, ApplicationFolderName));
    }

    /// <summary>
    /// 为测试或显式宿主根目录创建路径对象。
    /// </summary>
    internal static CodingChatRoomPaths Create(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        return new CodingChatRoomPaths(rootDirectory);
    }

    /// <summary>
    /// 创建应用根目录、日志目录和会话目录，不创建配置文件。
    /// </summary>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(SessionDirectory);
    }
}
