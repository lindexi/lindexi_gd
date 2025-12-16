// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => { options.ServiceName = "Test"; });

builder.Services.AddHostedService<WindowsBackgroundService>();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.ClearProviders());

var host = builder.Build();

Console.WriteLine("本服务将自动从腾讯（ntp.tencent.com）、阿里（ntp.aliyun.com）等国内网络时间服务同步时间");
Console.WriteLine();

if (OperatingSystem.IsWindows())
{
    // 此方式会弹出 UAC 框
    //BoostHelper.AddStartup("WechallceahairBerebairballha", Environment.ProcessPath!);

    Process.Start("sc.exe",
    [
        "create",
        "UpdateTimeServer",
        $"binPath=", $"\"{Environment.ProcessPath!}\"",
        "start=", "auto",
        "displayname=", "自动更新时间服务"
    ]);

    Console.WriteLine("已创建 Windows 服务：UpdateTimeServer，启动类型为自动开机自启。如需删除该服务，请运行命令：sc delete UpdateTimeServer，或直接删除本可执行 exe 文件");
    Console.WriteLine();
}

Console.WriteLine($"开始自动更新时间");
Console.WriteLine();
await host.RunAsync();

Console.Read();

[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
static extern IntPtr CreateService(
    IntPtr hSCManager,
    string lpServiceName,
    string lpDisplayName,
    uint dwDesiredAccess,
    uint dwServiceType,
    uint dwStartType,
    uint dwErrorControl,
    string lpBinaryPathName,
    string lpLoadOrderGroup,
    string lpdwTagId,
    string lpDependencies,
    string lpServiceStartName,
    string lpPassword);


public class WindowsBackgroundService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentProcess = Process.GetCurrentProcess();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var networkTime = await NtpClient.GetChineseNetworkTime();
                if (networkTime != null)
                {
                    if (DateTimeOffset.Now - networkTime.Value > TimeSpan.FromSeconds(5))
                    {
                        Console.WriteLine($"正在将系统时间从 {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} 同步到网络时间 {networkTime.Value:yyyy-MM-dd HH:mm:ss} ......");
                        SetNtpTime(networkTime.Value);
                    }
                }

                EmptyWorkingSet(currentProcess.Handle);
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// 为系统设置输入的时间
    /// </summary>
    public static bool SetNtpTime(DateTimeOffset time)
    {
        try
        {
            var localTime = time.LocalDateTime;

            var result = SetLocalTime(new SYSTEMTIME
            {
                wYear = (ushort) localTime.Year,
                wMonth = (ushort) localTime.Month,
                wDayOfWeek = (ushort) localTime.DayOfWeek,
                wDay = (ushort) localTime.Day,
                wHour = (ushort) localTime.Hour,
                wMinute = (ushort) localTime.Minute,
                wSecond = (ushort) localTime.Second,
                wMilliseconds = (ushort) localTime.Millisecond
            });

            return result;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// <para>
    /// Sets the current local time and date.
    /// </para>
    /// <para>
    /// From: <see href="https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-setlocaltime"/>
    /// </para>
    /// </summary>
    /// <param name="lpSystemTime">
    /// A pointer to a <see cref="SYSTEMTIME"/> structure that contains the new local date and time.
    /// The <see cref="SYSTEMTIME.wDayOfWeek"/> member of the <see cref="SYSTEMTIME"/> structure is ignored.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is <see cref="TRUE"/>.
    /// If the function fails, the return value is <see cref="FALSE"/>.
    /// To get extended error information, call <see cref="GetLastError"/>.
    /// </returns>
    /// <remarks>.
    /// The calling process must have the <see cref="SE_SYSTEMTIME_NAME"/> privilege. This privilege is disabled by default.
    /// The <see cref="SetLocalTime"/> function enables the <see cref="SE_SYSTEMTIME_NAME"/> privilege
    /// before changing the local time and disables the privilege before returning.
    /// For more information, see Running with Special Privileges.
    /// The system uses UTC internally.
    /// Therefore, when you call <see cref="SetLocalTime"/>, the system uses the current time zone information
    /// to perform the conversion, including the daylight saving time setting.
    /// Note that the system uses the daylight saving time setting of the current time, not the new time you are setting.
    /// Therefore, to ensure the correct result, call <see cref="SetLocalTime"/> a second time,
    /// now that the first call has updated the daylight saving time setting.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetLocalTime", ExactSpelling = true,
        SetLastError = true)]
    public static extern bool SetLocalTime([In] in SYSTEMTIME lpSystemTime);

    [DllImport("psapi")]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);
}

