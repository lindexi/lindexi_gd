// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
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

var mainWord = "玉米";
var spyWord = "小米";

var list = new List<List<ChatMessage>>();

var count = 3;

for (int i = 0; i < count; i++)
{
    var skillText =
        $"""
         你正在参与一个“谁是间谍”的游戏。每人每轮只能说一句话描述自己拿到的词语（不能直接说出那个词语），既不能让卧底发现，也要给同伴以暗示。当你能够确定某个人是“间谍”的时候，你可以去指认他，如果指认错误，你将会出局，请谨慎指认。如果指认成功，你将赢得比赛。如果你发现自己是卧底，也可以指认自己。
         你是第 {i} 人
         你拿到的词语是： "{(i == count - 1 ? spyWord : mainWord)}"
         """;
    list.Add(new List<ChatMessage>()
    {
        new ChatMessage(ChatRole.System,skillText),
        new ChatMessage(new ChatRole("裁判"), "现在游戏开始")
    });
}

ChatClientAgent aiAgent = chatClient.AsAIAgent();

for (int i = 0; i < int.MaxValue; i++)
{
    Console.WriteLine($"第 {i} 局");
    Console.WriteLine("=============");

    foreach (var chatMessageList in list)
    {
        chatMessageList.Add(new ChatMessage(new ChatRole("裁判"), $"第 {i} 回合开始，请开始你们的发言"));
    }

    for (var index = 0; index < list.Count; index++)
    {
        var chatMessageList = list[index];

        var textStringBuilder = new StringBuilder();

        Console.WriteLine($"第 {index} 人");

        await foreach (var agentRunResponseUpdate in aiAgent.RunReasoningStreamingAsync(chatMessageList))
        {
            if (agentRunResponseUpdate.IsFirstThinking)
            {
                Console.WriteLine("思考：");
            }

            if (agentRunResponseUpdate.Reasoning is not null)
            {
                Console.Write(agentRunResponseUpdate.Reasoning);
            }

            if (agentRunResponseUpdate.IsThinkingEnd)
            {
                Console.WriteLine();
                Console.WriteLine("--------");
            }

            var text = agentRunResponseUpdate.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Console.Write(text);
                textStringBuilder.Append(text);
            }
        }

        Console.WriteLine();
        Console.WriteLine("-------------");

        var currentText = textStringBuilder.ToString();
        if (string.IsNullOrEmpty(currentText))
        {

        }
        
        foreach (var temp in list)
        {
            var currentChatMessage = new ChatMessage(new ChatRole($"第 {index} 人"), currentText);

            if (ReferenceEquals(temp, chatMessageList))
            {
                currentChatMessage.Role = ChatRole.Assistant;
            }

            temp.Add(currentChatMessage);
        }
    }

    Console.WriteLine($"第 {i} 回合结束，请根据以上各人发言，请你猜测谁可能是卧底");

    for (var index = 0; index < list.Count; index++)
    {
        var chatMessageList = list[index];

        chatMessageList.Add(new ChatMessage(new ChatRole("裁判"), $"第 {i} 回合结束，请根据以上各人发言，请你猜测谁可能是卧底"));

        var textStringBuilder = new StringBuilder();

        Console.WriteLine($"第 {index} 人");

        await foreach (var agentRunResponseUpdate in aiAgent.RunReasoningStreamingAsync(chatMessageList))
        {
            if (agentRunResponseUpdate.IsFirstThinking)
            {
                Console.WriteLine("思考：");
            }

            if (agentRunResponseUpdate.Reasoning is not null)
            {
                Console.Write(agentRunResponseUpdate.Reasoning);
            }

            if (agentRunResponseUpdate.IsThinkingEnd)
            {
                Console.WriteLine();
                Console.WriteLine("--------");
            }

            var text = agentRunResponseUpdate.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Console.Write(text);
                textStringBuilder.Append(text);
            }
        }

        Console.WriteLine();
        Console.WriteLine("-------------");

        var currentText = textStringBuilder.ToString();
        var currentChatMessage = new ChatMessage(ChatRole.Assistant, currentText);
        chatMessageList.Add(currentChatMessage);
    }

    Console.ReadLine();
}

Console.WriteLine();

Console.Read();