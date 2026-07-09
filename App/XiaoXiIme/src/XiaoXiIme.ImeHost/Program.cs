using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

var options = args.Length > 0
    ? new XiaoXiImeIpcOptions(args[0])
    : XiaoXiImeIpcOptions.Default;

using var hostService = new ImeHostService(options);
using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

hostService.Start();
Console.WriteLine($"XiaoXi IME host started: {options.ServerName}");

try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
}
catch (OperationCanceledException)
{
}
