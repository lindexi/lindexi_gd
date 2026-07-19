
using System.ClientModel;

using AgentLib.AgentExtensions;

using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"c:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var promptFile = Path.Join(AppContext.BaseDirectory, "Prompt.txt");
var prompt = File.ReadAllText(promptFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")     //  
});
var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg"); // 请换成你自己的模型

var agent = chatClient.AsAIAgent();

var image1 = @"c:\lindexi\Work\TestFile\1.jpg";
var image2 = @"c:\lindexi\Work\TestFile\2.jpg";

var image1DataContent = await DataContent.LoadFromAsync(image1);
var image2DataContent = await DataContent.LoadFromAsync(image2);

await foreach (var agentResponseUpdate in agent.RunStreamingAsync([new ChatMessage(ChatRole.System, prompt), new ChatMessage(ChatRole.User, [
                   image1DataContent,
                   image2DataContent,
                   new TextContent("这是两个人的盘，我想问的是今年2026年能否合适生娃。今天是2026年7月19日。我要你先帮我分析这个盘")])]))
{
    Console.Write(agentResponseUpdate.Text);
}

//await agent.RunStreamingAndLogToConsoleAsync();

Console.WriteLine("Hello, World!");
