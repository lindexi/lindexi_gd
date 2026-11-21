using FractalForge.Research.Adaptive;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
return await AdaptiveRunner.RunAsync(args, Console.Out, Console.Error, cancellation.Token);
