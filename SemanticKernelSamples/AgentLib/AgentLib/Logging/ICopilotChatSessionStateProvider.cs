using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Logging;

public interface ICopilotChatSessionStateProvider
{
    /// <summary>
    /// 异步获取当前会话对应的序列化 AgentSession 状态。
    /// </summary>
    Task<JsonElement?> GetSerializedSessionStateAsync(CancellationToken cancellationToken = default);
}