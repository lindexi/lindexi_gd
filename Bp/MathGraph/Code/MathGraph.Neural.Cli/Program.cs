using MathGraphs.Neural.Cli;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
return await CpuApplication.RunAsync(args, Console.Out, cancellation.Token);
