using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class DeepSeekImmediateFillTests
{
    [TestMethod]
    [DataRow("https://api.deepseek.com/v1", "official-key")]
    [DataRow("https://API.DEEPSEEK.COM", "official-key")]
    [DataRow("http://api.deepseek.com", null)]
    [DataRow("https://api.deepseek.com:444/v1", null)]
    [DataRow("https://user@api.deepseek.com", null)]
    [DataRow("https://api.deepseek.com.example/v1", null)]
    public void WhenAddingProviderThenOnlyOfficialEndpointFillsKey(string endpoint, string? expected)
    {
        using var settings = new SettingsViewModel();
        settings.Providers.Add(new ProviderSettingsViewModel { EndPoint = endpoint, ApiKey = "official-key" });
        Assert.AreEqual(expected, settings.DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenEditingProviderKeyThenAutomaticKeyUpdates()
    {
        using var settings = CreateSettings();
        settings.Providers[0].ApiKey = "updated-key";
        Assert.AreEqual("updated-key", settings.DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenEditingEndpointThenKeyFillsImmediately()
    {
        using var settings = new SettingsViewModel();
        settings.Providers[0].ApiKey = "official-key";
        settings.Providers[0].EndPoint = "https://api.deepseek.com/v1";
        Assert.AreEqual("official-key", settings.DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenEndpointBecomesUnofficialThenAutomaticKeyClears()
    {
        using var settings = CreateSettings();
        settings.Providers[0].EndPoint = "https://example.com";
        Assert.IsNull(settings.DeepSeekApiKey);
    }

    [TestMethod]
    [DataRow("different-key")]
    [DataRow("official-key")]
    public void WhenUserEntersKeyThenProviderChangesDoNotOverwriteIt(string explicitKey)
    {
        using var settings = CreateSettings();
        settings.DeepSeekApiKey = explicitKey;
        settings.Providers[0].ApiKey = "updated-key";
        Assert.AreEqual(explicitKey, settings.DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenProvidersClearThenOldProviderNoLongerFillsKey()
    {
        using var settings = CreateSettings();
        var removed = settings.Providers[0];
        settings.Providers.Clear();
        removed.ApiKey = "removed-key";
        Assert.IsNull(settings.DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenProviderIsReplacedThenNewProviderFillsKey()
    {
        using var settings = CreateSettings();
        var removed = settings.Providers[0];
        settings.Providers[0] = new ProviderSettingsViewModel { EndPoint = "https://api.deepseek.com", ApiKey = "replacement-key" };
        removed.ApiKey = "removed-key";
        Assert.AreEqual("replacement-key", settings.DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenDisposedThenProviderChangesDoNotUpdateKey()
    {
        var settings = CreateSettings();
        settings.Dispose();
        settings.Providers[0].ApiKey = "updated-key";
        Assert.AreEqual("official-key", settings.DeepSeekApiKey);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void WhenClearingExplicitKeyThenOfficialProviderFillsImmediately(string? cleared)
    {
        using var settings = CreateSettings();
        settings.DeepSeekApiKey = "explicit-test-key";
        settings.DeepSeekApiKey = cleared;
        Assert.AreEqual("official-key", settings.DeepSeekApiKey);
    }

    private static SettingsViewModel CreateSettings()
    {
        var settings = new SettingsViewModel();
        settings.Providers[0].EndPoint = "https://api.deepseek.com/v1";
        settings.Providers[0].ApiKey = "official-key";
        return settings;
    }
}
