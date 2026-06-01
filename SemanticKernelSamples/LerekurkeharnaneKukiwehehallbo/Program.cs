using System.Text.Json;
using AgentLib.AgentExtensions;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

var configFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";

var agentApiEndpointManager = new AgentApiEndpointManager();

await agentApiEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(configFile));

agentApiEndpointManager.PrimaryModel =
    agentApiEndpointManager.GetBestModel(model => model.ModelDefinition.ModelName.Contains("M3"));

IChatClient chatClient = await agentApiEndpointManager.PrimaryModel.GetChatClientAsync();

var skillFolder = @"F:\lindexi\Code\dotnet-skills\plugins\dotnet-ai\skills\";

#pragma warning disable MAAI001
var agentSkillsProvider = new AgentSkillsProvider(skillFolder);

ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    AIContextProviders = [agentSkillsProvider],
    ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
    {
#pragma warning disable MEAI001
        ChatReducer = new SummarizingChatReducer(chatClient, 10, 5)
#pragma warning restore MEAI001
    }),
    ChatOptions = new ChatOptions()
    {
        
    }
});

await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "你好，我准备开发 MCP 服务器，请你给我一些建议")]);

Console.WriteLine("Hello, World!");


class FakeChatReducer : IChatReducer
{
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        return Task.FromResult(messages);
    }
}