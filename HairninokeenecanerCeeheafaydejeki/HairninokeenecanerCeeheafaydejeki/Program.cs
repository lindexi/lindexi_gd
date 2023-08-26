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
        var result = await kernel.CreateSemanticFunction("你好").InvokeAsync();
        Console.WriteLine(result.Result);
    }
}
