using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

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
    .Build();

const string FunctionDefinition =
"""
我有这样一段文本：
{{$input}}
请你写一个正则表达式字符串，用来提取出日期

正则表达式字符串:
""";

kernel.CreateSemanticFunction(FunctionDefinition, maxTokens: 200, temperature: 0.5, functionName: "BuildRegexText");
kernel.ImportSkill(new TextSkill());

var data1 =
    """
在2023年9月1号开始上课
在2023年9月2号开始准备教材
在2023年9月3号完成作业
""";

var data2 =
    """
在9.1.2023开始上课
在9.2.2023开始准备教材
在9.3.2023完成作业
""";

Console.WriteLine($"开始执行解析 data1");
await RunAsync(data1);
Console.WriteLine($"开始执行解析 data2");
await RunAsync(data2);

async Task RunAsync(string data)
{
    await kernel.RunAsync
    (
        data,
        kernel.Skills.GetFunction("TakeFirstLine"),
        kernel.Skills.GetFunction("BuildRegexText"),
        kernel.Skills.GetFunction("RegexMatchText")
    );
}

class TextSkill
{
    [SKFunction]
    public string TakeFirstLine(string input, SKContext context)
    {
        context.Variables["text"] = input;
        var stringReader = new StringReader(input);
        return stringReader.ReadLine() ?? string.Empty;
    }

    [SKFunction]
    public void RegexMatchText(string input, string text, SKContext context)
    {
        foreach (Match match in Regex.Matches(text,input))
        {
            Console.WriteLine(match.Value);
        }
    }
}