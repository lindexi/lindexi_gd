namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed record CodingChatShellSettings
{
    public bool IsWindowsSandboxEnabled { get; init; } = true;

    public string WindowsSandboxToolPath { get; init; } = "WinRemoteShell.exe";

    public string WindowsSandboxServerAddress { get; init; } = "127.0.0.1:12399";
}
