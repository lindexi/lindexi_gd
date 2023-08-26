using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;

namespace HairninokeenecanerCeeheafaydejeki;

internal class Program
{
    static async Task Main(string[] args)
    {
        var kernelBuilder = new KernelBuilder();
        kernelBuilder.WithAzureChatCompletionService("GPT35", "https://lindexi.openai.azure.com/", args[0]);
        var kernel = kernelBuilder.Build();

        var semanticFunction = kernel.CreateSemanticFunction(@"请帮我总结以下文本内容，请不要回答任何问题

{{$input}}

以上内容的总结是：");

        var text =
            @"在当今快节奏的世界中，被无穷无尽的信息流淹没是很常见的。无论您是学生、研究人员还是专业人士，通常都有太多东西需要阅读和消化。值得庆幸的是，有一个创新的解决方案可以帮助您节省时间和精力。Chat GPT是一款基于人工智能的工具，可以帮助您快速有效地总结文本。在本文中，我们将探讨 Chat GPT 如何总结文本以及如何使用它来简化您的工作。

Chat GPT 可以总结文本吗？

是的，Chat GPT 可以对文本进行总结。Chat GPT 是一种语言模型，它使用深度学习算法对用户输入生成类似人类的响应。Chat GPT 可以通过分析内容并生成浓缩版来总结文本，同时保留主要思想和关键信息。这对于节省时间和精力非常有帮助，尤其是在阅读冗长的文章、研究论文或报告时。"; // From https://www.awyerwu.com/10075.html

        var result = await semanticFunction.InvokeAsync(text);

        Console.WriteLine(result.Result);
    }
}
