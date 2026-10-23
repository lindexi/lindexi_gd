namespace AgentLib.Core.AgentApiManagers;

/// <summary>
/// 提供当前用于模型网络请求的 HTTP 客户端。
/// </summary>
public interface IHttpClientProvider
{
    /// <summary>
    /// 获取或设置当前 HTTP 客户端；为 <see langword="null"/> 时使用 SDK 默认客户端。
    /// </summary>
    HttpClient? HttpClient { get; set; }
}
