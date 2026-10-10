using CodingChatRoom.AvaloniaShell.Infrastructure;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingChatStartupOptionsTests
{
    [TestMethod]
    public void NoArgumentsShouldUseProductionDirectory()
    {
        var options = CodingChatStartupOptions.FromArguments(null);
        Assert.AreEqual(CodingChatRoomPaths.CreateForCurrentUser().RootDirectory, options.Paths.RootDirectory);
    }

    [DataTestMethod]
    [DataRow("--debug-onboarding")]
    [DataRow("--DEBUG-ONBOARDING")]
    public void DebugArgumentShouldEnableOnboarding(string argument)
    {
        var options = CodingChatStartupOptions.FromArguments([argument]);
        Assert.IsTrue(options.IsOnboardingDebug);
    }

    [TestMethod]
    public void SimilarArgumentShouldNotEnableDebugMode()
    {
        var options = CodingChatStartupOptions.FromArguments(["--debug-onboarding=false"]);
        Assert.IsFalse(options.IsOnboardingDebug);
    }

    [TestMethod]
    public void DebugDirectoryShouldBeOutsideProductionDirectory()
    {
        var options = CodingChatStartupOptions.FromArguments(["--debug-onboarding"]);
        string production = CodingChatRoomPaths.CreateForCurrentUser().RootDirectory + Path.DirectorySeparatorChar;
        Assert.IsFalse(options.Paths.RootDirectory.StartsWith(production, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void EachDebugLaunchShouldUseNewDirectory()
    {
        var first = CodingChatStartupOptions.FromArguments(["--debug-onboarding"]);
        var second = CodingChatStartupOptions.FromArguments(["--debug-onboarding"]);
        Assert.AreNotEqual(first.Paths.RootDirectory, second.Paths.RootDirectory);
    }

    [TestMethod]
    public void AllApplicationDataShouldUseDebugDirectory()
    {
        var paths = CodingChatStartupOptions.FromArguments(["--debug-onboarding"]).Paths;
        string[] locations =
        [
            paths.ConfigurationFile.FullName, paths.ShellSettingsFile.FullName, paths.WorkTasksFile.FullName,
            paths.LogDirectory, paths.SessionDirectory.FullName, paths.AbilitiesDirectory.FullName,
        ];
        Assert.IsTrue(locations.All(path => path.StartsWith(paths.RootDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)));
    }
}
