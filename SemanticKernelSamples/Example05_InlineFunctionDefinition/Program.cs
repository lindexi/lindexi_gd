using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
var logger = loggerFactory.CreateLogger("SemanticKernel");

// 这里的演示代码需要用到 AzureAI 的支持，需要提前申请好，申请地址：https://aka.ms/oai/access

var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = args[0]; // 请换成你的密钥

IKernel kernel = new KernelBuilder()
    .WithLogger(logger)
    .WithAzureChatCompletionService("GPT35", endpoint, apiKey)
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

var excuseFunction = kernel.CreateSemanticFunction(FunctionDefinition, maxTokens: 200,
    // 温度高一些，这样 GPT 才会乱说
    temperature: 1);

var result = await excuseFunction.InvokeAsync("我错过了篮球赛");
Console.WriteLine(result); // 我被邀请参加了一个秘密的超级英雄训练营，我必须去拯救世界！