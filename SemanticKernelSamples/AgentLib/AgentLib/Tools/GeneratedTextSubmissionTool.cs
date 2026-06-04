using System;
using System.ComponentModel;
using System.Reflection;

using Microsoft.Extensions.AI;

namespace AgentLib.Tools;

/// <summary>
/// 用于接收大模型通过工具调用提交的完整文本结果。
/// </summary>
public sealed class GeneratedTextSubmissionTool
{
    /// <summary>
    /// 已提交的文本内容。
    /// </summary>
    public string? SubmittedText { get; private set; }

    /// <summary>
    /// 是否已提交文本。
    /// </summary>
    public bool HasSubmittedText => !string.IsNullOrWhiteSpace(SubmittedText);

    /// <summary>
    /// 创建一个用于提交文本的工具函数。
    /// </summary>
    /// <param name="name">工具名称。</param>
    /// <param name="description">工具描述。</param>
    /// <returns>可调用的 AI 函数。</returns>
    public AIFunction CreateTool(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        MethodInfo methodInfo = GetType().GetMethod(nameof(SubmitText))
                                ?? throw new InvalidOperationException($"未找到 {nameof(SubmitText)} 方法。");
        return AIFunctionFactory.Create(methodInfo, this, name, description, serializerOptions: null);
    }

    [Description("提交处理后的完整文本内容。")] 
    public string SubmitText([Description("处理后的完整文本内容。请传入最终结果，不要附加解释。")] string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        SubmittedText = text;
        return "已收到处理后的文本。";
    }
}
