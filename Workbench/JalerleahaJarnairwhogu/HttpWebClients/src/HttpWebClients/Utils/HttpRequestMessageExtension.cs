using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpWebClients.Utils;

internal static class HttpRequestMessageExtension
{
    /// <summary>
    /// 拷贝重新创建 <see cref="HttpRequestMessage"/> 对象。由于 HttpRequestMessage 包含很多状态，在发送过一次之后，不能再次发送，否则将会因为状态问题导致上传的内容变更。例如 Content 作为 Stream 的存在，多次读取的内容将会不一致。因此重新拷贝一次，用来在域名备份等功能，可以重试
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    internal static async ValueTask<HttpRequestMessage> CloneAsync(this HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = await request.Content.CloneAsync().ConfigureAwait(false),
            Version = request.Version
        };

        foreach (var httpRequestOption in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(httpRequestOption.Key),httpRequestOption.Value);
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    internal static async ValueTask<HttpContent?> CloneAsync(this HttpContent? content)
    {
        if (content == null) return null;

        // 这里的 Content 如果是 ByteArrayContent 类型，那将返回 MemoryStream 类型，在 Memory 类型里面的实际存放的二进制数组将和 ByteArrayContent 的是相同的，意味着没有进行任何的二进制拷贝，性能特别好
        // 如果是 MemoryStream 类型，将返回 ReadonlyMemoryStream 类型，在 ReadonlyMemoryStream 里面重新包装 MemoryStream 内容，没有进行任何的二进制拷贝，性能特别好
        var stream = await content.ReadAsStreamAsync();

        var clone = new StreamContent(stream);
        foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
        {
            clone.Headers.Add(header.Key, header.Value);
        }
        return clone;
    }
}
