using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

    /// <summary>
    /// 给 <paramref name="httpRequestMessage"/> 追加 Cookie 内容
    /// </summary>
    /// <param name="httpRequestMessage"></param>
    /// <param name="cookies"></param>
    public static void AppendCookies(this HttpRequestMessage httpRequestMessage, params Cookie[] cookies)
    {
        var cookieCollection = httpRequestMessage.GetCookies();
        cookieCollection ??= new CookieCollection();
        foreach (var cookie in cookies)
        {
            cookieCollection.Add(cookie);
        }

        httpRequestMessage.ReplaceCookie(cookieCollection);
    }

    /// <summary>
    /// 获取当前的 <paramref name="httpRequestMessage"/> 的 Cookie 内容
    /// </summary>
    /// <param name="httpRequestMessage"></param>
    /// <returns>如果没有赋值，则返回空</returns>
    public static CookieCollection? GetCookies(this HttpRequestMessage httpRequestMessage)
    {
        if (httpRequestMessage.Options.TryGetValue(CookieHttpRequestOptionsKey, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// 设置请求的 Cookie 内容，设置的内容将会替换掉原来的。清空请传入 null 的值
    /// </summary>
    /// <param name="httpRequestMessage"></param>
    /// <param name="cookieCollection"></param>
    public static void ReplaceCookie(this HttpRequestMessage httpRequestMessage,
        CookieCollection? cookieCollection)
    {
        httpRequestMessage.Headers.Remove(HttpKnownHeaderNames.Cookie);
        if (cookieCollection is null)
        {
            return;
        }

        if (cookieCollection.Count > 0)
        {
            var cookieContainer = new CookieContainer();

            var uri = httpRequestMessage.RequestUri ?? new Uri("http://foo")/*使用 http://foo 只是为了不炸掉而已*/;
            cookieContainer.Add(uri, cookieCollection);

            // 必须通过 GetCookieHeader 进行设置，如此设置的 Cookie 之间采用 `;` 分割。通过 Fiddler 抓包能收到正确的 Cookie 内容
            var cookieHeader = cookieContainer.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                httpRequestMessage.Headers.Add(HttpKnownHeaderNames.Cookie, cookieHeader);
            }
            else
            {
                // 可能确实拿到空字符串，空字符串加入到 `httpRequestMessage.Headers.Add` 将会抛出异常
            }
        }
        else
        {
            // 没 Cookie 内容就不要加头了，加了也是浪费
        }

        // 存放入缓存，如此将可以支持追加
        httpRequestMessage.Options.Set(CookieHttpRequestOptionsKey, cookieCollection);

        // 不能通过如下方式一个个设置，采用以下方式设置的 Cookie 之间采用 `,` 分割，这在 EN 后台是不认的。但是 ASP.NET Core 是认识的
        //foreach (Cookie? cookie in cookieCollection)
        //{
        //    if (cookie is not null)
        //    {
        //        httpRequestMessage.Headers.Add(HttpKnownHeaderNames.Cookie, cookie.ToString());
        //    }
        //}
    }

    private static HttpRequestOptionsKey<CookieCollection> CookieHttpRequestOptionsKey =>
        new HttpRequestOptionsKey<CookieCollection>(CookieKey);

    private const string CookieKey = "Custom CookieCollection Key";
}