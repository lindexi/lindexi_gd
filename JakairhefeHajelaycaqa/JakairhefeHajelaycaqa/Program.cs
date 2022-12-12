using System.Buffers;
using System.Net.Sockets;
using System.Net;

namespace JakairhefeHajelaycaqa;

internal class Program
{
    static async Task Main(string[] args)
    {
        var dateTimeOffset = await NtpClient.GetChineseNetworkTime();

        if (dateTimeOffset is null)
        {
            Console.WriteLine("获取不到时间");
        }
        else
        {
            Console.WriteLine(dateTimeOffset);
            Console.WriteLine(dateTimeOffset.Value.LocalDateTime);

            // 本机时区时间和北京时间的差别是，本机系统时区可能被设置为非北京时间，当本机系统时区设置为北京时间，则本机时区时间和北京时间相同
            DateTime beijingTime = dateTimeOffset.Value.UtcDateTime.AddHours(8);
            Console.WriteLine(beijingTime);
        }
    }
}

// https://github.com/michaelschwarz/NETMF-Toolkit/blob/095b01679945c3f518dd52082eca78bbaff9811f/NTP/NtpClient.cs
public static class NtpClient
{
    /// <summary>
    /// 国内的授时服务提供的网络时间。默认返回北京时区的时间。如需转换为本机时区时间，请使用 <code> var dateTimeOffset = NtpClient.GetChineseNetworkTime();var 本机时区时间 = dateTimeOffset.LocalDateTime;</code> 转换。本机时区时间和北京时间的差别是，本机系统时区可能被设置为非北京时间，当本机系统时区设置为北京时间，则本机时区时间和北京时间相同
    /// </summary>
    /// <remarks>实现方法是去询问腾讯和阿里的授时服务器</remarks>
    /// <returns>返回空表示没有能够获取到任何的时间，预计是网络错误了。返回北京时区的时间</returns>
    /// 本来想着异常对外抛出的，但是似乎抛出异常也没啥用
    public static async ValueTask<DateTimeOffset?> GetChineseNetworkTime()
    {
        // 感谢 [国内外常用公共NTP网络时间同步服务器地址_味辛的博客-CSDN博客_ntp服务器](https://blog.csdn.net/weixin_42588262/article/details/82501488 )
        var dateTimeOffset = await GetChineseNetworkTimeCore("ntp.tencent.com"); // 腾讯
        dateTimeOffset ??= await GetChineseNetworkTimeCore("ntp.aliyun.com"); // 阿里
        dateTimeOffset ??= await GetChineseNetworkTimeCore("cn.pool.ntp.org"); // 国家服务器
        dateTimeOffset ??= await GetChineseNetworkTimeCore("cn.ntp.org.cn"); // 中国授时
        dateTimeOffset ??= await GetChineseNetworkTimeCore("time.windows.com"); // time.windows.com 微软Windows自带

        if (dateTimeOffset is not null)
        {
            return dateTimeOffset.Value.ToOffset(TimeSpan.FromHours(8));
        }
        else
        {
            return null;
        }

        static async ValueTask<DateTimeOffset?> GetChineseNetworkTimeCore(string ntpServer)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ntpServer);
                IPAddress[] addressList = hostEntry.AddressList;

                if (addressList.Length == 0)
                {
                    // 被投毒了？那就换其他一个吧
                    return null;
                }

                foreach (var address in addressList)
                {
                    try
                    {
                        var ipEndPoint = new IPEndPoint(address, 123);
                        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));

                        return await GetNetworkUtcTime(ipEndPoint, cancellationTokenSource.Token);
                    }
                    catch
                    {
                        // 失败就继续换下一个
                    }

                    if (!cancellationTokenSource.TryReset())
                    {
                        cancellationTokenSource.Dispose();
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                }
            }
            catch
            {
                // 失败就失败
                // 本来想着异常对外抛出的，但是似乎抛出异常也没啥用
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the current DateTime from time-a.nist.gov.
    /// </summary>
    /// <returns>A DateTime containing the current time.</returns>
    public static ValueTask<DateTimeOffset> GetNetworkUtcTime()
    {
        return GetNetworkUtcTime("time-a.nist.gov");
    }

    /// <summary>
    /// Gets the current DateTime from <paramref name="ntpServer"/>.
    /// </summary>
    /// <param name="ntpServer">The hostname of the NTP server.</param>
    /// <returns>A DateTime containing the current time.</returns>
    public static async ValueTask<DateTimeOffset> GetNetworkUtcTime(string ntpServer)
    {
        var hostEntry = await Dns.GetHostEntryAsync(ntpServer);
        IPAddress[] address = hostEntry.AddressList;

        if (address == null || address.Length == 0)
        {
            throw new ArgumentException($"Could not resolve ip address from '{ntpServer}'.", "ntpServer");
        }

        var ipEndPoint = new IPEndPoint(address[0], 123);

        return await GetNetworkUtcTime(ipEndPoint);
    }

    /// <summary>
    /// Gets the current DateTime form <paramref name="endPoint"/> IPEndPoint.
    /// </summary>
    /// <param name="endPoint">The IPEndPoint to connect to.</param>
    /// <param name="token"></param>
    /// <returns>A DateTime containing the current time.</returns>
    public static async ValueTask<DateTimeOffset> GetNetworkUtcTime(IPEndPoint endPoint,
        CancellationToken token = default)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        await socket.ConnectAsync(endPoint, token);

        const int length = 48;

        // 实现方法请参阅 RFC 2030 的内容
        var ntpData = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            // 初始化数据
            ntpData[0] = 0x1B;
            for (int i = 1; i < length; i++)
            {
                ntpData[i] = 0;
            }

            await socket.SendAsync(ntpData.AsMemory(0, length), token);
            await socket.ReceiveAsync(ntpData.AsMemory(0, length), token);

            byte offsetTransmitTime = 40;
            ulong intPart = 0;
            ulong fractPart = 0;

            for (int i = 0; i <= 3; i++)
            {
                intPart = 256 * intPart + ntpData[offsetTransmitTime + i];
            }

            for (int i = 4; i <= 7; i++)
            {
                fractPart = 256 * fractPart + ntpData[offsetTransmitTime + i];
            }

            ulong milliseconds = (intPart * 1000 + (fractPart * 1000) / 0x100000000L);

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            var dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);

            return dateTimeOffset;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(ntpData);
        }
    }
}