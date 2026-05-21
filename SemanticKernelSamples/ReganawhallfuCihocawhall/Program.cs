// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

ChatClientAgent aiAgent = chatClient.AsAIAgent();

var imageFilePath = @"C:\lindexi\Work\ImageTest\ImageUrl.txt";
var imageUrl = await File.ReadAllTextAsync(imageFilePath);

ChatMessage message = 


            new ChatMessage(ChatRole.User,
                [
                    new TextContent("根据以下图片，生成表格的 Markdown 文档内容"),
                    new UriContent(imageUrl,"image/png")
                ])
        ;

await foreach (var agentResponseUpdate in aiAgent.RunStreamingAsync(message))
{
    foreach (var aiContent in agentResponseUpdate.Contents)
    {
        if (aiContent is TextReasoningContent textReasoningContent)
        {
            if(!string.IsNullOrEmpty(textReasoningContent.Text))
            {
                Console.Write(textReasoningContent.Text);
            }
        }
        else if(aiContent is TextContent textContent)
        {
            if(!string.IsNullOrEmpty(textContent.Text))
            {
                Console.Write(textContent.Text);
            }
        }
    }
}

Console.WriteLine();

Console.Read();