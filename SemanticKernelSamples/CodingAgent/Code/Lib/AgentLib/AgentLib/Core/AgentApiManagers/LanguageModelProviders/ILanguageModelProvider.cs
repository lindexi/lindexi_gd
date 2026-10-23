using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 语言模型接口，隔离具体 LLM 实现。
/// </summary>
public interface ILanguageModelProvider
{
    /// <summary>
    /// 获取模型提供商支持的所有模型列表。
    /// </summary>
    /// <returns>模型定义列表，包含模型的提供商、模型 ID、能力画像、上下文窗口等元数据。</returns>
    IReadOnlyList<ILanguageModel> GetSupportedModels();
}