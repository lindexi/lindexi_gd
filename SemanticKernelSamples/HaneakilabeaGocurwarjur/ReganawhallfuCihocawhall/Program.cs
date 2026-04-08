// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

ChatClientAgent aiAgent = chatClient.AsAIAgent();

ChatMessage message = new ChatMessage(ChatRole.User, "请讲一个笑话");

bool isThinking = false;

await foreach (var agentResponseUpdate in aiAgent.RunStreamingAsync(message))
{
    foreach (AIContent aiContent in agentResponseUpdate.Contents)
    {
        if (aiContent is TextReasoningContent textReasoningContent)
        {
            Console.Write(textReasoningContent.Text);
            isThinking = true;
        }
        else if(aiContent is TextContent textContent)
        {
            if (string.IsNullOrEmpty(textContent.Text))
            {
                continue;
            }

            if (isThinking)
            {
                Console.WriteLine();
                Console.WriteLine("--------");
            }

            Console.Write(textContent.Text);
            isThinking = false;
        }
    }
}

Console.Read();