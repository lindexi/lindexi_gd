using System.Globalization;

namespace PrismCodingAgent.AvaloniaShell.Services.Settings;

internal sealed record CodingChatShellSettings
{
    public string LanguageTag { get; init; } = GetDefaultLanguageTag();

    private static string GetDefaultLanguageTag()
    {
        for (var culture = CultureInfo.CurrentUICulture; culture.Name.Length > 0; culture = culture.Parent)
        {
            if (culture.Name is "zh-Hans" or "en")
            {
                return culture.Name;
            }
        }

        return "zh-Hans";
    }

    public bool IsWindowsSandboxEnabled { get; init; } = true;

    public string WindowsSandboxToolPath { get; init; } = "WinRemoteShell.exe";

    public string WindowsSandboxServerAddress { get; init; } = "127.0.0.1:12399";

    public string? DeepSeekApiKey { get; init; }

    public bool IsNetworkProxyEnabled { get; init; }

    public string? NetworkProxyAddress { get; init; }

    public bool BypassProxyOnLocal { get; init; } = true;

    public bool IsCopilotInstructionsEnabled { get; init; }

    public string? CopilotInstructionsPath { get; init; }
}