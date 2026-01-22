using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpWebClients.Configurations;

public class HttpWebClientConfiguration
{
    /// <summary>
    /// 配置禁止被多个 HttpWebClient 共用
    /// </summary>
    public bool IsUsed { get; internal set; }

    public HttpWebClient? OwnerHttpWebClient { get; internal set; }
}
