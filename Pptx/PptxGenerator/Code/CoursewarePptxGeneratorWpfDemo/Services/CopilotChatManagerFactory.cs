using System.IO;
using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Creates language-model chat managers without page rendering dependencies.
/// </summary>
public sealed class CopilotChatManagerFactory : ICopilotChatManagerFactory
{
    private const string ConfigurationFilePath = @"C:\lindexi\Work\Key\AgentConfiguration.json";
    private const string ModelName = "qwen3.7-plus";

    private readonly WpfDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotChatManagerFactory" /> class.
    /// </summary>
    public CopilotChatManagerFactory()
    {
        _dispatcher = WpfDispatcher.Instance;
    }

    /// <inheritdoc />
    public async Task<CopilotChatManager> CreateAsync(
        AgentWorkload workload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configurationFile = new FileInfo(ConfigurationFilePath);
        if (!configurationFile.Exists)
        {
            throw new FileNotFoundException("语言模型配置文件不存在。", configurationFile.FullName);
        }

        var chatManager = new CopilotChatManager { MainThreadDispatcher = _dispatcher };
        var endpointManager = chatManager.AgentApiEndpointManager;
        await endpointManager.LoadConfigurationFromJsonFileAsync(configurationFile).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var modelName = GetModelName(workload);
        var model = endpointManager.GetModel(modelName);
        if (model is null)
        {
            throw new InvalidOperationException($"未找到工作负载 {workload} 指定的语言模型：{modelName}");
        }

        endpointManager.PrimaryModel = model;
        return chatManager;
    }

    private static string GetModelName(AgentWorkload workload)
    {
        return workload switch
        {
            AgentWorkload.ThemeAnalysis or AgentWorkload.SlideGeneration or AgentWorkload.Evaluation => ModelName,
            _ => throw new ArgumentOutOfRangeException(nameof(workload), workload, "未知的语言模型工作负载。"),
        };
    }
}