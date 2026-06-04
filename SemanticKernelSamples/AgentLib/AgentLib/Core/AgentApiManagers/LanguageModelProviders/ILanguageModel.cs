using AgentLib.Core.AgentApiManagers.Contexts;
using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 表示一个语言模型，提供模型定义和聊天客户端创建能力。
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// 模型的元数据定义。
    /// </summary>
    ModelDefinition ModelDefinition { get; }

    /// <summary>
    /// 异步获取与此模型关联的聊天客户端。
    /// </summary>
    /// <returns>聊天客户端实例。</returns>
    Task<IChatClient> GetChatClientAsync();
}