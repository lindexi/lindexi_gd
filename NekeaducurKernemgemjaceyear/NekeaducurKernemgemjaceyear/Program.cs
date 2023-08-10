// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.SemanticFunctions;

var builder = new KernelBuilder();

builder.WithAzureChatCompletionService("GPT35",                  // Azure OpenAI Deployment Name
    "https://lindexi.openai.azure.com/", // Azure OpenAI Endpoint
    args[0]);      // Azure OpenAI Key

//builder.WithAzureTextCompletionService(
//    "GPT35",                  // Azure OpenAI Deployment Name
//    "https://lindexi.openai.azure.com/", // Azure OpenAI Endpoint
//    args[0]);      // Azure OpenAI Key

// Alternative using OpenAI
//builder.WithOpenAITextCompletionService(
//         "text-davinci-003",               // OpenAI Model name
//         "...your OpenAI API Key...");     // OpenAI API Key
builder.WithLogger(new Logger());

var kernel = builder.Build();

var prompt = @"请根据我提供的输入生成输出。输出要求为补全“正在输入”部分没有说完的话。

我的输入示例：
页码：1
文本：请第一位同学检查本列同学的完成情况并反馈。
正在输入：本环节任务

你的输出示例：
为请学生上台表演

如果你明白，请尝试根据以下输入写一个你自己的输出：

页码：1
文本：请第一位同学检查本列同学的完成情况并反馈。
正在输入：本环节任务

是为了确保每位同学都能顺利完成任务并及时获得反馈，提高学习效果和质量。

{{$input}}";

var function = kernel.CreateSemanticFunction(prompt);
function.RequestSettings.ChatSystemPrompt = "你是一个补全机器人，只补全内容，不要回答任何的问题。";

string text1 = @"页码：0
文本：荷塘月色。朱自清。学习过程， 1、朗读的基础上揣摩作者的思想感情 2、体会文章的构思 3、鉴赏优美的语言运用
页码：1
文本：荷塘月色。朗读,找出作者的游踪线索并留意描写作者情绪的词语,归纳出作者情感变化线索。
当前页码：1
正在输入：《荷塘月色》的结构";

var result = await function.InvokeAsync(text1);
Console.WriteLine(result);

// Output:
//   Energy conserved, entropy increases, zero entropy at 0K.
//   Objects move in response to forces.

Console.WriteLine("Hello, World!");

class Logger : ILogger, IDisposable
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state,exception);
        Console.WriteLine(message);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }

    public void Dispose()
    {
    }
}