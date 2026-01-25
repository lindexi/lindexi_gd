// See https://aka.ms/new-console-template for more information

using System.Security.Principal;
using FefaleregairHelhigacemwai;

if (!OperatingSystem.IsWindows())
{
    return;
}

if (args.Length > 0)
{
    Console.WriteLine($"[{Environment.ProcessId}] 被启动，权限为 {(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) ? "管理员" : "普通用户")};参数为 {string.Join(" ", args)}");
    Console.ReadLine();
    return;
}

Console.WriteLine($"[{Environment.ProcessId}] 启动进程");

var (success, processId, errorMessage) = ProcessRunner.StartProcessWithShellProcessToken(Environment.ProcessPath!, "argument");

if (success)
{
    Console.WriteLine($"启动成功，进程 ID 为 {processId}");
}
else
{
    Console.WriteLine($"启动失败，错误信息为 {errorMessage}");
}

Console.ReadLine();
Console.WriteLine("Hello, World!");
