using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentLib.Core.AgentApiManagers;

public class LindexiAgentConfiguration
{
    public static AgentApiManagerConfiguration LoadDefault()
    {
        var doubaoKeyFile = @"c:\lindexi\Work\Doubao.txt";
        var deepseekKeyFile = @"c:\lindexi\Work\deepseek.txt";
        var miniMaxKeyFile = @"C:\lindexi\Work\Key\MiniMax.txt";

        var doubaoKey = File.ReadAllText(doubaoKeyFile);
        var deepseekKey = File.ReadAllText(deepseekKeyFile);
        var miniMaxKey = File.ReadAllText(miniMaxKeyFile);

        var agentApiManagerConfiguration = new AgentApiManagerConfiguration()
        {
            PrimaryModel = "deepseek-v4-pro",
            OpenAIConfigurationList = new List<OpenAIProtocolLanguageModelConfiguration>()
            {
                new OpenAIProtocolLanguageModelConfiguration("https://api.deepseek.com", deepseekKey)
                {
                    ModelDefinitions = new List<ModelDefinition>()
                    {
                        new ModelDefinition
                        {
                            Provider = "deepseek",
                            ModelName = "deepseek-v4-pro",
                            Capabilities = new LlmModelCapabilities
                            {
                                Reasoning = true,
                                ToolCall = true,
                                Temperature = true,
                                Input = new LlmModalityCapability { Text = true, Image = false },
                            },
                            ContextWindowSize = 1_000_000,
                            MaxOutputTokens = 393_216,
                        },
                        new ModelDefinition
                        {
                            Provider = "deepseek",
                            ModelName = "deepseek-v4-flash",
                            Capabilities = new LlmModelCapabilities
                            {
                                Reasoning = true,
                                ToolCall = true,
                                Temperature = true,
                                Input = new LlmModalityCapability { Text = true, Image = false },
                                IsFlash = true,
                            },
                            ContextWindowSize = 1_000_000,
                            MaxOutputTokens = 393_216,
                        },
                    }
                },

                new OpenAIProtocolLanguageModelConfiguration("https://ark.cn-beijing.volces.com/api/v3", doubaoKey)
                {
                    ModelDefinitions = new List<ModelDefinition>()
                    {
                        new ModelDefinition()
                        {
                            ModelName = "Doubao-Seed-2.0-pro",
                            ModelId = "ep-20260306101224-c8mtg",
                            Provider = "Doubao",
                            Capabilities = new LlmModelCapabilities()
                            {
                                Reasoning = true,
                                ToolCall = true,
                                Temperature = false,
                                Attachment = false,
                                Input = new LlmModalityCapability()
                                {
                                    Text = true,
                                    //Audio = true,
                                    Image = true,
                                    Video = true,
                                },
                                ResponseFormat = false,
                            }
                        },
                        new ModelDefinition()
                        {
                            ModelName = "Doubao-Seed-2.0-lite",
                            ModelId = "ep-20260519114607-snpl5",
                            Provider = "Doubao",
                            Capabilities = new LlmModelCapabilities()
                            {
                                Reasoning = true,
                                ToolCall = true,
                                Temperature = false,
                                Attachment = false,
                                Input = new LlmModalityCapability()
                                {
                                    Text = true,
                                    Audio = true,
                                    Image = true,
                                    Video = true,
                                },
                                ResponseFormat = false,
                                IsFlash = true,
                            }
                        },
                    }
                },

                new OpenAIProtocolLanguageModelConfiguration("https://api.minimaxi.com/v1", miniMaxKey)
                {
                    ModelDefinitions =
                    [
                        new ModelDefinition()
                        {
                            ModelName = "MiniMax-M2.7"
                        }
                    ]
                }
            }
        };
        return agentApiManagerConfiguration;
    }
}