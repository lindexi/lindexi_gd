#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using System.ClientModel.Primitives;
using System.Net;

using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Tests.Core;

[TestClass]
public sealed class ResponsesClientProviderTests
{
    [TestMethod]
    public void LoadConfiguration_FromJson_AllModelsProvideResponsesClients()
    {
        var manager = new AgentApiEndpointManager();
        using var httpClient = manager.HttpClient;

        manager.LoadConfiguration(CreateConfiguration());

        Assert.HasCount(2, manager.GetSupportedModels().Cast<IResponsesClientProvider>().ToArray());
    }

    [TestMethod]
    public void ReplaceConfiguration_FromJson_AllModelsProvideResponsesClients()
    {
        var manager = new AgentApiEndpointManager();
        using var httpClient = manager.HttpClient;
        manager.LoadConfiguration(CreateConfiguration());

        manager.ReplaceConfiguration(CreateConfiguration());

        Assert.HasCount(2, manager.GetSupportedModels().Cast<IResponsesClientProvider>().ToArray());
    }

    [TestMethod]
    public async Task GetResponsesClientAsync_FromJson_UsesConfiguredEndpoint()
    {
        var manager = new AgentApiEndpointManager();
        using var httpClient = manager.HttpClient;
        manager.LoadConfiguration(CreateConfiguration());
        var provider = (IResponsesClientProvider) manager.PrimaryModel;

        var clientContext = await provider.GetResponsesClientAsync();

        Assert.AreEqual(new Uri("https://example.com/v1"), clientContext.Client.Endpoint);
    }

    [TestMethod]
    public async Task GetChatClientAsync_FromJson_StillCreatesChatClient()
    {
        var manager = new AgentApiEndpointManager();
        using var httpClient = manager.HttpClient;
        manager.LoadConfiguration(CreateConfiguration());

        using var client = await manager.PrimaryModel.GetChatClientAsync();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    public async Task GetResponsesClientAsync_UsesSharedHttpClientAndConfiguredCredentials()
    {
        var manager = new AgentApiEndpointManager();
        using var originalHttpClient = manager.HttpClient;
        using var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        manager.HttpClient = httpClient;
        manager.LoadConfiguration(CreateConfiguration());
        var clientContext = await ((IResponsesClientProvider) manager.PrimaryModel).GetResponsesClientAsync();

        await clientContext.Client.DeleteResponseAsync("response-test", new RequestOptions());

        Assert.AreEqual(
            "DELETE https://example.com/v1/responses/response-test Bearer test-key",
            handler.RequestDetails);
    }

    [TestMethod]
    [DataRow("explicit-model", null, "explicit-model")]
    [DataRow(null, null, "test-model")]
    [DataRow(null, "mapped-model", "mapped-model")]
    [DataRow("explicit-model", "mapped-model", "explicit-model")]
    public async Task GetResponsesClientAsync_ShouldReturnResolvedModelIdAsync(string? modelId, string? mappedId, string expectedId)
    {
        var definition = new AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition
        {
            Provider = "test", ModelName = "test-model", ModelId = modelId,
        };
        using var httpClient = new HttpClient();
        var manager = new AgentApiEndpointManager();
        manager.HttpClient?.Dispose();
        manager.HttpClient = httpClient;
        var provider = new JsonConfigurationOpenAIProtocolLanguageModelProvider(
            new OpenAIProtocolLanguageModelConfiguration("https://example.com/v1", "test-key")
            {
                ModelDefinitions = [definition],
            }, manager)
        {
            ModelNameToIdMap = new DictionaryModelNameToIdMap(mappedId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["test-model"] = mappedId }),
        };

        var clientContext = await ((IResponsesClientProvider)provider.GetSupportedModels().Single()).GetResponsesClientAsync();

        Assert.AreEqual(expectedId, clientContext.ModelId);
    }

    [TestMethod]
    public void GetSupportedModels_ShouldPreserveModelDefinition()
    {
        var definition = new AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition
        {
            Provider = "test", ModelName = "test-model",
        };
        var provider = new JsonConfigurationOpenAIProtocolLanguageModelProvider(
            new OpenAIProtocolLanguageModelConfiguration("https://example.com/v1", "test-key")
            {
                ModelDefinitions = [definition],
            });

        var model = provider.GetSupportedModels().Single();

        Assert.AreSame(definition, model.ModelDefinition);
    }

    private static AgentApiManagerConfiguration CreateConfiguration()
    {
        return AgentApiManagerConfiguration.FromJsonString("""
            {
              "PrimaryModel": "test/first",
              "OpenAIConfigurationList": [
                {
                  "EndPoint": "https://example.com/v1",
                  "Key": "test-key",
                  "ModelDefinitions": [
                    { "Provider": "test", "ModelName": "first", "ModelId": "model-1" },
                    { "Provider": "test", "ModelName": "second", "ModelId": "model-2" }
                  ]
                }
              ]
            }
            """);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestDetails { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestDetails = $"{request.Method} {request.RequestUri} {request.Headers.Authorization}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
