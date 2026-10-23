using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.ChatRoom;

internal static class ChatRoomRoleModelBinder
{
    internal static void RegisterProvidersAndSelectPrimary(
        ChatRoomRole role,
        IReadOnlyDictionary<string, ILanguageModelProvider> languageModelProviders,
        string? defaultPrimaryModelId)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(languageModelProviders);

        if (role.Definition.IsHuman)
        {
            return;
        }

        foreach (ILanguageModelProvider provider in languageModelProviders.Values)
        {
            role.RegisterLanguageModelProvider(provider);
        }

        SelectPrimary(role, defaultPrimaryModelId);
    }

    internal static void SelectPrimary(ChatRoomRole role, string? defaultPrimaryModelId)
    {
        ArgumentNullException.ThrowIfNull(role);
        string? providerId = role.Definition.ModelProviderId;
        string? modelId = role.Definition.ModelId;
        if (string.IsNullOrWhiteSpace(providerId) && string.IsNullOrWhiteSpace(modelId))
        {
            modelId = defaultPrimaryModelId;
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return;
            }
        }

        IReadOnlyList<ILanguageModel> availableModels = role.EndpointManager.GetSupportedModels();
        ILanguageModel? matched = !string.IsNullOrWhiteSpace(modelId)
            ? role.EndpointManager.ResolveModel(modelId)
            : availableModels.FirstOrDefault(model => model.ModelDefinition.Provider == providerId);
        if (matched is null
            && !string.IsNullOrWhiteSpace(modelId)
            && !string.IsNullOrWhiteSpace(providerId))
        {
            matched = role.EndpointManager.GetModel(modelId, providerId);
        }
        if (matched is null)
        {
            throw new PrimaryModelNotFoundException(providerId, modelId, availableModels);
        }

        role.EndpointManager.PrimaryModel = matched;
    }
}
