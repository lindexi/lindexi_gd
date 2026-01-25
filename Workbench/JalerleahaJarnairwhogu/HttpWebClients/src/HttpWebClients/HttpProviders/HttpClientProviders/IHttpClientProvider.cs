using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpWebClients.HttpProviders;

/// <summary>
/// 提供 <see cref="HttpClient"/> 对象
/// </summary>
public interface IHttpClientProvider : IDisposable
{
    /// <summary>
    /// 获取 <see cref="HttpClient"/> 对象
    /// </summary>
    /// <returns></returns>
    HttpClient GetHttpClient();
}