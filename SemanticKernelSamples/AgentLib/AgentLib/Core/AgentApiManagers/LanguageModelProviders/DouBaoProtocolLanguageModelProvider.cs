using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public class DouBaoProtocolLanguageModelProvider(string endPoint, string key,
    Dictionary<string/*ModelName*/, string/*ModelId*/> modelNameToIdDictionary
)
    : OpenAIProtocolLanguageModelProviderBase(endPoint, key)
{
    public override IModelNameToIdMap? ModelNameToIdMap { get; init; } = new DictionaryModelNameToIdMap
        { ModelNameToIdDictionary = modelNameToIdDictionary };

    protected override IReadOnlyList<ModelDefinition> GetModelDefinitions()
    {
        return
        [
            new ModelDefinition()
            {
                ModelName = "Doubao-Seed-2.0-pro",
                //ModelId = "ep-20260306101224-c8mtg",
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
                //ModelId = "ep-20260519114607-snpl5",
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
        ];
    }
}