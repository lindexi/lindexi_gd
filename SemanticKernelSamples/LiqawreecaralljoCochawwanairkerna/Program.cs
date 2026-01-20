// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using System.ComponentModel;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ImageGenerationOptions = OpenAI.Images.ImageGenerationOptions;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

var createVideoAgent = chatClient.CreateAIAgent(instructions: "这是一个生成图片的工具", tools:
[
    AIFunctionFactory.Create(GenerateImage)
]);
var createVideoTool = createVideoAgent.AsAIFunction(thread: createVideoAgent.GetNewThread(new InMemoryChatMessageStore()
{
    new ChatMessage(ChatRole.System,
        """
        你是一个图片生成工具，你懂得如何调用工具生成图片，调用工具的时候需要传入生成图片的提示词。你需要将收到的生成图片的需求，转换为图片生成工具的提示词输入，调用工具生成图片
        """)
}));

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools: [createVideoTool]);

var agentThread = aiAgent.GetNewThread(new InMemoryChatMessageStore()
{
    new ChatMessage(ChatRole.System,
        """
        你是一位学习辅导员，你将辅导学生做作业，对学生不会的题进行讲解。
        
        ## 输出视频
        
        为了能够让学生更好理解，你需要调用工具，生成讲解图片
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

[Description("创建图片。这是一个使用 LLM 大语言模型的图片生成工具，需要传入生成图片的描述")]
static async Task<string> GenerateImage([Description("生成图片的描述信息，这是一段提示词内容")] string imageDescription)
{
    var keyFile = @"C:\lindexi\Work\Doubao.txt";
    var key = File.ReadAllText(keyFile);

    var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
    {
        Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
    });

    ImageClient imageClient = openAiClient.GetImageClient("ep-20260120102721-c4pxb");

    var result = await imageClient.GenerateImageAsync(imageDescription, new ImageGenerationOptions()
    {
    });

    var generatedImage = result.Value;

    return $"生成图片： {generatedImage.ImageUri}";
}