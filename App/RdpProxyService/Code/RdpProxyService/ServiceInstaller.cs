using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace RdpProxyService;

internal static class ServiceInstaller
{
    internal const string ServiceName = "RdpProxyService";
    private const string DisplayName = "RDP TCP Proxy Service";
    private const string Description = "将 TCP 33890 端口转发到本机 RDP 3389 端口。";
    private const string FirewallRuleName = "RDP TCP Proxy (TCP 33890)";

    internal static async Task InstallAndStartAsync()
    {
        EnsureAdministrator();

        var executablePath = Environment.ProcessPath
                             ?? throw new InvalidOperationException("无法确定当前程序路径。");
        var serviceExists = await RunScAsync(["query", ServiceName], throwOnError: false).ConfigureAwait(false) == 0;

        if (!serviceExists)
        {
            await RunScAsync
            (
                [
                    "create", ServiceName,
                    "binPath=", $"\"{executablePath}\"",
                    "start=", "auto",
                    "DisplayName=", DisplayName
                ]
            ).ConfigureAwait(false);
            await RunScAsync(["description", ServiceName, Description]).ConfigureAwait(false);
            await RunScAsync
                (
                    ["failure", ServiceName, "reset=", "86400", "actions=", "restart/5000/restart/15000/restart/30000"]
                )
                .ConfigureAwait(false);
            await RunScAsync(["failureflag", ServiceName, "1"]).ConfigureAwait(false);
        }
        else
        {
            await RunScAsync
                (["config", ServiceName, "binPath=", $"\"{executablePath}\"", "start=", "auto"]).ConfigureAwait(false);
        }

        await EnsureFirewallRuleAsync(executablePath).ConfigureAwait(false);

        var startExitCode = await RunScAsync(["start", ServiceName], throwOnError: false).ConfigureAwait(false);
        if (startExitCode is not 0 and not 1056)
        {
            throw new InvalidOperationException($"服务启动失败，sc.exe 退出代码：{startExitCode}。");
        }

        Console.WriteLine("RDP 代理服务已安装并启动：外部端口 33890 -> 本机端口 3389。");
    }

    private static async Task EnsureFirewallRuleAsync(string executablePath)
    {
        var ruleExists = await RunNetshAsync
        (
            [
                "advfirewall", "firewall", "show", "rule", $"name={FirewallRuleName}"
            ], throwOnError: false
        ).ConfigureAwait(false) == 0;

        if (ruleExists)
        {
            await RunNetshAsync
            (
                [
                    "advfirewall", "firewall", "delete", "rule", $"name={FirewallRuleName}"
                ]
            ).ConfigureAwait(false);
        }

        await RunNetshAsync
        (
            [
                "advfirewall", "firewall", "add", "rule",
                $"name={FirewallRuleName}", "dir=in", "action=allow", "protocol=TCP", "localport=33890",
                $"program={executablePath}", "profile=any", "enable=yes"
            ]
        ).ConfigureAwait(false);
    }

    private static void EnsureAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            throw new UnauthorizedAccessException("必须以管理员身份运行此程序。");
        }
    }

    private static Task<int> RunScAsync(IReadOnlyList<string> arguments, bool throwOnError = true)
    {
        return RunProcessAsync(Path.Combine(Environment.SystemDirectory, "sc.exe"), arguments, throwOnError);
    }

    private static Task<int> RunNetshAsync(IReadOnlyList<string> arguments, bool throwOnError = true)
    {
        return RunProcessAsync(Path.Combine(Environment.SystemDirectory, "netsh.exe"), arguments, throwOnError);
    }

    private static async Task<int> RunProcessAsync(string fileName, IReadOnlyList<string> arguments, bool throwOnError)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 Windows 服务管理工具。");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);

        if (throwOnError && process.ExitCode != 0)
        {
            var output = string.Join
            (
                Environment.NewLine, await standardOutput.ConfigureAwait(false),
                await standardError.ConfigureAwait(false)
            ).Trim();
            throw new Win32Exception(process.ExitCode, $"系统配置操作失败：{output}");
        }

        return process.ExitCode;
    }
}