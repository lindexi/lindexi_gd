using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using MiniMaxSdk.Internal.Serialization;
using MiniMaxSdk.Music.Internal.Payloads;
using MiniMaxSdk.Music.Models;

namespace MiniMaxSdk.Music.Clients;

/// <summary>
/// MiniMax 音乐客户端，支持音乐生成、歌词生成与翻唱前处理。
/// </summary>
public sealed class MiniMaxMusicClient : IDisposable
{
    private static readonly Uri MusicGenerationEndpoint = new("https://api.minimaxi.com/v1/music_generation");
    private static readonly Uri LyricsGenerationEndpoint = new("https://api.minimaxi.com/v1/lyrics_generation");
    private static readonly Uri CoverPreprocessEndpoint = new("https://api.minimaxi.com/v1/music_cover_preprocess");

    private static MiniMaxJsonSerializerContext SerializerContext => MiniMaxJsonSerializerContext.Default;

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly string _apiKey;

    /// <summary>
    /// 初始化 <see cref="MiniMaxMusicClient"/> 实例。
    /// </summary>
    /// <param name="apiKey">用于验证账户信息的 MiniMax API Key。</param>
    /// <param name="httpClient">可选的 <see cref="HttpClient"/> 实例。</param>
    public MiniMaxMusicClient(string apiKey, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiKey));
        }

        _apiKey = apiKey.Trim();
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
    }

    /// <summary>
    /// 调用 MiniMax 音乐生成接口。
    /// </summary>
    /// <param name="request">音乐生成请求。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>音乐生成结果。</returns>
    public async Task<MiniMaxMusicGenerationResult> GenerateMusicAsync(MiniMaxMusicGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var response = await PostAsync(
            MusicGenerationEndpoint,
            MusicGenerationRequestPayload.FromRequest(request),
            SerializerContext.MusicGenerationRequestPayload,
            SerializerContext.MusicGenerationResponsePayload,
            "音乐生成",
            cancellationToken).ConfigureAwait(false);

        return new MiniMaxMusicGenerationResult(
            response.Data?.Status,
            response.Data?.Audio,
            ToBaseResponse(response.BaseResp));
    }

    /// <summary>
    /// 调用 MiniMax 歌词生成接口。
    /// </summary>
    /// <param name="request">歌词生成请求。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>歌词生成结果。</returns>
    public async Task<MiniMaxLyricsGenerationResult> GenerateLyricsAsync(MiniMaxLyricsGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var response = await PostAsync(
            LyricsGenerationEndpoint,
            LyricsGenerationRequestPayload.FromRequest(request),
            SerializerContext.LyricsGenerationRequestPayload,
            SerializerContext.LyricsGenerationResponsePayload,
            "歌词生成",
            cancellationToken).ConfigureAwait(false);

        return new MiniMaxLyricsGenerationResult(
            response.SongTitle,
            response.StyleTags,
            response.Lyrics,
            ToBaseResponse(response.BaseResp));
    }

    /// <summary>
    /// 调用 MiniMax 翻唱前处理接口。
    /// </summary>
    /// <param name="request">翻唱前处理请求。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>翻唱前处理结果。</returns>
    public async Task<MiniMaxCoverPreprocessResult> PreprocessCoverAsync(MiniMaxCoverPreprocessRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var response = await PostAsync(
            CoverPreprocessEndpoint,
            CoverPreprocessRequestPayload.FromRequest(request),
            SerializerContext.CoverPreprocessRequestPayload,
            SerializerContext.CoverPreprocessResponsePayload,
            "翻唱前处理",
            cancellationToken).ConfigureAwait(false);

        return new MiniMaxCoverPreprocessResult(
            response.CoverFeatureId,
            response.FormattedLyrics,
            response.StructureResult,
            response.AudioDuration,
            response.TraceId,
            ToBaseResponse(response.BaseResp));
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        Uri endpoint,
        TRequest payload,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        string scenarioName,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = JsonContent.Create(payload, requestTypeInfo);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"MiniMax {scenarioName}请求失败，HTTP {(int)response.StatusCode} {response.ReasonPhrase}。响应内容：{responseContent}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync(responseTypeInfo, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"MiniMax {scenarioName}响应为空。");

        ValidateBaseResponse(apiResponse, scenarioName);
        return apiResponse;
    }

    private static void ValidateBaseResponse<TResponse>(TResponse response, string scenarioName)
        where TResponse : class
    {
        var baseResponse = response switch
        {
            MusicGenerationResponsePayload musicResponse => musicResponse.BaseResp,
            LyricsGenerationResponsePayload lyricsResponse => lyricsResponse.BaseResp,
            CoverPreprocessResponsePayload coverResponse => coverResponse.BaseResp,
            _ => throw new InvalidOperationException($"未知的 MiniMax {scenarioName}响应类型：{typeof(TResponse).FullName}")
        };

        if (baseResponse?.StatusCode is int statusCode && statusCode != 0)
        {
            throw new InvalidOperationException($"MiniMax {scenarioName}失败，状态码：{statusCode}，错误信息：{baseResponse.StatusMessage}");
        }
    }

    private static MiniMaxBaseResponse ToBaseResponse(BaseResponsePayload? payload)
    {
        return payload is null
            ? new MiniMaxBaseResponse(0, null)
            : new MiniMaxBaseResponse(payload.StatusCode, payload.StatusMessage);
    }

    /// <summary>
    /// 释放当前客户端持有的资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
