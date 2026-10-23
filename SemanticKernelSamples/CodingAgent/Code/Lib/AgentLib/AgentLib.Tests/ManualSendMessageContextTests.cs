using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;

namespace AgentLib.Tests;

[TestClass]
public sealed class ManualSendMessageContextTests
{
    [TestMethod]
    public async Task CreateManualSendMessageContextAsync_PreservesSelectedModel()
    {
        var setup = CopilotChatManagerTestContext.Create(new FakeChatClient());
        var model = setup.ChatManager.AgentApiEndpointManager.PrimaryModel;

        var context = await setup.ChatManager.CreateManualSendMessageContextAsync();
        var replacement = new FakeLanguageModel(new FakeChatClient());
        setup.ChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([replacement]));
        setup.ChatManager.AgentApiEndpointManager.PrimaryModel = replacement;

        Assert.AreSame(model, context.LanguageModel);
    }

    [TestMethod]
    public async Task CreateManualSendMessageContextAsync_PreservesChatClient()
    {
        var setup = CopilotChatManagerTestContext.Create(new FakeChatClient());

        var context = await setup.ChatManager.CreateManualSendMessageContextAsync();

        Assert.AreSame(setup.PrimaryChatClient, context.ChatClient);
    }

    [TestMethod]
    public async Task CreateManualSendMessageContextAsync_CancelingCreationTokenDoesNotCancelAgentInitialization()
    {
        var setup = CopilotChatManagerTestContext.Create(new FakeChatClient());
        using var source = new CancellationTokenSource();
        var context = await setup.ChatManager.CreateManualSendMessageContextAsync(source.Token);
        source.Cancel();

        var agent = await context.GetChatClientAgentAsync();

        Assert.IsNotNull(agent);
    }
}
