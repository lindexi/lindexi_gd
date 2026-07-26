using System.Net.Http;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Services;

public sealed class ModelConnectionTestService : IModelConnectionTestService
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(15);
    private readonly IAgentModelService _agentModelService;

    public ModelConnectionTestService(IAgentModelService agentModelService)
    {
        _agentModelService = agentModelService;
    }

    public async Task<ModelConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        using var timeoutSource = new CancellationTokenSource(TestTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            var client = await _agentModelService.GetSelectedChatClientAsync().ConfigureAwait(false);
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "Reply OK")],
                cancellationToken: linkedSource.Token).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(response.Text)
                ? new ModelConnectionTestResult(false, AiChatErrorCategory.EmptyResponse, "连接成功，但模型未返回内容。")
                : new ModelConnectionTestResult(true, null, "模型连接测试成功。");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ModelConnectionTestResult(false, AiChatErrorCategory.Timeout, "模型连接测试超时。");
        }
        catch (HttpRequestException exception)
        {
            var category = exception.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => AiChatErrorCategory.Authentication,
                System.Net.HttpStatusCode.TooManyRequests => AiChatErrorCategory.RateLimit,
                _ when exception.StatusCode is not null && (int)exception.StatusCode >= 500 => AiChatErrorCategory.Server,
                _ => AiChatErrorCategory.Network,
            };
            return new ModelConnectionTestResult(false, category, GetFailureMessage(category));
        }
        catch (InvalidOperationException)
        {
            return new ModelConnectionTestResult(false, AiChatErrorCategory.Configuration, "模型配置不可用。");
        }
        catch (Exception)
        {
            return new ModelConnectionTestResult(false, AiChatErrorCategory.Unknown, "模型连接测试失败，请查看日志。");
        }
    }

    private static string GetFailureMessage(AiChatErrorCategory category) => category switch
    {
        AiChatErrorCategory.Authentication => "认证失败，请检查模型凭据。",
        AiChatErrorCategory.RateLimit => "请求受到限流，请稍后重试。",
        AiChatErrorCategory.Server => "模型服务暂时不可用。",
        _ => "网络连接失败，请检查网络和服务地址。",
    };
}
