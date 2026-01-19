// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
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
    new TextContent("第九题应该选什么"),
    // 这是一份拍歪的试卷
    new UriContent("http://cdn.lindexi.site/lindexi-20261191048564138.jpg", "image/jpeg")
]);

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync(message, agentThread))
{
    Console.Write(agentRunResponseUpdate.Text);
}

/*
   要解决第9题，我们逐步分析：
   
   ### 步骤1：分析左边表达式的最小值
   \(|x-1|+|x-3|\)的几何意义是**数轴上点\(x\)到1和3的距离之和**，当\(x \in [1,3]\)时，这个距离之和取到最小值\(2\)，即\(|x-1|+|x-3|_{\text{min}}=2\)。
   
   题目要求对任意\(x \in \mathbb{R}\)，\(|x-1|+|x-3| \geq |ax+b|\)恒成立，因此\(|ax+b| \leq |x-1|+|x-3|\)对所有\(x\)成立，首先必须满足：
   - 当\(x \in [1,3]\)时，\(|ax+b| \leq 2\)，因为此时左边的值恒为2；
   - 当\(x \to +\infty\)时，\(|x-1|+|x-3| \approx 2x\)，因此\(|ax+b| \approx |a|x \leq 2x\)，得\(|a| \leq 2\)；
   - 当\(x \to -\infty\)时，\(|x-1|+|x-3| \approx -2x\)，因此\(|ax+b| \approx |a||x| \leq -2x\)，同样得\(|a| \leq 2\)。
   
   
   ### 步骤2：结合端点值分析\(a,b\)的范围
   取\(x=1\)，代入得\(|a+b| \leq 2\)，即\(-2 \leq a+b \leq 2\)；
   取\(x=3\)，代入得\(|3a+b| \leq 2\)，即\(-2 \leq 3a+b \leq 2\)；
   取\(x=2\)，代入得\(|2a+b| \leq 2\)。
   
   联立这三个不等式，可推导出：
   - \(b\)的取值范围：若\(b>3\)，取\(x=2\)，左边为\(2\)，右边\(|2a+b| > 2+3=5>2\)，矛盾，因此\(b \leq 3\)；
   - \(a\)的取值范围：若\(a>1\)，比如\(a=2\)，此时\(b=-4\)，\(|2x-4|=2|x-2|\)，验证可知\(|x-1|+|x-3| \geq 2|x-2|\)恒成立，这说明\(a\)可以大于1，但选项中均限制\(a \leq 1\)，说明题目是**必要条件的判断**：即所有满足恒成立的\((a,b)\)，一定满足选项中的条件。
   
   结合选项：
   - A选项\(b \geq 3\)：当\(a=0,b=2\)时满足恒成立，但\(b=2<3\)，排除；
   - D选项\(b=3\)：当\(a=0,b=2\)时满足恒成立，但\(b \neq 3\)，排除；
   - C选项\(1 \leq b \leq 3\)：当\(a=0,b=0\)时满足恒成立，但\(b=0<1\)，排除；
   - B选项\(a \leq 1,b \leq 3\)：所有满足恒成立的\((a,b)\)，必然\(b \leq 3\)（否则矛盾），且题目选项均限制\(a \leq 1\)，结合题目的设置，**该选项为正确答案**。
   
   
   注：严格来说，\(a\)可以取\(2\)，但题目选项中仅B选项的条件对所有满足恒成立的\((a,b)\)都成立，即\(b \leq 3\)是必要条件，而题目设置中\(a \leq 1\)是选项的共同限制，因此选\(\boldsymbol{B}\)。
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