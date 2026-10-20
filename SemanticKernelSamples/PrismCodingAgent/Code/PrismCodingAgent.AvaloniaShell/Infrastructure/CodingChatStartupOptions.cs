using System;
using System.IO;

namespace PrismCodingAgent.AvaloniaShell.Infrastructure;

internal sealed record CodingChatStartupOptions(bool IsOnboardingDebug, CodingChatRoomPaths Paths, string? DemoLanguage = null)
{
    internal static CodingChatStartupOptions FromArguments(string[]? arguments)
    {
        bool isOnboardingDebug = arguments?.Contains("--debug-onboarding", StringComparer.OrdinalIgnoreCase) == true;
        CodingChatRoomPaths paths = isOnboardingDebug
            ? CodingChatRoomPaths.Create(Path.Join(Path.GetTempPath(), "CodingChatRoom-OnboardingDebug", Guid.NewGuid().ToString("N")))
            : CodingChatRoomPaths.CreateForCurrentUser();
        string? demoLanguage = arguments?.Contains("--demo-en", StringComparer.OrdinalIgnoreCase) == true
            ? "en"
            : arguments?.Contains("--demo-zh", StringComparer.OrdinalIgnoreCase) == true ? "zh-Hans" : null;
        return new CodingChatStartupOptions(isOnboardingDebug, paths, demoLanguage);
    }
}
