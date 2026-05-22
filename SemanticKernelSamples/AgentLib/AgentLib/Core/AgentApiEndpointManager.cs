using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.DeepSeek;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;

namespace AgentLib.Core;

public class AgentApiEndpointManager
{
    public void RegisterLanguageModelProvider(ILanguageModelProvider languageModelProvider)
    {
        var languageModels = languageModelProvider.GetSupportedModels();
        SupportedModels.AddRange(languageModels);
        // 有更新内容了，需要重新评估自动选择首选模型
        _autoSetPrimaryLanguageModel = null;
    }

    public IReadOnlyList<ILanguageModel> GetSupportedModels() => SupportedModels;

    private List<ILanguageModel> SupportedModels { get; } = [];

    public ILanguageModel PrimaryModel
    {
        get
        {
            if (_userSetPrimaryLanguageModel != null)
            {
                return _userSetPrimaryLanguageModel;
            }

            if (_autoSetPrimaryLanguageModel == null)
            {
                var supportedModels = GetSupportedModels();
                if (supportedModels.Count == 0)
                {
                    throw new InvalidOperationException($"尚未调用 {nameof(RegisterLanguageModelProvider)} 完成任何注册，无法获取到模型列表");
                }
                else if (supportedModels.Count == 1)
                {
                    return supportedModels[0];
                }

                // 全模态优先 Omni
                _autoSetPrimaryLanguageModel = supportedModels.ToList().OrderDescending(new LanguageModelCapabilityComparer()).First();
            }

            return _autoSetPrimaryLanguageModel;
        }
        set
        {
            var supportedModels = GetSupportedModels();
            if (!supportedModels.Contains(field))
            {
                throw new ArgumentException($"只能设置 {nameof(GetSupportedModels)} 支持的模型");
            }

            _userSetPrimaryLanguageModel = value;
            _autoSetPrimaryLanguageModel = null; // 用户设置后，自动选择的模型不再生效，直到用户取消设置
        }
    }

    /// <summary>
    /// 用户设置的首选模型
    /// </summary>
    private ILanguageModel? _userSetPrimaryLanguageModel;
    /// <summary>
    /// 用户没有设置的前提下，自动决定的首选模型
    /// </summary>
    private ILanguageModel? _autoSetPrimaryLanguageModel;

    public IApiEndpointProvider? ApiEndpointProvider { get; set; }

    /// <summary>
    /// 根据当前配置创建聊天客户端。
    /// </summary>
    public IChatClient CreateChatClient()
    {
        var apiEndpoint = ApiEndpointProvider?.GetApiEndpoint();
        if (string.IsNullOrWhiteSpace(apiEndpoint?.EndPoint))
        {
            throw new InvalidOperationException("必须先设置才能创建");
        }

        return CreateChatClient(apiEndpoint.Value);
    }

    /// <summary>
    /// 根据 API 终结点配置创建聊天客户端。
    /// </summary>
    public static IChatClient CreateChatClient(ApiEndpoint apiEndpoint)
    {
        if (string.IsNullOrWhiteSpace(apiEndpoint.EndPoint))
        {
            throw new ArgumentException("API 终结点不能为空。", nameof(apiEndpoint));
        }

        if (string.IsNullOrWhiteSpace(apiEndpoint.Key))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiEndpoint));
        }

        if (string.IsNullOrWhiteSpace(apiEndpoint.ModelId))
        {
            throw new ArgumentException("模型名称不能为空。", nameof(apiEndpoint));
        }

        if (!Uri.TryCreate(apiEndpoint.EndPoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("API 终结点必须是绝对地址。", nameof(apiEndpoint));
        }

        if (IsDeepSeekEndpoint(endpointUri))
        {
            return new DeepSeekChatClient(apiEndpoint.Key, apiEndpoint.ModelId, apiEndpoint.EndPoint);
        }

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiEndpoint.Key), new OpenAIClientOptions()
        {
            Endpoint = endpointUri
        });

        ChatClient chatClient = openAiClient.GetChatClient(apiEndpoint.ModelId);
        return chatClient.AsIChatClient();
    }

    private static bool IsDeepSeekEndpoint(Uri endpointUri)
    {
        return endpointUri.Host.Equals("api.deepseek.com", StringComparison.OrdinalIgnoreCase)
               || endpointUri.Host.Contains(".deepseek.com", StringComparison.OrdinalIgnoreCase);
    }
}