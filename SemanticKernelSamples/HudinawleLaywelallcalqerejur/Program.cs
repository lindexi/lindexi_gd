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

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory();
chatHistory.AddUserMessage("Hello, what is your code?");

var completion = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory);
await foreach (var streamingChatMessageContent in completion)
{
    Console.Write(streamingChatMessageContent.Content);
    if(streamingChatMessageContent is OpenAIStreamingChatMessageContent content)
    {
        if (content.FinishReason is not null)
        {
            if (content.FinishReason == CompletionsFinishReason.Stopped)
            {
                
            }
        }
    }
}

Console.Read();
