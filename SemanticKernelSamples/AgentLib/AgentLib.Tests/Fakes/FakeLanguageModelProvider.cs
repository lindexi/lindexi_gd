using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Tests.Fakes;

internal sealed class FakeLanguageModelProvider(params ILanguageModel[] supportedModels) : ILanguageModelProvider
{
    private readonly IReadOnlyList<ILanguageModel> _supportedModels = supportedModels;

    public IReadOnlyList<ILanguageModel> GetSupportedModels()
    {
        return _supportedModels;
    }
}
