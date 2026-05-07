using System.Net.Http.Headers;

namespace VolcEngineSdk.OpenSpeech;

public sealed class OpenSpeechAuthentication
{
    private OpenSpeechAuthentication(string resourceId, string? apiKey, string? appId, string? accessKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ResourceId = resourceId;
        ApiKey = apiKey;
        AppId = appId;
        AccessKey = accessKey;
    }

    public string ResourceId { get; }

    public string? ApiKey { get; }

    public string? AppId { get; }

    public string? AccessKey { get; }

    /// <summary>
    /// 新版控制台
    /// </summary>
    /// <param name="apiKey"></param>
    /// <param name="resourceId"></param>
    /// <returns></returns>
    public static OpenSpeechAuthentication CreateWithApiKey(string apiKey, string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return new OpenSpeechAuthentication(resourceId, apiKey, null, null);
    }

    /// <summary>
    /// 旧版控制台
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="accessKey"></param>
    /// <param name="resourceId"></param>
    /// <returns></returns>
    public static OpenSpeechAuthentication CreateWithLegacyCredentials(string appId, string accessKey, string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKey);
        return new OpenSpeechAuthentication(resourceId, null, appId, accessKey);
    }

    internal void Apply(HttpRequestHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        headers.Add("X-Api-Resource-Id", ResourceId);

        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            headers.Add("X-Api-Key", ApiKey);
            return;
        }

        headers.Add("X-Api-App-Id", AppId);
        headers.Add("X-Api-Access-Key", AccessKey);
    }
}
