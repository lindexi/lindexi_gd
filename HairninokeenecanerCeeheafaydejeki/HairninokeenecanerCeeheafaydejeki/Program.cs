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

        var semanticFunction = kernel.CreateSemanticFunction(@"你是一个变量命名器，你正在帮我给一个变量进行命名，要求命名符合 C# 的命名规范。我的需求是：

{{$input}}

请给出10个可能的命名：");

        var text =
            @"我正在制作一个 SemanticKernel 的 Function 对象，这个函数的作用就是提供文本总结能力，我需要将这个对象放入到属性里面，我需要为这个属性命名";

        var result = await semanticFunction.InvokeAsync(text);

        Console.WriteLine(result.Result);
    }
}
