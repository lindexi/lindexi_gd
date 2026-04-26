using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;

namespace NetPortFinder;

public partial class MainWindow : Window
{
    private const int AfInet = 2;
    private const int SearchProgressInterval = 500;

    private static readonly PortProbe[] Probes =
    [
        new("TCP", IPAddress.Loopback, SocketType.Stream, ProtocolType.Tcp, NeedsListen: true),
        new("TCP", IPAddress.Any, SocketType.Stream, ProtocolType.Tcp, NeedsListen: true),
        new("UDP", IPAddress.Loopback, SocketType.Dgram, ProtocolType.Udp, NeedsListen: false),
        new("UDP", IPAddress.Any, SocketType.Dgram, ProtocolType.Udp, NeedsListen: false),
    ];

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void DetectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        await ExecuteBusyActionAsync($"准备检测端口 {port}。", progress => Task.Run(() => DetectPort(port, progress)));
    }

    private async void FindAvailablePortButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ExecuteBusyActionAsync("开始自动查找可用监听端口。", async progress =>
        {
            var result = await Task.Run(() => FindAvailablePort(progress));
            if (result.Port is not int port)
            {
                return;
            }

            PortTextBox.Text = port.ToString();
            PortTextBox.Focus();
            PortTextBox.SelectAll();
            AppendLog($"已将找到的可用端口 {port} 填入输入框。");
        });
    }

    private async Task ExecuteBusyActionAsync(string startMessage, Func<IProgress<string>, Task> action)
    {
        SetBusyState(true);
        LogTextBox.Clear();
        AppendLog(startMessage);

        try
        {
            var progress = new Progress<string>(AppendLog);
            await action(progress);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private bool TryGetPort(out int port)
    {
        if (int.TryParse(PortTextBox.Text, out port) && port is >= 1 and <= 65535)
        {
            return true;
        }

        MessageBox.Show(this, "请输入 1 到 65535 之间的有效端口号。", "端口无效", MessageBoxButton.OK, MessageBoxImage.Warning);
        PortTextBox.Focus();
        PortTextBox.SelectAll();
        port = 0;
        return false;
    }

    private void SetBusyState(bool isBusy)
    {
        DetectButton.IsEnabled = !isBusy;
        FindAvailablePortButton.IsEnabled = !isBusy;
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        LogTextBox.AppendText(line);
        LogTextBox.ScrollToEnd();
    }

    private static void DetectPort(int port, IProgress<string> progress)
    {
        progress.Report($"当前进程管理员权限：{(IsRunningAsAdministrator() ? "是" : "否")}。");
        var report = InspectPort(port, progress, includeProcessLookup: true);

        progress.Report("自动分析结果：");
        foreach (var item in report.Analysis)
        {
            progress.Report($"- {item}");
        }
    }

    private static PortSearchResult FindAvailablePort(IProgress<string> progress)
    {
        progress.Report($"当前进程管理员权限：{(IsRunningAsAdministrator() ? "是" : "否")}。");

        var tcpExcludedRanges = GetExcludedPortRanges("tcp");
        var udpExcludedRanges = GetExcludedPortRanges("udp");
        progress.Report($"已读取系统排除端口范围：TCP {tcpExcludedRanges.Count} 段，UDP {udpExcludedRanges.Count} 段。");

        var checkedCount = 0;
        foreach (var range in GetPreferredSearchRanges())
        {
            progress.Report($"开始扫描候选区间 {range.Start}-{range.End}。");

            for (var port = range.Start; port <= range.End; port++)
            {
                if (ContainsPort(tcpExcludedRanges, port) || ContainsPort(udpExcludedRanges, port))
                {
                    continue;
                }

                checkedCount++;
                if (checkedCount % SearchProgressInterval == 0)
                {
                    progress.Report($"已扫描 {checkedCount} 个候选端口，当前检查到 {port}。");
                }

                if (!CanListenOnAllProbes(port))
                {
                    continue;
                }

                progress.Report($"找到可用于 TCP/UDP 与 127.0.0.1/0.0.0.0 的可用端口：{port}。");
                return new PortSearchResult(port, []);
            }
        }

        var analysis = new List<string>
        {
            "在常用扫描区间内未找到同时满足四项监听条件的端口。",
            "如果几乎所有端口都报 AccessDenied，优先检查 Windows 排除端口范围、Hyper-V/HNS、HTTP.sys、VPN/代理或安全软件。"
        };

        foreach (var item in analysis)
        {
            progress.Report($"自动分析：{item}");
        }

        return new PortSearchResult(null, analysis);
    }

    private static PortInspectionReport InspectPort(int port, IProgress<string>? progress, bool includeProcessLookup)
    {
        var probeResults = new List<ProbeResult>();
        progress?.Report("开始执行监听检测。");

        foreach (var probe in Probes)
        {
            progress?.Report($"尝试监听 {probe.Protocol} {probe.Address}:{port}。");
            var result = TryListen(probe, port);
            probeResults.Add(result);

            if (result.IsOccupied)
            {
                progress?.Report($"{probe.Protocol} {probe.Address}:{port} 监听失败。{result.Message}");
            }
            else
            {
                progress?.Report($"{probe.Protocol} {probe.Address}:{port} 可成功监听。{result.Message}");
            }
        }

        var owners = new List<PortOwner>();
        if (probeResults.Any(result => result.IsOccupied) && includeProcessLookup)
        {
            progress?.Report("检测到至少一个监听失败，开始检索可能占用端口的进程。");
            owners = GetPortOwners(port)
                .OrderBy(owner => owner.Protocol)
                .ThenBy(owner => owner.Address.ToString())
                .ThenBy(owner => owner.ProcessId)
                .DistinctBy(owner => (owner.Protocol, owner.Address, owner.Port, owner.ProcessId))
                .ToList();

            if (owners.Count == 0)
            {
                progress?.Report("未从系统监听表中找到对应进程，失败原因可能并非普通用户态进程占用。");
            }
            else
            {
                foreach (var owner in owners)
                {
                    progress?.Report($"可能占用进程：{owner.Protocol} {owner.Address}:{owner.Port} -> PID {owner.ProcessId} ({owner.ProcessName})");
                }
            }
        }
        else if (probeResults.All(result => !result.IsOccupied))
        {
            progress?.Report("所有检测项均通过，当前端口未发现占用。");
        }

        var analysis = AnalyzePortIssues(port, probeResults, owners);
        return new PortInspectionReport(port, probeResults, owners, analysis);
    }

    private static IReadOnlyList<string> AnalyzePortIssues(int port, IReadOnlyList<ProbeResult> probeResults, IReadOnlyList<PortOwner> owners)
    {
        var analysis = new List<string>();

        if (probeResults.All(result => !result.IsOccupied))
        {
            analysis.Add("四项监听均成功，此端口可直接用于本机监听。");
            return analysis;
        }

        if (probeResults.Any(result => result.SocketErrorCode == SocketError.AddressAlreadyInUse))
        {
            analysis.Add("出现 AddressAlreadyInUse，说明该端口已经被现有监听直接占用。");
        }

        if (owners.Count > 0)
        {
            analysis.Add("系统监听表已返回可能占用该端口的进程，可结合 PID 继续确认来源。");
        }

        if (probeResults.Any(result => result.SocketErrorCode == SocketError.AccessDenied))
        {
            analysis.Add("出现 AccessDenied，说明至少一个绑定请求被系统拒绝，不一定是普通进程冲突。");

            var tcpExcludedRange = FindContainingRange(GetExcludedPortRanges("tcp"), port);
            if (tcpExcludedRange is not null)
            {
                analysis.Add($"该端口命中 TCP 排除端口范围 {tcpExcludedRange.Start}-{tcpExcludedRange.End}，系统可能直接拒绝监听。");
            }

            var udpExcludedRange = FindContainingRange(GetExcludedPortRanges("udp"), port);
            if (udpExcludedRange is not null)
            {
                analysis.Add($"该端口命中 UDP 排除端口范围 {udpExcludedRange.Start}-{udpExcludedRange.End}，系统可能直接拒绝监听。");
            }

            var loopbackDeniedProtocols = probeResults
                .Where(result => result.Address.Equals(IPAddress.Loopback) && result.SocketErrorCode == SocketError.AccessDenied)
                .Select(result => result.Protocol)
                .Distinct()
                .ToList();

            if (loopbackDeniedProtocols.Any(protocol => probeResults.Any(result => result.Protocol == protocol && result.Address.Equals(IPAddress.Any) && !result.IsOccupied)))
            {
                analysis.Add("同协议下 127.0.0.1 失败而 0.0.0.0 成功，更像是环回地址被代理、安全软件、端口转发组件或系统策略限制。");
            }

            if (owners.Count == 0)
            {
                analysis.Add("监听表未返回对应进程时，常见原因是 Windows 端口保留、HTTP.sys、Hyper-V/HNS、VPN/代理或安全软件。");
            }

            if (!IsRunningAsAdministrator())
            {
                analysis.Add("当前程序未以管理员身份运行，可尝试提升权限后再次验证，以排除本机安全策略影响。");
            }
        }

        if (analysis.Count == 0)
        {
            analysis.Add("检测失败，但未匹配到典型问题模式，请结合详细日志进一步确认。");
        }

        return analysis.Distinct().ToList();
    }

    private static bool CanListenOnAllProbes(int port)
    {
        foreach (var probe in Probes)
        {
            if (TryListen(probe, port).IsOccupied)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<PortRange> GetPreferredSearchRanges()
    {
        yield return new PortRange(10000, 60000);
        yield return new PortRange(1024, 9999);
        yield return new PortRange(60001, 65535);
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

    private static IReadOnlyList<PortRange> GetExcludedPortRanges(string protocol)
    {
        var output = RunProcessAndReadOutput("netsh", $"int ipv4 show excludedportrange protocol={protocol}");
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var ranges = new List<PortRange>();
        var matches = Regex.Matches(output, @"^\s*(\d+)\s+(\d+)\s*$", RegexOptions.Multiline);
        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var start) || !int.TryParse(match.Groups[2].Value, out var end))
            {
                continue;
            }

            ranges.Add(new PortRange(start, end));
        }

        return ranges;
    }

    private static string RunProcessAndReadOutput(string fileName, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);

            return string.Join(Environment.NewLine, new[] { standardOutput, standardError }.Where(text => !string.IsNullOrWhiteSpace(text)));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static PortRange? FindContainingRange(IReadOnlyList<PortRange> ranges, int port)
    {
        return ranges.FirstOrDefault(range => range.Start <= port && port <= range.End);
    }

    private static bool ContainsPort(IReadOnlyList<PortRange> ranges, int port)
    {
        return FindContainingRange(ranges, port) is not null;
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
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

    private sealed record PortInspectionReport(int Port, IReadOnlyList<ProbeResult> ProbeResults, IReadOnlyList<PortOwner> Owners, IReadOnlyList<string> Analysis);

    private sealed record PortSearchResult(int? Port, IReadOnlyList<string> Analysis);

    private sealed record PortRange(int Start, int End);

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
