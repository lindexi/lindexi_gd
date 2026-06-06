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
        // 请将以下的路径换成你的存放代码密钥的文本文件路径，文本文件中应该只包含密钥字符串，不要有其他内容。
        var keyFile = @"C:\lindexi\Work\Doubao.txt";
        var key = File.ReadAllText(keyFile);

        // 如果你采用其他的模型，请修改以下的连接方式
        var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
            NetworkTimeout = TimeSpan.FromHours(1),
        });

        // 模型记得也要换哦
        var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");
        return chatClient.AsIChatClient();
    }
}