/// <summary>
/// <para>
/// Specifies a date and time, using individual members for the month, day, year, weekday, hour, minute, second, and millisecond.
/// The time is either in coordinated universal time (UTC) or local time, depending on the function that is being called.
/// </para>
/// <para>
/// From: <see href="https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-systemtime"/>
/// </para>
/// </summary>
/// <remarks>
/// The <see cref="SYSTEMTIME"/> does not check to see if the date represented is a real and valid date.
/// When working with this API, you should ensure its validity, especially in leap reat scenarios.
/// See leap day readiness for more information.
/// It is not recommended that you add and subtract values from the <see cref="SYSTEMTIME"/> structure to obtain relative times.
/// Instead, you should:
/// Convert the <see cref="SYSTEMTIME"/> structure to a <see cref="FILETIME"/> structure.
/// Copy the resulting <see cref="FILETIME"/> structure to a <see cref="ULARGE_INTEGER"/> structure.
/// Use normal 64-bit arithmetic on the <see cref="ULARGE_INTEGER"/> value.
/// The system can periodically refresh the time by synchronizing with a time source.
/// Because the system time can be adjusted either forward or backward, do not compare system time readings to determine elapsed time.
/// Instead, use one of the methods described in Windows Time.
/// </remarks>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct SYSTEMTIME
{
    /// <summary>
    /// The year. The valid values for this member are 1601 through 30827.
    /// </summary>
    public WORD wYear;

    /// <summary>
    /// The month. This member can be one of the following values.
    /// 1 January
    /// 2 February
    /// 3 March
    /// 4 April
    /// 5 May
    /// 6 June
    /// 7 July
    /// 8 August
    /// 9 September
    /// 10 October
    /// 11 November
    /// 12 December
    /// </summary>
    public WORD wMonth;

    /// <summary>
    /// The day of the week. This member can be one of the following values.
    /// 0 Sunday
    /// 1 Monday
    /// 2 Tuesday
    /// 3 Wednesday
    /// 4 Thursday
    /// 5 Friday
    /// 6 Saturday
    /// </summary>
    public WORD wDayOfWeek;

    /// <summary>
    /// The day of the month. The valid values for this member are 1 through 31.
    /// </summary>
    public WORD wDay;

    /// <summary>
    /// The hour. The valid values for this member are 0 through 23.
    /// </summary>
    public WORD wHour;

    /// <summary>
    /// The minute. The valid values for this member are 0 through 59.
    /// </summary>
    public WORD wMinute;

    /// <summary>
    /// The second. The valid values for this member are 0 through 59.
    /// </summary>
    public WORD wSecond;

    /// <summary>
    /// The millisecond. The valid values for this member are 0 through 999.
    /// </summary>
    public WORD wMilliseconds;
}

/// <summary>
/// <para>
/// A <see cref="WORD"/> is a 16-bit unsigned integer (range: 0 through 65535 decimal). Because a WORD is unsigned,
/// its first bit (Most Significant Bit (MSB)) is not reserved for signing.
/// </para>
/// <para>
/// From: <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/f8573df3-a44a-4a50-b070-ac4c3aa78e3c"/>
/// </para>
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct WORD
{
    [FieldOffset(0)] private ushort _value;

    /// <inheritdoc/>
    public override string ToString() => _value.ToString();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    public static implicit operator ushort(WORD val) => val._value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    public static implicit operator WORD(ushort val) => new WORD { _value = val };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    public static explicit operator short(WORD val) => unchecked((short) val._value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    public static explicit operator WORD(short val) => new WORD { _value = unchecked((ushort) val) };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    public static implicit operator uint(WORD val) => val._value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    public static implicit operator int(WORD val) => val._value;
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