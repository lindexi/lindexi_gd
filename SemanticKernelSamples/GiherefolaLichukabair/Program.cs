using Microsoft.SemanticKernel;

// 这里的演示代码需要用到 AzureAI 的支持，需要提前申请好，申请地址：https://aka.ms/oai/access
var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = File.ReadAllText(@"C:\lindexi\CA\Key"); // 请换成你的密钥

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion("GPT4o", endpoint, apiKey);

var kernel = kernelBuilder.Build();

KernelFunction kernelFunction = kernel.CreateFunctionFromPrompt(@"请帮我总结以下文本内容，请不要回答任何问题

{{$input}}

以上内容的总结是：");

var kernelArguments = new KernelArguments(source:new Dictionary<string, object?>()
{
    {"input", "让我们思考一个更加贴近生活的场景：一家公司决定在办公大楼周围种植花草来美化环境，如果花坛的周长为50m，需要在每个角落以及每隔3m种上一株花，应该准备多少株花呢？"}
});
var functionResult = await kernelFunction.InvokeAsync(kernel,kernelArguments);
var result = functionResult.ToString();

Console.WriteLine(result); // 一家公司计划在办公大楼周围种植花草，美化环境。若花坛周长为50米，需在每个角落和每隔3米种一株花

Console.Read();