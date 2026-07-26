using System.Runtime.InteropServices;
using System.Text;

namespace DeepSeekWpf.Services;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly ISettingsService _settingsService;
    private readonly IAgentModelService _modelService;
    private readonly IAppLogger _logger;

    public DiagnosticsService(
        ISettingsService settingsService,
        IAgentModelService modelService,
        IAppLogger logger)
    {
        _settingsService = settingsService;
        _modelService = modelService;
        _logger = logger;
    }

    public Task<string> CreateSummaryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = _settingsService.CurrentSettings;
        var builder = new StringBuilder()
            .AppendLine("DeepSeekWpf 本地诊断摘要")
            .AppendLine($"生成时间: {DateTimeOffset.Now:O}")
            .AppendLine($"应用版本: {typeof(App).Assembly.GetName().Version}")
            .AppendLine($"运行时: {RuntimeInformation.FrameworkDescription}")
            .AppendLine($"操作系统: {RuntimeInformation.OSDescription}")
            .AppendLine($"设置文件: {_settingsService.SettingsFilePath}")
            .AppendLine($"Agent 配置文件: {_modelService.ConfigurationFilePath}")
            .AppendLine($"缓存目录: {settings.CachePath}")
            .AppendLine($"数据目录: {settings.DataPath}")
            .AppendLine($"日志目录: {settings.LogPath}")
            .AppendLine($"已选模型: {settings.SelectedModelSpecifier}")
            .AppendLine($"已注册模型数: {_modelService.RegisteredModels.Count}")
            .AppendLine($"设置加载错误: {FormatError(_settingsService.LastLoadError)}")
            .AppendLine($"日志写入错误: {FormatError(_logger.LastWriteError)}");

        return Task.FromResult(builder.ToString());
    }

    public Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        return _logger.ClearLogsAsync(cancellationToken);
    }

    private static string FormatError(Exception? exception)
    {
        return exception is null ? "无" : $"{exception.GetType().Name}: {exception.Message}";
    }
}