using System;
using System.Net;
using System.Net.Http;

namespace HttpWebClients.Utils;

internal static class HttpResponseMessageExtension
{
    /// <summary>
    /// 获取 <see cref="CookieCollection"/> 设置
    /// </summary>
    /// <param name="httpResponseMessage"></param>
    /// <returns></returns>
    public static CookieCollection GetCookie(this HttpResponseMessage httpResponseMessage)
    {
        Uri? requestUri = httpResponseMessage.RequestMessage?.RequestUri;
        if (requestUri is null)
        {
            return new CookieCollection();
        }

        var cookieContainer = new CookieContainer();
        if (httpResponseMessage.Headers.TryGetValues(HttpKnownHeaderNames.SetCookie, out var cookieValueList))
        {
            foreach (var value in cookieValueList)
            {
                cookieContainer.SetCookies(requestUri, value);
            }
        }

        return cookieContainer.GetCookies(requestUri);
    }
}