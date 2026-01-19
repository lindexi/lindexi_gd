// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text.Json;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools:
[
    //AIFunctionFactory.Create(GetRegionId),
    //AIFunctionFactory.Create(DeleteElement),
]);

var agentThread = aiAgent.GetNewThread(new InMemoryChatMessageStore()
{
    new ChatMessage(ChatRole.System,"你是一位学习辅导员，你将辅导学生做作业，对学生不会的题进行讲解")
});

ChatMessage message = new(ChatRole.User,
[
    new TextContent("我第三题不会做，你和我讲一下"),
    new UriContent("http://cdn.lindexi.site/lindexi-20261191019524327.jpg", "image/jpeg")
]);

bool? isThinking = null;
bool isFirstResponse = true;

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync(message, agentThread))
{
    // -		RawRepresentation	{OpenAI.Chat.StreamingChatCompletionUpdate}	object {OpenAI.Chat.StreamingChatCompletionUpdate}
    // -		agentRunResponseUpdate.RawRepresentation	[{assistant}] Text = ""	object {Microsoft.Extensions.AI.ChatResponseUpdate}
    // 
    var contentIsEmpty = string.IsNullOrEmpty(agentRunResponseUpdate.Text);

    if (contentIsEmpty && agentRunResponseUpdate.RawRepresentation is Microsoft.Extensions.AI.ChatResponseUpdate streamingChatCompletionUpdate)
    {
        if (streamingChatCompletionUpdate.RawRepresentation is OpenAI.Chat.StreamingChatCompletionUpdate chatCompletionUpdate)
        {
#pragma warning disable SCME0001
            ref JsonPatch patch = ref chatCompletionUpdate.Patch;
            if (patch.TryGetJson("$.choices[0].delta"u8, out var data))
            {
                var jsonElement = JsonElement.Parse(data.Span);
                if (jsonElement.TryGetProperty("reasoning_content", out var choicesElement))
                {
                    if (isThinking is null)
                    {
                        isThinking = true;
                        Console.WriteLine("思考：");
                    }

                    if (isThinking is true)
                    {
                        Console.Write(choicesElement);
                    }
                }
            }

#pragma warning restore SCME0001
        }
    }

    if (!contentIsEmpty)
    {
        if (isThinking is true && isFirstResponse)
        {
            Console.WriteLine();
            Console.WriteLine("--------");
        }

        isFirstResponse = false;
        isThinking = false;

        Console.Write(agentRunResponseUpdate.Text);
    }
}

/*
   我们先回忆一下平移和旋转的特点：
   平移是物体**沿着直线做直线运动**，移动后物体的方向、形状、大小都不变；
   旋转是物体**绕着一个点/轴做圆周运动**，运动时物体的方向会发生改变。
   
   现在逐个分析第三题的小题：
   (1) 教室门打开关上：门是绕着门轴做转动，属于旋转，所以选②。
   (2) 电风扇的运动：风扇叶片绕着中心轴转圈，是旋转，选②；推拉窗是沿着直线左右移动，属于平移，选①。
   (3) 找平移运动：
   ①转动的呼啦圈是绕着身体转圈，是旋转；②电风扇运动是旋转；③拨算珠时，算珠沿着直线移动，是平移，所以选③。
   (4) 左图的三角形和右边的组合图形：三角形的方向发生了改变，是绕着一个点旋转后得到的，所以选②。
 */
Console.WriteLine();

Console.Read();


//[Description("获取某个区域范围的元素 Id 号")]
//static string GetRegionId([Description("区域范围，采用 X Y 宽度 高度 的格式")]string region)
//{
//    return "F1";
//}

//[Description("删除给定 Id 号的元素")]
//static void DeleteElement(string elementId)
//{
//
//}

//[Description("移动给定 Id 号的元素")]
//static void MoveElement(string elementId)
//{
//
//}