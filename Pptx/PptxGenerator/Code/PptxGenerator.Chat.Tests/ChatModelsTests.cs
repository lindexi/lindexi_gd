using System.IO;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class ChatModelsTests
{
    private const string Config = """
        {
          "PrimaryModel": "sample/demo",
          "OpenAIConfigurationList": [
            {
              "EndPoint": "https://example.invalid/v1",
              "Key": "test-key",
              "ModelDefinitions": [
                { "Provider": "sample", "ModelName": "demo", "ModelId": "demo-id" },
                { "Provider": "sample", "ModelName": "other" }
              ]
            },
            {
              "EndPoint": "https://second.invalid/api",
              "Key": "test-key",
              "ModelDefinitions": [{ "Provider": "second", "ModelName": "third" }]
            }
          ]
        }
        """;

    [TestMethod]
    public async Task MissingPreferredFileLoadsFallbackPrimaryAsync()
    {
        var (preferred, fallback) = CreatePaths();
        await File.WriteAllTextAsync(fallback, Config);

        var models = await ChatModels.LoadAsync(preferred, fallback);

        Assert.AreEqual("demo-id", models.Primary?.Id);
    }

    [TestMethod]
    public async Task InvalidPreferredFileIsReportedInsteadOfLoadingFallbackAsync()
    {
        var (preferred, fallback) = CreatePaths();
        await File.WriteAllTextAsync(preferred, "{");
        await File.WriteAllTextAsync(fallback, Config);

        var models = await ChatModels.LoadAsync(preferred, fallback);

        Assert.AreEqual(("ConfigInvalid", preferred), (models.NoticeKey, models.FailedPath));
    }

    [TestMethod]
    public async Task LoadsModelsFromAllConfiguredProvidersAsync()
    {
        var (preferred, fallback) = CreatePaths();
        await File.WriteAllTextAsync(preferred, Config);

        var models = await ChatModels.LoadAsync(preferred, fallback);

        CollectionAssert.AreEqual(new[] { "demo-id", "other", "third" }, models.Options.Select(model => model.Id).ToArray());
    }

    [TestMethod]
    public async Task AmbiguousPrimaryLeavesSelectionEmptyAsync()
    {
        var (preferred, fallback) = CreatePaths();
        await File.WriteAllTextAsync(preferred, Config.Replace("\"other\"", "\"demo\"", StringComparison.Ordinal));

        var models = await ChatModels.LoadAsync(preferred, fallback);

        Assert.IsNull(models.Primary);
    }

    private static (string Preferred, string Fallback) CreatePaths()
    {
        var directory = Directory.CreateTempSubdirectory("PptxGenerator.Chat-").FullName;
        return (Path.Combine(directory, "Preferred.json"), Path.Combine(directory, "Fallback.json"));
    }
}
