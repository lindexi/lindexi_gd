using AgentLib.Model;
using Microsoft.Extensions.AI;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class MessageAuthorTests
{
    [TestMethod]
    public void ChatMessageShouldKeepOriginalAuthor()
    {
        using var viewModel = new MessageItemViewModel(new CopilotChatMessage(ChatRole.Assistant, "answer"));

        Assert.AreEqual("Copilot", viewModel.Author);
    }

    [TestMethod]
    public void ResponsesMessageShouldShowProtocolInAuthor()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "answer");
        using var viewModel = new MessageItemViewModel(message);

        _ = message.ResponseInfo;

        Assert.AreEqual("Copilot (ResponseAPI)", viewModel.Author);
    }

    [TestMethod]
    public void ResponsesMessageShouldNotifyAuthorChange()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "answer");
        using var viewModel = new MessageItemViewModel(message);
        var properties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => properties.Add(args.PropertyName);

        _ = message.ResponseInfo;
        _ = message.ResponseInfo;

        CollectionAssert.AreEqual(new[] { nameof(MessageItemViewModel.Author) }, properties);
    }

    [TestMethod]
    public async Task StoredResponsesMessageShouldPreserveAuthorAsync()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"message-author-{Guid.NewGuid():N}");
        Console.WriteLine(directory);
        var store = new AgentLib.Logging.FileCopilotChatSessionStore(directory);
        var session = new CopilotChatSession();
        var message = new CopilotChatMessage(ChatRole.Assistant, "answer");
        _ = message.ResponseInfo;
        session.ChatMessages.Add(message);

        await store.SaveSessionAsync(session, null);
        var restored = await store.LoadSessionAsync(session.SessionId);
        using var viewModel = new MessageItemViewModel(restored.Messages.Single());

        Assert.AreEqual("Copilot (ResponseAPI)", viewModel.Author);
    }

    [TestMethod]
    public void ClonedResponsesMessageShouldPreserveAuthor()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "answer");
        _ = message.ResponseInfo;

        using var viewModel = new MessageItemViewModel(message.Clone());

        Assert.AreEqual("Copilot (ResponseAPI)", viewModel.Author);
    }
}
