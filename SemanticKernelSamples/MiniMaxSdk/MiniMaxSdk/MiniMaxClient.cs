namespace MiniMaxSdk;

public sealed class MiniMaxClient : IDisposable
{
    public MiniMaxClient(string apiKey, HttpClient? httpClient = null)
    {
        ImageGeneration = new MiniMaxImageGenerationClient(apiKey, httpClient);
    }

    /// <summary>
    /// 图片生成客户端。
    /// </summary>
    public MiniMaxImageGenerationClient ImageGeneration { get; }

    public void Dispose()
    {
        ImageGeneration.Dispose();
    }
}
