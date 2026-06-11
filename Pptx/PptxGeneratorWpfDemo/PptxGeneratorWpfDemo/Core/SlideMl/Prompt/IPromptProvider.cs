namespace PptxGenerator;

/// <summary>
/// 提示词提供者接口，负责构建 SlideML 生成所需的系统提示词和用户提示词。
/// 抽取为接口以便被独立评估和替换。
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// 构建 SlideML 排版引擎系统提示词。
    /// </summary>
    string BuildSystemPrompt();

    /// <summary>
    /// 构建初始用户提示词，包裹用户的自然语言需求。
    /// </summary>
    /// <param name="userPrompt">用户自然语言需求描述。</param>
    string BuildInitialUserPrompt(string userPrompt);
}
