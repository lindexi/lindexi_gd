using System;
using System.ComponentModel;
using System.Reflection;

using Microsoft.Extensions.AI;

namespace AvaloniaAgentLib.Tools;

/// <summary>
/// 用于接收大模型通过工具调用提交的完整文本结果。
/// </summary>
public sealed class GeneratedTextSubmissionTool
{
    public string? SubmittedText { get; private set; }

    public bool HasSubmittedText => !string.IsNullOrWhiteSpace(SubmittedText);

    public AIFunction CreateTool(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        MethodInfo methodInfo = GetType().GetMethod(nameof(SubmitText))
                                ?? throw new InvalidOperationException($"未找到 {nameof(SubmitText)} 方法。");
        return AIFunctionFactory.Create(methodInfo, this, name, description, serializerOptions: null);
    }

    [Description("提交处理后的完整文本内容。")]
    public string SubmitText([Description("处理后的完整文本内容。请传入最终结果，不要附加解释。" )] string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        SubmittedText = text;
        return "已收到处理后的文本。";
    }
}
