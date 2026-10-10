using System;
using System.Windows.Input;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示启动失败页面的数据和退出操作。
/// </summary>
public sealed class StartupFailureViewModel
{
    public StartupFailureViewModel(string configurationFilePath, Exception exception, Action exitAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationFilePath);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(exitAction);

        ConfigurationFilePath = configurationFilePath;
        ErrorMessage = exception.Message;
        ErrorDetails = exception.ToString();
        ExitCommand = new SimpleCommand(exitAction);
    }

    public string ConfigurationFilePath { get; }

    public string ErrorMessage { get; }

    public string ErrorDetails { get; }

    public ICommand ExitCommand { get; }
}
