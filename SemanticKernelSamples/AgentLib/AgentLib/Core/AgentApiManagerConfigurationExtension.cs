using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Core;

/// <summary>
/// 提供 <see cref="AgentApiEndpointManager"/> 的扩展方法。
/// </summary>
public static class AgentApiManagerConfigurationExtension
{
    /// <summary>
    /// 从 JSON 配置文件加载配置到指定的 API 终结点管理器。
    /// </summary>
    /// <param name="agentApiEndpointManager">API 终结点管理器。</param>
    /// <param name="file">JSON 配置文件。</param>
    /// <returns>表示异步操作的任务。</returns>
    public static async Task LoadConfigurationFromJsonFileAsync(this AgentApiEndpointManager agentApiEndpointManager, FileInfo file)
    {
        var configuration = await AgentApiManagerConfiguration.FromJsonFileAsync(file);
        agentApiEndpointManager.LoadConfiguration(configuration);
    }
}