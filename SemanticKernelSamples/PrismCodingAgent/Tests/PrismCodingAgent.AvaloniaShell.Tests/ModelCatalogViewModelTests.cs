using PrismCodingAgent.AvaloniaShell.Services.Integrations.OpenAI;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class ModelCatalogViewModelTests
{
    [TestMethod]
    public void ManualAdditionShouldTrimModelId()
    {
        var provider = new ProviderSettingsViewModel();
        using var model = new ModelCatalogViewModel(provider);
        model.ModelId = " coding-model ";
        model.AddManualCommand.Execute(null);
        Assert.AreEqual("coding-model", provider.Models.Single().ModelId);
    }

    [TestMethod]
    public void RepeatedAdditionShouldNotDuplicateModel()
    {
        var provider = new ProviderSettingsViewModel();
        using var model = new ModelCatalogViewModel(provider);
        model.AddCommand.Execute("coding-model");
        model.AddCommand.Execute("coding-model");
        Assert.HasCount(1, provider.Models);
    }

    [TestMethod]
    public void BlankModelShouldNotBeAdded()
    {
        var provider = new ProviderSettingsViewModel();
        using var model = new ModelCatalogViewModel(provider);
        model.AddCommand.Execute(" ");
        Assert.IsEmpty(provider.Models);
    }

    [TestMethod]
    public async Task UnsupportedCatalogShouldStillAllowManualAddition()
    {
        var provider = new ProviderSettingsViewModel { EndPoint = "https://example.com/v1" };
        var client = new CatalogClient(Task.FromResult(OpenAIModelCatalogResult.Failure("unsupported")));
        using var model = new ModelCatalogViewModel(provider, client);
        await model.LoadAsync();
        model.AddCommand.Execute("manual-model");
        Assert.AreEqual("manual-model", provider.Models.Single().ModelId);
    }

    [TestMethod]
    public async Task ConnectionChangeShouldDiscardStaleResponse()
    {
        var provider = new ProviderSettingsViewModel { EndPoint = "https://example.com/v1" };
        var response = new TaskCompletionSource<OpenAIModelCatalogResult>();
        var client = new CatalogClient(response.Task);
        using var model = new ModelCatalogViewModel(provider, client);
        Task load = model.LoadAsync();
        provider.EndPoint = "https://other.example/v1";
        response.SetResult(OpenAIModelCatalogResult.Success(["stale-model"]));
        await load;
        Assert.IsEmpty(model.Models);
    }

    [TestMethod]
    public void HttpEndpointWithUserInfoShouldPassFormatValidation()
    {
        Assert.IsTrue(ModelCatalogViewModel.HasValidEndpoint(new ProviderSettingsViewModel
        {
            EndPoint = "https://user:password@example.com/v1",
        }));
    }

    [DataTestMethod]
    [DataRow("", "SetupEndpointRequired")]
    [DataRow("   ", "SetupEndpointRequired")]
    [DataRow("not-an-address", "SetupInvalidEndpoint")]
    [DataRow("file:///tmp/models", "SetupInvalidEndpoint")]
    public async Task InvalidEndpointShouldShowSpecificMessage(string endpoint, string resourceKey)
    {
        using var model = new ModelCatalogViewModel(new ProviderSettingsViewModel { EndPoint = endpoint });
        await model.LoadAsync();
        Assert.AreEqual(ModelCatalogViewModel.Text(resourceKey), model.Status);
    }

    private sealed class CatalogClient(Task<OpenAIModelCatalogResult> result) : IOpenAIModelCatalogClient
    {
        public Task<OpenAIModelCatalogResult> GetModelIdsAsync(string endPoint, string apiKey, CancellationToken cancellationToken = default)
            => result;
    }

    [DataTestMethod]
    [DataRow("file:///tmp/models")]
    [DataRow("")]
    public void InvalidEndpointShouldBeRejected(string endpoint)
    {
        Assert.IsFalse(ModelCatalogViewModel.HasValidEndpoint(new ProviderSettingsViewModel { EndPoint = endpoint }));
    }
}
