// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;

using VolcEngineSdk;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

var createVideoAgent = chatClient.CreateAIAgent(instructions: "这是一个生成视频的工具", tools:
[
    AIFunctionFactory.Create(GenerateVideo)
]);
var createVideoTool = createVideoAgent.AsAIFunction(thread: createVideoAgent.GetNewThread());

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools: [createVideoTool]);

var agentThread = aiAgent.GetNewThread(new InMemoryChatMessageStore()
{
    new ChatMessage(ChatRole.System,
        """
        你是一位学习辅导员，你将辅导学生做作业，对学生不会的题进行讲解。
        
        ## 格式要求
        
        如果你需要输出公式，请将公式使用 <Formula></Formula> 标签包围。例如 <Formula>\(x \in \mathbb{R}\)</Formula>。哪怕输出的是简单的公式，也都要使用 <Formula></Formula> 标签包围。例如 <Formula>\(1+1=2\)</Formula> 。输出的公式请严格符合 Latex 规范
        
        ## 输出视频
        
        为了能够让学生更好理解，你需要调用工具，生成讲解视频
        """)
});

ChatMessage message = new(ChatRole.User,
[
    new TextContent("为什么第六题错了"),
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

[Description("创建视频。这是一个使用 LLM 大语言模型的视频生成工具，需要传入生成视频的描述")]
static async Task<string> GenerateVideo([Description("生成视频的描述信息，这是一段提示词内容")] string videoDescription)
{
    var keyFile = @"C:\lindexi\Work\Doubao.txt";
    var key = File.ReadAllText(keyFile);
    var arkClient = new ArkClient(key);

    var createTaskResult = await arkClient.ContentGeneration.Tasks.Create("ep-20260119192612-ndzvr", [
        new ArkTextContent(videoDescription),
    ]);

    if (createTaskResult.TaskId is { } taskId)
    {
        while (true)
        {
            ArkGetTaskResult arkGetTaskResult = await arkClient.ContentGeneration.Tasks.Get(taskId);
            Console.WriteLine($"状态: {arkGetTaskResult.Status}");
            var videoUrl = arkGetTaskResult.Content?.VideoUrl;
            if (!string.IsNullOrEmpty(videoUrl))
            {
                return $"视频下载地址： {videoUrl}";
            }
        }
    }

    return "生成视频失败";
}