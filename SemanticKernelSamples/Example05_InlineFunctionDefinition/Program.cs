using Microsoft.SemanticKernel;

// 这里的演示代码需要用到 AzureAI 的支持，需要提前申请好，申请地址：https://aka.ms/oai/access

var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = args[0]; // 请换成你的密钥

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion("GPT4o", endpoint, apiKey)
    // 当然，这里也可以支持 OpenAI 的服务。或者是其他第三方的服务
    //.WithOpenAIChatCompletionService()
    .Build();


const string FunctionDefinition = @"
为给定的事件想出一个创造性的理由或借口。
要有创意，要有趣。让你的想象力尽情驰骋。

事情：我要迟到了。
借口：我被长颈鹿帮绑架了。

事情：我有一年没去健身房了
借口：我一直忙着训练我的宠物龙。

事情： {{$input}}
借口：";

KernelFunction kernelFunction = kernel.CreateFunctionFromPrompt(FunctionDefinition);

var kernelArguments = new KernelArguments();
kernelArguments.Add("input", "我错过了篮球赛");
var result = await kernelFunction.InvokeAsync(kernel, kernelArguments);
Console.WriteLine(result); // 我被邀请参加了一个秘密的超级英雄训练营，我必须去拯救世界！