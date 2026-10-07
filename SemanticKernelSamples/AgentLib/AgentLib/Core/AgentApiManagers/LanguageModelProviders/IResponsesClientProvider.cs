#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using OpenAI.Responses;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 提供 OpenAI Responses 客户端获取能力。
/// </summary>
public interface IResponsesClientProvider
{
    /// <summary>
    /// 获取使用当前模型连接配置的 Responses 客户端；请求时需指定模型 ID。
    /// </summary>
    /// <returns>Responses 客户端实例。</returns>
    Task<ResponsesClient> GetResponsesClientAsync();
}
