using System.Net;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// 这里的演示代码需要用到 AzureAI 的支持，需要提前申请好，申请地址：https://aka.ms/oai/access
var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = File.ReadAllText(@"C:\lindexi\CA\Key"); // 请换成你的密钥

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion("GPT4o", endpoint, apiKey);

var kernel = kernelBuilder.Build();

var kernelFunction = kernel.CreateFunctionFromPrompt(@"我需要你帮忙将以下输入内容进行拆分，以下输入内容是一批词语，请将名单拆分为一行一个词语。如果输入的内容不是一个个词语，请输出拆分失败
[输入开始]
{{$Input}}
[输入结束]

拆分的词语：");

var kernelArguments = new KernelArguments();
kernelArguments["input"] = "潇潇，大壮";
var functionResult = await kernelFunction.InvokeAsync(kernel,kernelArguments);
var value = functionResult.GetValue<ChatMessageContent>();

Console.Read();

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory();
chatHistory.AddUserMessage("Hello, what is your code?");

var completion = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory);
try
{
    await foreach (var streamingChatMessageContent in completion)
    {
        Console.Write(streamingChatMessageContent.Content);
        if (streamingChatMessageContent is OpenAIStreamingChatMessageContent content)
        {
            if (content.FinishReason is not null)
            {
                if (content.FinishReason == CompletionsFinishReason.Stopped)
                {

                }
            }
        }
    }
}
catch (Exception e)
{
    if (e is HttpOperationException httpOperationException)
    {
        if (httpOperationException.StatusCode == HttpStatusCode.Unauthorized)
        {
            
        }
    }
}

Console.Read();
