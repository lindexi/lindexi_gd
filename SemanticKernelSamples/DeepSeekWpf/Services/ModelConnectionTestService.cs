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

        IChatClient? client = null;
        try
        {
            client = await _agentModelService.GetSelectedChatClientAsync().ConfigureAwait(false);
            await foreach (var update in client.GetStreamingResponseAsync(
                               [new ChatMessage(ChatRole.User, "Reply OK")],
                               cancellationToken: linkedSource.Token)
                               .ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(update.Text))
                {
                    return new ModelConnectionTestResult(true, null, "模型流式连接测试成功。");
                }
            }

            return new ModelConnectionTestResult(false, AiChatErrorCategory.EmptyResponse, "连接成功，但模型未返回内容。");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
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
        finally
        {
            if (client is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
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
