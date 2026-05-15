using Microsoft.Extensions.AI;

using OpenAI;

using System;
using System.ClientModel;
using System.IO;

namespace PptxGenerator;

public class ChatClientCreator
{
    public IChatClient GetChatClient()
    {
        var keyFile = @"C:\lindexi\Work\Doubao.txt";
        var key = File.ReadAllText(keyFile);

        var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
            NetworkTimeout = TimeSpan.FromHours(1),
        });

        var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");
        return chatClient.AsIChatClient();
    }
}