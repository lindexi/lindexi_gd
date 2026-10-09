using System.Net;
using System.Text;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class ResponsesCompactionTests
{
    [TestMethod]
    [DataRow(200)]
    [DataRow(400)]
    public async Task CompactShouldApplyOnlySuccessfulServerOutput(int status)
    {
        using var handler = new CompactHandler(status);
        using var http = new HttpClient(handler);
        var manager = new CopilotChatManager();
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = http;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString("""
            {"PrimaryModel":"demo","OpenAIConfigurationList":[{"EndPoint":"https://example.com/v1","Key":"test-key",
            "ModelDefinitions":[{"ModelName":"demo","ModelId":"model-id","Provider":"test"}]}]}
            """));
        var context = await manager.CreateManualSendMessageContextAsync();
        await using var coding = new CodingAgent();
        var agent = new ResponsesCodingAgent(coding);
        var state = manager.SelectedSession.ResponsesSession;
        var original = ResponseItem.CreateUserMessageItem("original");
        state.AppendItem(original);
        if (status == 400)
        {
            await Assert.ThrowsAsync<System.ClientModel.ClientResultException>(() =>
                agent.TryCompactConversationAsync(context.LanguageModel, state));
            Assert.AreSame(original, state.Items.Single());
        }
        else
        {
            Assert.IsTrue(await agent.TryCompactConversationAsync(context.LanguageModel, state));
            string json = System.ClientModel.Primitives.ModelReaderWriter.Write(state.Items.Single()).ToString();
            StringAssert.Contains(json, "encrypted_content");
        }
    }

    private sealed class CompactHandler(int status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith("/responses/compact", StringComparison.Ordinal));
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent(status == 200
                    ? """{"output":[{"type":"compaction","id":"cmp_test","encrypted_content":"opaque-state"}]}"""
                    : """{"error":{"message":"compact unavailable","type":"invalid_request_error"}}""",
                    Encoding.UTF8, "application/json"),
            });
        }
    }
}
