using System.Net.Http;
using System.Net.Sockets;
using HttpWebClients.HostBackup;

namespace HttpWebClients.HttpProviders;

/// <summary>
/// 默认的 HttpClient 提供器，将自己创建和释放
/// </summary>
public class DefaultHttpClientProvider : IHttpClientProvider
{
    public DefaultHttpClientProvider() : this(null)
    {
    }

    internal DefaultHttpClientProvider(HostBackupManager? hostBackupManager)
    {
        if (hostBackupManager is not null)
        {
            var socketConnector = new SocketConnector(hostBackupManager);
            var socketsHttpHandler = CreateSocketsHttpHandler(socketConnector);
            _httpClient = new HttpClient(socketsHttpHandler);
        }
        else
        {
            _httpClient = new HttpClient();
        }
    }

    //private readonly SocketConnector _socketConnector;

    public HttpClient GetHttpClient() => _httpClient;

    private readonly HttpClient _httpClient;

    internal SocketsHttpHandler CreateSocketsHttpHandler(SocketConnector socketConnector)
    {
        var socketsHttpHandler = new SocketsHttpHandler()
        {
            EnableMultipleHttp2Connections = true,
            AllowAutoRedirect = true,

            ConnectCallback = async (context, token) =>
            {
                var socket = await socketConnector.ConnectSocketAsync(context, token);

                // 在 NetworkStream 里，设置 ownsSocket 参数为 true 将会在 NetworkStream 被释放的时候，自动释放 Socket 资源
                return new NetworkStream(socket, ownsSocket: true);
            },
        };

        return socketsHttpHandler;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}