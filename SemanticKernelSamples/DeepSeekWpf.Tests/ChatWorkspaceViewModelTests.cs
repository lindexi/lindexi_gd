using System.Runtime.CompilerServices;
using AgentLib.Model;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;
using DeepSeekWpf.ViewModels;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class ChatWorkspaceViewModelTests
{
    [TestMethod]
    public Task InitializeAsync_WithFileIssue_LoadsValidSessionsAndReportsFailure() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var repository = new FakeChatRepository
        {
            LoadResult = new ChatRepositoryLoadResult(
                [CreateSession("valid", "hello")],
                [new ChatLoadIssue("broken.json", "JsonException", "repair")]),
        };
        var viewModel = CreateViewModel(temp, repository: repository);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.Sessions.Count);
        Assert.IsTrue(viewModel.StatusMessage.Contains("失败 1", StringComparison.Ordinal));
    });

    [TestMethod]
    public Task SendMessageCommand_PersistsUserAndAssistantMessages() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var repository = new FakeChatRepository();
        var ai = new FakeAiChatService
        {
            Handler = (_, token) => FakeAiChatService.FromChunks(
                [new AiResponseChunk(AiResponsePart.Thought, "reason"), new AiResponseChunk(AiResponsePart.Content, "answer")], token),
        };
        var viewModel = CreateViewModel(temp, ai, repository);
        await viewModel.InitializeAsync();
        viewModel.PendingUserMessage = "question";

        await viewModel.SendMessageCommand.ExecuteAsync();

        Assert.AreEqual("question", viewModel.SelectedSession!.Messages[0].Content);
        Assert.AreEqual("answer", viewModel.SelectedSession.Messages[1].Content);
        Assert.AreEqual("reason", viewModel.SelectedSession.Messages[1].ThoughtContent);
        Assert.IsTrue(repository.SavedSessions.Count >= 3);
    });

    [TestMethod]
    public Task RetryCommand_RetryableFailure_DoesNotDuplicateUserMessage() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var ai = new FakeAiChatService();
        ai.Handler = (_, token) => ai.CallCount == 1
            ? ThrowAi(new AiChatException(AiChatErrorCategory.RateLimit, "rate", "id", true), token)
            : FakeAiChatService.FromChunks([new AiResponseChunk(AiResponsePart.Content, "recovered")], token);
        var viewModel = CreateViewModel(temp, ai);
        await viewModel.InitializeAsync();
        viewModel.PendingUserMessage = "question";

        await viewModel.SendMessageCommand.ExecuteAsync();
        Assert.IsTrue(viewModel.CanRetry);
        await viewModel.RetryCommand.ExecuteAsync();

        Assert.AreEqual(2, viewModel.SelectedSession!.Messages.Count);
        Assert.AreEqual(1, viewModel.SelectedSession.Messages.Count(message => message.Role == Microsoft.Extensions.AI.ChatRole.User));
        Assert.AreEqual("recovered", viewModel.SelectedSession.Messages.Last().Content);
    });

    [TestMethod]
    public Task SaveInlineEditCommand_TruncatesFollowingHistory() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var session = CreateSession("edit", "first");
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateAssistant("second", false)));
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateUser("third")));
        var repository = new FakeChatRepository { LoadResult = new ChatRepositoryLoadResult([session], []) };
        var viewModel = CreateViewModel(temp, repository: repository);
        await viewModel.InitializeAsync();
        var edited = viewModel.SelectedSession!.Messages[0];
        viewModel.BeginInlineEditCommand.Execute(edited);
        edited.EditingContent = "changed";

        await viewModel.SaveInlineEditCommand.ExecuteAsync(edited);

        Assert.AreEqual("changed", edited.Content);
    });

    [TestMethod]
    public Task DeleteCommand_RejectAcceptAndUndo_FollowsConfirmation() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var session = CreateSession("delete", "value");
        var repository = new FakeChatRepository { LoadResult = new ChatRepositoryLoadResult([session], []) };
        var interaction = new FakeUserInteractionService { ConfirmationResult = false };
        var viewModel = CreateViewModel(temp, repository: repository, interaction: interaction);
        await viewModel.InitializeAsync();

        await viewModel.DeleteSelectedChatCommand.ExecuteAsync();
        Assert.AreEqual(0, repository.DeletedSessionIds.Count);

        interaction.ConfirmationResult = true;
        await viewModel.DeleteSelectedChatCommand.ExecuteAsync();
        Assert.IsTrue(viewModel.UndoDeleteCommand.CanExecute(null));
        await viewModel.UndoDeleteCommand.ExecuteAsync();

        Assert.IsTrue(repository.SavedSessions.Any(item => item.Id == session.Id));
    });

    [TestMethod]
    public Task SearchText_FiltersSessionsByTitleAndContent() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var repository = new FakeChatRepository
        {
            LoadResult = new ChatRepositoryLoadResult(
                [CreateSession("alpha", "ordinary"), CreateSession("beta", "needle content")], []),
        };
        var viewModel = CreateViewModel(temp, repository: repository);
        await viewModel.InitializeAsync();

        viewModel.SearchText = "needle";

        Assert.AreEqual(1, viewModel.FilteredSessions.Cast<ChatSession>().Count());
        Assert.AreEqual("beta", viewModel.FilteredSessions.Cast<ChatSession>().Single().Title);
    });

    [TestMethod]
    public Task Generation_DisablesCommandsAndCancellationProducesNoError() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ai = new FakeAiChatService { Handler = (_, token) => WaitForCancellation(started, token) };
        var viewModel = CreateViewModel(temp, ai);
        await viewModel.InitializeAsync();
        viewModel.PendingUserMessage = "cancel me";

        var sendTask = viewModel.SendMessageCommand.ExecuteAsync();
        await started.Task;
        Assert.IsFalse(viewModel.NewChatCommand.CanExecute(null));
        Assert.IsTrue(viewModel.StopCommand.CanExecute(null));
        viewModel.StopCommand.Execute(null);
        await sendTask;

        Assert.IsFalse(viewModel.HasError);
        Assert.AreEqual("已停止生成", viewModel.StatusMessage);
    });

    private static ChatWorkspaceViewModel CreateViewModel(
        TempDirectory temp,
        FakeAiChatService? ai = null,
        FakeChatRepository? repository = null,
        FakeUserInteractionService? interaction = null)
    {
        var model = new AgentModelDescriptor("fake/model", "fake", "model", null, null, null, null);
        return new ChatWorkspaceViewModel(
            ai ?? new FakeAiChatService(),
            repository ?? new FakeChatRepository(),
            new FakeSettingsService(temp.Path),
            new FakeAgentModelService { RegisteredModels = [model], SelectedModel = model },
            interaction ?? new FakeUserInteractionService(),
            new FakeChatExportService(),
            new FakeAppLogger());
    }

    private static ChatSession CreateSession(string title, string content)
    {
        var session = new ChatSession { Title = title };
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateUser(content)));
        return session;
    }

    private static async IAsyncEnumerable<AiResponseChunk> ThrowAi(
        AiChatException exception,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        throw exception;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<AiResponseChunk> WaitForCancellation(
        TaskCompletionSource started,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        started.SetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        yield break;
    }
}