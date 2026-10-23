#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using OpenAI.Responses;

namespace AgentLib.Tests.Core;

[TestClass]
public sealed class ResponsesClientContextTests
{
    [TestMethod]
    public void Constructor_ShouldRejectNullClient()
    {
        Assert.Throws<ArgumentNullException>(() => new ResponsesClientContext(null!, "model-id"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Constructor_ShouldRejectMissingModelId(string? modelId)
    {
        var client = new ResponsesClient("test-key");

        Assert.Throws<ArgumentException>(() => new ResponsesClientContext(client, modelId!));
    }

    [TestMethod]
    public void Constructor_ShouldPreserveClient()
    {
        var client = new ResponsesClient("test-key");

        var clientContext = new ResponsesClientContext(client, "model-id");

        Assert.AreSame(client, clientContext.Client);
    }

    [TestMethod]
    public void Constructor_ShouldPreserveModelId()
    {
        var clientContext = new ResponsesClientContext(new ResponsesClient("test-key"), "model-id");

        Assert.AreEqual("model-id", clientContext.ModelId);
    }
}
