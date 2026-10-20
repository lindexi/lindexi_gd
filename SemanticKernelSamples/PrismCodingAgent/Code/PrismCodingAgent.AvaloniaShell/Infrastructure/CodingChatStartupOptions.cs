using System;
using System.IO;

namespace PrismCodingAgent.AvaloniaShell.Infrastructure;

internal sealed record CodingChatStartupOptions(bool IsOnboardingDebug, CodingChatRoomPaths Paths)
{
    internal static CodingChatStartupOptions FromArguments(string[]? arguments)
    {
        bool isOnboardingDebug = arguments?.Contains("--debug-onboarding", StringComparer.OrdinalIgnoreCase) == true;
        CodingChatRoomPaths paths = isOnboardingDebug
            ? CodingChatRoomPaths.Create(Path.Join(Path.GetTempPath(), "CodingChatRoom-OnboardingDebug", Guid.NewGuid().ToString("N")))
            : CodingChatRoomPaths.CreateForCurrentUser();
        return new CodingChatStartupOptions(isOnboardingDebug, paths);
    }
}
