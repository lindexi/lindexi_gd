// See https://aka.ms/new-console-template for more information

using System.ClientModel;
using OpenAI;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
    NetworkTimeout = TimeSpan.FromHours(1),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

// 什么的语气讲
// 每句话的中间加 停顿几秒

Console.WriteLine("Hello, World!");

