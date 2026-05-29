using MiniMaxSdk.Images.Clients;

namespace MiniMaxSdk;

/// <summary>
/// MiniMax SDK 的统一客户端入口。
/// </summary>
public sealed class MiniMaxClient : IDisposable
{
    /// <summary>
    /// 初始化 <see cref="MiniMaxClient"/> 实例。
    /// </summary>
    /// <param name="apiKey">用于验证账户信息的 MiniMax API Key。</param>
    /// <param name="httpClient">可选的 <see cref="HttpClient"/> 实例。</param>
    public MiniMaxClient(string apiKey, HttpClient? httpClient = null)
    {
        ImageGeneration = new MiniMaxImageGenerationClient(apiKey, httpClient);
    }

    /// <summary>
    /// 图片生成客户端。
    /// </summary>
    public MiniMaxImageGenerationClient ImageGeneration { get; }

    /// <summary>
    /// 释放当前客户端持有的资源。
    /// </summary>
    public void Dispose()
    {
        ImageGeneration.Dispose();
    }
}
