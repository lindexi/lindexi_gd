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

var prompt = @"请根据我提供的输入进行分类。请仅在以下分类类型中选取一个最匹配的类型，不同的分类使用；分割。如果没有任何一个类型匹配，请输出`无匹配`。注意，你的输入应该只包含分类的类型名，不包含其他内容。

生成页面；生成图片；生成形状；填充形状内容；

{{$input}}";

var function = kernel.CreateSemanticFunction(prompt);
function.RequestSettings.ChatSystemPrompt = "你是一个分类器，只分类内容，不要回答任何的问题。";

string text1 = @"我想要创建出一页课件";

var result = await function.InvokeAsync(text1);
Console.WriteLine(result);

string text2 = @"我想要画一个页面背景图";
result = await function.InvokeAsync(text2);
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