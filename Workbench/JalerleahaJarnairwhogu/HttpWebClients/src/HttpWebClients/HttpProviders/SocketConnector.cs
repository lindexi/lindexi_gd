using HttpWebClients.HostBackup;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpWebClients.HttpProviders;

class SocketConnector
{
    public SocketConnector(HostBackupManager hostBackupManager)
    {
        _hostBackupManager = hostBackupManager;
    }

    private readonly HostBackupManager _hostBackupManager;

    /// <summary>
    /// 系统是否支持 IP v6 访问网络
    /// </summary>
    /// 可通过 AppContext.SetSwitch("System.Net.DisableIPv6", true); 禁用
    private static bool OSSupportsIPv6 => _isOSSupportsIPv6 ??= Socket.OSSupportsIPv6;
    private static bool? _isOSSupportsIPv6;

    public async Task<Socket> ConnectSocketAsync(SocketsHttpConnectionContext context,
        CancellationToken token)
    {
        // 实验，域名的 IP 顺序是不保证的
        // 我在 godaddy 配置我的 foo.lindexi.com 域名包含 5 个 IP 地址
        // 在经过 ipconfig /flushdns 之后，拿到的 Dns.GetHostAddressesAsync 返回的值是随机的

        IPAddress[] ipAddresses;

        try
        {
            ipAddresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token);
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.HostNotFound)
            {
                // 无法从 DNS 解析到域名，需要记录一下
            }

            throw;
        }

        SocketError lastError = SocketError.NoData;
        foreach (IPAddress ipAddress in ipAddresses)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (!OSSupportsIPv6)
                {
                    continue;
                }
            }
        }

        var result = await TryConnectSocketAsync(context.DnsEndPoint, token);

        if (result.IsSuccess)
        {
            return result.Socket;
        }

        Debug.Assert(result.Exception is not null);

        if (result.Exception is not null)
        {
            if (result.Exception is SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.HostNotFound)
                {
                    // Host 都没找到了，那就不需要继续啦

                }
            }

            ExceptionDispatchInfo.Throw(result.Exception);
        }

        throw new Exception(null, result.Exception);
    }

    private async Task<ConnectSocketResult> TryConnectSocketAsync(EndPoint endPoint, CancellationToken token)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.NoDelay = true;

            await socket.ConnectAsync(endPoint, token)
                // 配置异步等待后不需要回到原来的线程
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            socket.Dispose();

            return new ConnectSocketResult()
            {
                Exception = e
            };
        }

        return ConnectSocketResult.Success(socket);
    }
}

readonly record struct ConnectSocketResult()
{
    [MemberNotNullWhen(true, nameof(Socket))]
    public bool IsSuccess => Socket != null;

    public Socket? Socket { get; init; }

    public Exception? Exception { get; init; }

    public static ConnectSocketResult Success(Socket socket)
        => new ConnectSocketResult();
}