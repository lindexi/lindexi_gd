using System.Globalization;
using System.Text.Json;
using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Settings;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class LocalizationSettingsTests
{
    [TestMethod]
    [DataRow("en", "en")]
    [DataRow("en-US", "en")]
    [DataRow("en-GB", "en")]
    [DataRow("zh-Hans", "zh-Hans")]
    [DataRow("zh-CN", "zh-Hans")]
    [DataRow("fr-FR", "zh-Hans")]
    public void WhenLanguageIsNotConfiguredThenDefaultMatchesSupportedSystemLanguage(string cultureName, string expectedLanguage)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            var settings = new CodingChatShellSettings();

            Assert.AreEqual(expectedLanguage, settings.LanguageTag);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    [DataRow("en", "en")]
    [DataRow("en-US", "en")]
    [DataRow("en-GB", "en")]
    [DataRow("zh-Hans", "zh-Hans")]
    [DataRow("zh-CN", "zh-Hans")]
    [DataRow("fr-FR", "zh-Hans")]
    public void WhenSavedSettingsOmitLanguageThenDefaultMatchesSupportedSystemLanguage(string cultureName, string expectedLanguage)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            var settings = JsonSerializer.Deserialize<CodingChatShellSettings>("{}");

            Assert.AreEqual(expectedLanguage, settings?.LanguageTag);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    [DataRow("zh-CN", "en")]
    [DataRow("en-US", "zh-Hans")]
    public async Task WhenSavingLanguageThenItCanBeLoadedAgain(string cultureName, string savedLanguage)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            string root = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var service = new CodingChatSettingsService(CodingChatRoomPaths.Create(root),
                new WindowsSandboxToolSource(false, string.Empty, string.Empty));
            await service.SaveAsync(new AgentApiManagerConfiguration(), new CodingChatShellSettings { LanguageTag = savedLanguage });

            var settings = await service.LoadInitialShellSettingsAsync();

            Assert.AreEqual(savedLanguage, settings.LanguageTag);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
