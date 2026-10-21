using System.Text;
using DotNetCampus.Cli;
using WinRemoteShell.Commands;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);

var commandLine = CommandLine.Parse(args);
await commandLine
    .AddHandler<ServerCommand>()
    .AddHandler<ExecCommand>()
    .AddHandler<ListCommand>()
    .AddHandler<ChangeDirectoryCommand>()
    .AddHandler<ShellCommand>()
    .AddHandler<PushCommand>()
    .AddHandler<PullCommand>()
    .AddHandler<UpdateCommand>()
    .AddHandler<ApplyUpdateCommand>()
    .AddHandler<ScreenshotCommand>()
    .AddHandler<ProcessListCommand>()
    .AddHandler<KillCommand>()
    .RunAsync();
