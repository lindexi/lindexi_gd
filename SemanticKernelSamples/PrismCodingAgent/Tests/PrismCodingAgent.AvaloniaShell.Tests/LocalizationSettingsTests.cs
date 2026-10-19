using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Settings;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class LocalizationSettingsTests
{
    [TestMethod]
    public async Task WhenSavingLanguageThenItCanBeLoadedAgain()
    {
        string root = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new CodingChatSettingsService(CodingChatRoomPaths.Create(root),
            new WindowsSandboxToolSource(false, string.Empty, string.Empty));
        await service.SaveAsync(new AgentApiManagerConfiguration(), new CodingChatShellSettings { LanguageTag = "en" });

        var settings = await service.LoadInitialShellSettingsAsync();

        Assert.AreEqual("en", settings.LanguageTag);
    }
}
