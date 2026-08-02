using DotNetCampus.Cli;
using WinRemoteShell.Commands;

var commandLine = CommandLine.Parse(args);
await commandLine
    .AddHandler<ServerCommand>()
    .AddHandler<ExecCommand>()
    .AddHandler<ShellCommand>()
    .AddHandler<PushCommand>()
    .AddHandler<PullCommand>()
    .AddHandler<ScreenshotCommand>()
    .RunAsync();
