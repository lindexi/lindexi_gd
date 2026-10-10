namespace CodingAgent.AvaloniaShell.Services.Settings;

internal sealed record CodingChatShellSettings
{
    public bool IsWindowsSandboxEnabled { get; init; } = true;

    public string WindowsSandboxToolPath { get; init; } = "WinRemoteShell.exe";

    public string WindowsSandboxServerAddress { get; init; } = "127.0.0.1:12399";

    public bool IsNetworkProxyEnabled { get; init; }

    public string? NetworkProxyAddress { get; init; }

    public bool BypassProxyOnLocal { get; init; } = true;

    public bool IsCopilotInstructionsEnabled { get; init; }

    public string? CopilotInstructionsPath { get; init; }
}