using HttpWebClients.HostBackup;
using HttpWebClients.HttpProviders;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HttpWebClients.Configurations;

public class HttpWebClientConfiguration
{
    /// <summary>
    /// 配置禁止被多个 HttpWebClient 共用
    /// </summary>
    public bool IsUsed { get; internal set; }

    public HttpWebClient? OwnerHttpWebClient { get; internal set; }

    public required JsonSerializerContext MainJsonSerializerContext { get; init; }

    /// <summary>
    /// 提供 <see cref="HttpClient"/> 对象
    /// </summary>
    public required IHttpClientProvider HttpClientProvider { get; init; }

    internal HostBackupManager? HostBackupManager { get; set; }

    public HttpWebClient BuildClient()
    {
        return new HttpWebClient(this);
    }
}