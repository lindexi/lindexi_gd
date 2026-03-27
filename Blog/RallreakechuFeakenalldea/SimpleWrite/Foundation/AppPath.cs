using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleWrite.Foundation.Primitive;

namespace SimpleWrite.Foundation;

/// <summary>
/// 应用路径管理器
/// </summary>
public class AppPathManager
{
    public AppPathManager()
    {
        var localApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        DataDirectory = Directory.CreateDirectory(Path.Join(localApplicationDataFolder, "SimpleWrite"));

        ConfigurationDirectory = Directory.CreateDirectory(Path.Join(DataDirectory, "Configurations"));

        RootLogDirectory = Directory.CreateDirectory(Path.Join(DataDirectory, "Logs"));

        // 日志文件夹，命名格式 年月日_时分秒-进程号
        var logFolderName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "-" + Environment.ProcessId;
        LogDirectory = Directory.CreateDirectory(Path.Join(RootLogDirectory, logFolderName));

        CopilotChatLogDirectory = Directory.CreateDirectory(Path.Join(LogDirectory, "CopilotChatLogs"));

        var applicationConfigurationFile = Path.Join(ConfigurationDirectory, "ApplicationConfiguration.coin");
        ApplicationConfigurationFile = new FilePath(applicationConfigurationFile);
    }

    public DirectoryPath DataDirectory { get; }

    public DirectoryPath ConfigurationDirectory { get; }

    public DirectoryPath RootLogDirectory { get; }
    public DirectoryPath LogDirectory { get; }

    public DirectoryPath CopilotChatLogDirectory { get; }

    /// <summary>
    /// 应用程序配置文件
    /// </summary>
    public FilePath ApplicationConfigurationFile { get; }
}