using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using System.Text;

namespace AgentLib.ChatRoom;

/// <summary>
/// 当指定了首选模型（<see cref="ChatRoomRoleDefinition.ModelProviderId"/> 或
/// <see cref="ChatRoomRoleDefinition.ModelId"/>）但在已注册的提供商中找不到匹配模型时抛出。
/// </summary>
public sealed class PrimaryModelNotFoundException : InvalidOperationException
{
    /// <summary>
    /// 角色定义中指定的模型提供商 ID。可能为 <see langword="null"/>。
    /// </summary>
    public string? RequestedProviderId { get; }

    /// <summary>
    /// 角色定义中指定的模型 ID。可能为 <see langword="null"/>。
    /// </summary>
    public string? RequestedModelId { get; }

    /// <summary>
    /// 当前已注册的所有可用模型列表。
    /// </summary>
    public IReadOnlyList<ILanguageModel> AvailableModels { get; }

    /// <summary>
    /// 创建 <see cref="PrimaryModelNotFoundException"/>。
    /// </summary>
    /// <param name="requestedProviderId">角色定义中指定的模型提供商 ID。</param>
    /// <param name="requestedModelId">角色定义中指定的模型 ID。</param>
    /// <param name="availableModels">当前已注册的所有可用模型列表。</param>
    public PrimaryModelNotFoundException(
        string? requestedProviderId,
        string? requestedModelId,
        IReadOnlyList<ILanguageModel> availableModels)
        : base(BuildMessage(requestedProviderId, requestedModelId, availableModels))
    {
        RequestedProviderId = requestedProviderId;
        RequestedModelId = requestedModelId;
        AvailableModels = availableModels;
    }

    private static string BuildMessage(
        string? requestedProviderId,
        string? requestedModelId,
        IReadOnlyList<ILanguageModel> availableModels)
    {
        var sb = new StringBuilder();
        sb.Append("找不到首选模型。");

        if (!string.IsNullOrWhiteSpace(requestedProviderId) && !string.IsNullOrWhiteSpace(requestedModelId))
        {
            sb.Append($" 指定的提供商为 \"{requestedProviderId}\"，模型为 \"{requestedModelId}\"。");
        }
        else if (!string.IsNullOrWhiteSpace(requestedProviderId))
        {
            sb.Append($" 指定的提供商为 \"{requestedProviderId}\"。");
        }
        else if (!string.IsNullOrWhiteSpace(requestedModelId))
        {
            sb.Append($" 指定的模型为 \"{requestedModelId}\"。");
        }

        if (availableModels.Count > 0)
        {
            sb.AppendLine(" 当前可用的模型：");
            foreach (ILanguageModel model in availableModels)
            {
                sb.AppendLine($"- {model.ModelDefinition.Provider}/{model.ModelDefinition.ModelName}");
            }
        }
        else
        {
            sb.Append(" 当前没有任何已注册的可用模型。");
        }

        return sb.ToString().TrimEnd();
    }
}
