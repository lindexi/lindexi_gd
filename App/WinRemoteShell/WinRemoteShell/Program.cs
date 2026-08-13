using DotNetCampus.Cli;
using WinRemoteShell.Commands;

var commandLine = CommandLine.Parse(args);
await commandLine
    .AddHandler<ServerCommand>()
    .AddHandler<ExecCommand>()
    .AddHandler<ListCommand>()
    .AddHandler<ChangeDirectoryCommand>()
    .AddHandler<ShellCommand>()
    .AddHandler<PushCommand>()
    .AddHandler<PullCommand>()
    .AddHandler<ScreenshotCommand>()
    .RunAsync();
