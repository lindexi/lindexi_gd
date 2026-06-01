using System.Text.Json;
using AgentLib.AgentExtensions;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

var agentApiEndpointManager = new AgentApiEndpointManager();

var agentApiManagerConfiguration = LindexiAgentConfiguration.LoadDefault();
var configFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";
await agentApiManagerConfiguration.SaveToFileAsync(new FileInfo(configFile));

var miniMaxKeyFile = @"C:\lindexi\Work\Key\MiniMax.txt";
var miniMaxKey = await File.ReadAllTextAsync(miniMaxKeyFile);

var languageModelProvider = JsonConfigurationOpenAIProtocolLanguageModelProvider.FromConfiguration(
    new OpenAIProtocolLanguageModelConfiguration("https://api.minimaxi.com/v1", miniMaxKey)
    {
        ModelDefinitions =
        [
            new ModelDefinition()
            {
                ModelName = "MiniMax-M2.7"
            }
        ]
    });

agentApiEndpointManager.RegisterLanguageModelProvider(languageModelProvider);

IChatClient chatClient = await agentApiEndpointManager.PrimaryModel.GetChatClientAsync();

var skillFolder = @"F:\lindexi\Code\dotnet-skills\plugins\dotnet-ai\skills\";

#pragma warning disable MAAI001
var agentSkillsProvider = new AgentSkillsProvider(skillFolder, ScriptRunner);

Task<object?> ScriptRunner(AgentFileSkill skill, AgentFileSkillScript script, JsonElement? arguments,
    IServiceProvider? serviceProvider, CancellationToken cancellationToken)
{
    return Task.FromResult<object?>(null);
}

ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    AIContextProviders = [agentSkillsProvider]
});

await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "你好，我准备开发 MCP 服务器，请你给我一些建议")]);

Console.WriteLine("Hello, World!");


class F : AIContextProvider
{
}

class MiniMaxProtocolLanguageModelProvider
{
}