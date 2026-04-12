using System.Text;
using System.IO;

namespace DeepSeekWpf.Services;

public sealed class FileAppLogger : IAppLogger
{
    private readonly ISettingsService _settingsService;

    public FileAppLogger(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Log(string message)
    {
        try
        {
            var logPath = _settingsService.CurrentSettings.LogPath;
            Directory.CreateDirectory(logPath);
            var filePath = Path.Combine(logPath, $"app-{DateTime.Now:yyyyMMdd}.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(filePath, line, Encoding.UTF8);
        }
        catch
        {
        }
    }
}
