using DeepSeekWpf.Models;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Services;

public interface IAgentModelService
{
    string ConfigurationFilePath { get; }

    IReadOnlyList<AgentModelDescriptor> RegisteredModels { get; }

    AgentModelDescriptor? SelectedModel { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);

    void SelectModel(string modelSpecifier);

    Task<IChatClient> GetSelectedChatClientAsync();
}