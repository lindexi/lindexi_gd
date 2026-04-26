using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;

namespace NetPortFinder;

public partial class MainWindow : Window
{
    private const int AfInet = 2;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void DetectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortTextBox.Text, out var port) || port is < 1 or > 65535)
        {
            MessageBox.Show(this, "请输入 1 到 65535 之间的有效端口号。", "端口无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            PortTextBox.Focus();
            PortTextBox.SelectAll();
            return;
        }

        DetectButton.IsEnabled = false;
        LogTextBox.Clear();
        AppendLog($"准备检测端口 {port}。");

        try
        {
            var progress = new Progress<string>(AppendLog);
            await Task.Run(() => DetectPort(port, progress));
        }
        finally
        {
            DetectButton.IsEnabled = true;
        }
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        LogTextBox.AppendText(line);
        LogTextBox.ScrollToEnd();
    }

    private static void DetectPort(int port, IProgress<string> progress)
    {
        var probes = new[]
        {
            new PortProbe("TCP", IPAddress.Loopback, SocketType.Stream, ProtocolType.Tcp, NeedsListen: true),
            new PortProbe("TCP", IPAddress.Any, SocketType.Stream, ProtocolType.Tcp, NeedsListen: true),
            new PortProbe("UDP", IPAddress.Loopback, SocketType.Dgram, ProtocolType.Udp, NeedsListen: false),
            new PortProbe("UDP", IPAddress.Any, SocketType.Dgram, ProtocolType.Udp, NeedsListen: false),
        };

        var conflicts = new List<ProbeResult>();

        progress.Report("开始执行监听检测。");

        foreach (var probe in probes)
        {
            progress.Report($"尝试监听 {probe.Protocol} {probe.Address}:{port}。");
            var result = TryListen(probe, port);

            if (result.IsOccupied)
            {
                conflicts.Add(result);
                progress.Report($"{probe.Protocol} {probe.Address}:{port} 监听失败，端口可能已被占用。{result.Message}");
            }
            else
            {
                progress.Report($"{probe.Protocol} {probe.Address}:{port} 可成功监听。{result.Message}");
            }
        }

        if (conflicts.Count == 0)
        {
            progress.Report("所有检测项均通过，当前端口未发现占用。");
            return;
        }

        progress.Report("检测到至少一个监听失败，开始检索可能占用端口的进程。");

        var owners = GetPortOwners(port)
            .OrderBy(owner => owner.Protocol)
            .ThenBy(owner => owner.Address.ToString())
            .ThenBy(owner => owner.ProcessId)
            .DistinctBy(owner => (owner.Protocol, owner.Address, owner.Port, owner.ProcessId))
            .ToList();

        if (owners.Count == 0)
        {
            progress.Report("未从系统监听表中找到对应进程，可能是端口状态刚发生变化，或当前权限不足以获取完整信息。");
            return;
        }

        foreach (var owner in owners)
        {
            progress.Report($"可能占用进程：{owner.Protocol} {owner.Address}:{owner.Port} -> PID {owner.ProcessId} ({owner.ProcessName})");
        }
    }

    private static ProbeResult TryListen(PortProbe probe, int port)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, probe.SocketType, probe.ProtocolType)
            {
                ExclusiveAddressUse = true
            };

            socket.Bind(new IPEndPoint(probe.Address, port));

            if (probe.NeedsListen)
            {
                socket.Listen(1);
            }

            return new ProbeResult(probe.Protocol, probe.Address, false, "监听成功。", null);
        }
        catch (SocketException ex)
        {
            return new ProbeResult(probe.Protocol, probe.Address, true, $"Socket 错误码: {(int)ex.SocketErrorCode} ({ex.SocketErrorCode})。", ex.SocketErrorCode);
        }
        catch (Exception ex)
        {
            return new ProbeResult(probe.Protocol, probe.Address, true, $"异常: {ex.Message}", null);
        }
    }

    private static IEnumerable<PortOwner> GetPortOwners(int port)
    {
        foreach (var owner in GetTcpOwners(port))
        {
            yield return owner;
        }

        foreach (var owner in GetUdpOwners(port))
        {
            yield return owner;
        }
    }

    private static IEnumerable<PortOwner> GetTcpOwners(int port)
    {
        var buffer = IntPtr.Zero;
        try
        {
            var size = 0;
            _ = GetExtendedTcpTable(IntPtr.Zero, ref size, true, AfInet, TcpTableClass.TcpTableOwnerPidListener, 0);

            buffer = Marshal.AllocHGlobal(size);
            var result = GetExtendedTcpTable(buffer, ref size, true, AfInet, TcpTableClass.TcpTableOwnerPidListener, 0);
            if (result != 0)
            {
                yield break;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowPointer = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPointer);
                if (GetPort(row.LocalPort) == port)
                {
                    yield return CreatePortOwner("TCP", row.LocalAddr, port, unchecked((int)row.OwningPid));
                }

                rowPointer = IntPtr.Add(rowPointer, rowSize);
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static IEnumerable<PortOwner> GetUdpOwners(int port)
    {
        var buffer = IntPtr.Zero;
        try
        {
            var size = 0;
            _ = GetExtendedUdpTable(IntPtr.Zero, ref size, true, AfInet, UdpTableClass.UdpTableOwnerPid, 0);

            buffer = Marshal.AllocHGlobal(size);
            var result = GetExtendedUdpTable(buffer, ref size, true, AfInet, UdpTableClass.UdpTableOwnerPid, 0);
            if (result != 0)
            {
                yield break;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowPointer = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<MibUdpRowOwnerPid>();

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibUdpRowOwnerPid>(rowPointer);
                if (GetPort(row.LocalPort) == port)
                {
                    yield return CreatePortOwner("UDP", row.LocalAddr, port, unchecked((int)row.OwningPid));
                }

                rowPointer = IntPtr.Add(rowPointer, rowSize);
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static PortOwner CreatePortOwner(string protocol, uint address, int port, int processId)
    {
        var bytes = BitConverter.GetBytes(address);
        var ipAddress = new IPAddress(bytes);
        return new PortOwner(protocol, ipAddress, port, processId, GetProcessName(processId));
    }

    private static int GetPort(uint rawPort)
    {
        var bytes = BitConverter.GetBytes(rawPort);
        return (bytes[0] << 8) + bytes[1];
    }

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return "未知进程";
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int size, bool order, int ipVersion, TcpTableClass tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(IntPtr udpTable, ref int size, bool order, int ipVersion, UdpTableClass tableClass, uint reserved);

    private sealed record PortProbe(string Protocol, IPAddress Address, SocketType SocketType, ProtocolType ProtocolType, bool NeedsListen);

    private sealed record ProbeResult(string Protocol, IPAddress Address, bool IsOccupied, string Message, SocketError? SocketErrorCode);

    private sealed record PortOwner(string Protocol, IPAddress Address, int Port, int ProcessId, string ProcessName);

    private enum TcpTableClass
    {
        TcpTableOwnerPidListener = 3,
    }

    private enum UdpTableClass
    {
        UdpTableOwnerPid = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibUdpRowOwnerPid
    {
        public uint LocalAddr;
        public uint LocalPort;
        public uint OwningPid;
    }
}