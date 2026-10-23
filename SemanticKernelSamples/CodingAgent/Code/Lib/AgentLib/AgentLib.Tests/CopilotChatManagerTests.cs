using AgentLib.Core.AgentApiManagers;
using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerTests
{
    [TestMethod(DisplayName = "真实 LLM 调试 SendMessage 工具调用和自定义 ChatReducer")]
    [Ignore("这是需要真实 LLM 服务的手动调试测试，避免常规测试运行。")]
    public async Task InnerTest()
    {
        var copilotChatManager = new CopilotChatManager();
        copilotChatManager.AgentApiEndpointManager.LoadConfiguration(LindexiAgentConfiguration.LoadDefault());

        var chatReducer = new DebugChatReducer();
        var weatherTool = AIFunctionFactory.Create(
            GetWeather,
            name: nameof(GetWeather),
            description: "获取指定城市的当前天气。city 表示城市名称。");

        var request = new SendMessageRequest("请调用工具获取广州今天的天气，然后用一句中文告诉我结果。")
        {
            ChatReducer = chatReducer,
            RequirePerServiceCallChatHistoryPersistence = true,
            Tools = [weatherTool],
            AppendDefaultTools = false,
        };

        SendMessageResult result = copilotChatManager.SendMessage(request);

        await result.RunTask;
    }

    private static string GetWeather(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("城市名称不能为空。", nameof(city));
        }

        return $"{city} 当前天气：晴，温度 26℃，湿度 58%，东北风 2 级。";
    }

    private sealed class DebugChatReducer : IChatReducer
    {
        public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(messages);

            List<ChatMessage> messageList = messages.ToList();
            return Task.FromResult<IEnumerable<ChatMessage>>(messageList);
        }
    }
}