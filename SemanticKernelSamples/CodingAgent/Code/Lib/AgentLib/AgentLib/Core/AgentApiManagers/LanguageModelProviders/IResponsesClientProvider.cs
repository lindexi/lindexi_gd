#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 提供 OpenAI Responses 客户端获取能力。
/// </summary>
public interface IResponsesClientProvider
{
    /// <summary>
    /// 获取当前模型的 Responses 客户端上下文。
    /// </summary>
    /// <returns>包含原生客户端及已解析模型标识的不可变上下文。</returns>
    Task<ResponsesClientContext> GetResponsesClientAsync();
}
