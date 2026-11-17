using FractalForge.Research.Deepening;

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
Console.CancelKeyPress += handler;
try
{
    return await DeepeningRunner.RunAsync(args, Console.Out, Console.Error, cancellation.Token);
}
finally { Console.CancelKeyPress -= handler; }
