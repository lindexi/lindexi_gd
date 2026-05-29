namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示 MiniMax 接口返回的状态码及详情。
/// </summary>
/// <param name="StatusCode">状态码，<c>0</c> 表示请求成功。</param>
/// <param name="StatusMessage">具体错误详情。</param>
/// <remarks>
/// 更多错误码可查看 MiniMax 错误码查询列表。
/// </remarks>
public sealed record MiniMaxBaseResponse(int StatusCode, string? StatusMessage);
