// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

ChatClientAgent aiAgent = chatClient.CreateAIAgent();

var agentThread = aiAgent.GetNewThread(new InMemoryChatMessageStore()
{
    new ChatMessage(ChatRole.System,
        """
        你是一位学习辅导员，你将辅导学生做作业，对学生不会的题进行讲解。
        
        ## 格式要求
        
        如果你需要输入公式，请将公式使用 <Formula></Formula> 标签包围。例如 <Formula>\(x \in \mathbb{R}\)</Formula>。输出的公式请严格符合 Latex 规范
        """)
});

ChatMessage message = new(ChatRole.User,
[
    new TextContent("为什么第五题和第六题错了"),
    // 这是一份手机拍的试卷，且有学生写的内容
    new UriContent("http://cdn.lindexi.site/lindexi-20261191737286334.jpg", "image/jpeg"),
]);

await foreach (var agentRunResponseUpdate in aiAgent.RunReasoningStreamingAsync(message, agentThread))
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
    }
}

Console.WriteLine();

Console.Read();