using HttpWebClients.Configurations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpWebClients;

/// <summary>
/// 提供与后台 HTTP 请求的能力客户端
/// </summary>
/// 此类是本程序集入口
/// 参考项目：
/// - https://github.com/dotnetcore/WebApiClient
/// - https://github.com/reactiveui/refit
public class HttpWebClient : IDisposable
{
    internal HttpWebClient(HttpWebClientConfiguration configuration)
    {
        if (configuration.IsUsed)
        {
            throw new InvalidOperationException();
        }

        configuration.IsUsed = true;
        configuration.OwnerHttpWebClient = this;
    }

    public void Dispose()
    {
    }
}
