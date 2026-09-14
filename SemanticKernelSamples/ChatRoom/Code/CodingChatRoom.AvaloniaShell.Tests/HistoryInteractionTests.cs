using AgentLib;
using AgentLib.Logging;
using AgentLib.Model;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class HistoryInteractionTests
{
    [TestMethod]
    public void InitialSessionShouldBeMarkedCurrent()
    {
        var vm = CreateViewModel(new StreamingStore());
        Assert.IsTrue(vm.Sessions.Single().IsCurrent);
    }

    [TestMethod]
    public async Task SwitchingSessionShouldMoveCurrentMarker()
    {
        var store = new StreamingStore();
        store.Continue.TrySetResult();
        var vm = CreateViewModel(store);
        await vm.LoadAsync();
        var item = vm.Sessions.Single(item => item.SessionId == store.Session.SessionId);
        var shell = MainViewModel.CreateForTests(vm, new ChatViewModel());
        shell.OpenSessionCommand.Execute(item);
        await WaitUntilAsync(() => vm.SelectedSession?.SessionId == item.SessionId);
        Assert.AreEqual(item.SessionId, vm.Sessions.Single(candidate => candidate.IsCurrent).SessionId);
    }

    [TestMethod]
    public async Task SwitchingSessionShouldOnlyChangeActiveWorkTask()
    {
        Guid targetSessionId = Guid.NewGuid();
        var firstStore = new StreamingStore(targetSessionId);
        var secondStore = new StreamingStore(targetSessionId);
        firstStore.Continue.TrySetResult();
        secondStore.Continue.TrySetResult();
        var firstManager = new CopilotChatManager();
        var secondManager = new CopilotChatManager();
        var firstSessions = new SessionListViewModel(
            CodingChatApplicationTestFactory.CreateApplication(firstManager, firstStore));
        var secondSessions = new SessionListViewModel(
            CodingChatApplicationTestFactory.CreateApplication(secondManager, secondStore));
        await firstSessions.LoadAsync();
        await secondSessions.LoadAsync();
        var shell = MainViewModel.CreateForTests(firstSessions, new ChatViewModel());
        var secondTask = new WorkTaskItemViewModel("Second", new ChatViewModel(), secondSessions);
        shell.WorkTasks.Add(secondTask);
        shell.ActivateWorkTaskCommand.Execute(secondTask);
        Guid firstSessionId = firstManager.SelectedSession.SessionId;
        SessionItemViewModel targetSession = secondSessions.Sessions.Single(
            item => item.SessionId == targetSessionId);

        shell.OpenSessionCommand.Execute(targetSession);
        await WaitUntilAsync(() => secondManager.SelectedSession.SessionId == targetSessionId);

        Assert.AreEqual((firstSessionId, targetSessionId),
            (firstManager.SelectedSession.SessionId, secondManager.SelectedSession.SessionId));
    }

    [TestMethod]
    public async Task DeletingCurrentSessionShouldMarkReplacement()
    {
        var store = new StreamingStore();
        store.Continue.TrySetResult();
        var vm = CreateViewModel(store);
        await vm.LoadAsync();
        var item = vm.Sessions.Single(item => item.SessionId == store.Session.SessionId);
        var shell = MainViewModel.CreateForTests(vm, new ChatViewModel());
        shell.OpenSessionCommand.Execute(item);
        await WaitUntilAsync(() => vm.SelectedSession?.SessionId == item.SessionId);
        vm.DeleteSessionCommand.Execute(item);
        Assert.AreEqual(vm.SelectedSession?.SessionId, vm.Sessions.Single(candidate => candidate.IsCurrent).SessionId);
    }

    [TestMethod]
    public void CancellingTitleEditShouldRestoreDisplayWithoutChangingTitle()
    {
        var vm = CreateViewModel(new StreamingStore());
        var item = vm.Sessions.Single();
        string title = item.Title;
        vm.EditTitleCommand.Execute(item);
        item.EditedTitle = "Uncommitted title";
        vm.CancelEditCommand.Execute(item);
        Assert.AreEqual((false, title), (item.IsEditing, item.Title));
    }

    [TestMethod]
    public async Task FirstItemShouldAppearBeforeLoadingCompletes()
    {
        var store = new StreamingStore();
        var vm = CreateViewModel(store);
        Task loading = vm.LoadAsync();
        Assert.IsTrue(vm.IsLoading && vm.Sessions.Any(item => item.SessionId == store.Session.SessionId));
        store.Continue.TrySetResult();
        await loading;
    }

    [TestMethod]
    public async Task CommandsShouldRemainEnabledDuringLoading()
    {
        var store = new StreamingStore();
        var vm = CreateViewModel(store);
        var shell = MainViewModel.CreateForTests(vm, new ChatViewModel());
        Task loading = vm.LoadAsync();
        var item = vm.Sessions.Single(item => item.SessionId == store.Session.SessionId);
        bool enabled = shell.OpenSessionCommand.CanExecute(item)
            && vm.EditTitleCommand.CanExecute(item) && vm.DeleteSessionCommand.CanExecute(item);
        store.Continue.TrySetResult();
        await loading;
        Assert.IsTrue(enabled);
    }

    [TestMethod]
    public async Task LaterItemsShouldPreserveTitleEditAndItemIdentity()
    {
        var store = new StreamingStore();
        var vm = CreateViewModel(store);
        Task loading = vm.LoadAsync();
        var item = vm.Sessions.Single(item => item.SessionId == store.Session.SessionId);
        vm.EditTitleCommand.Execute(item);
        item.EditedTitle = "Unfinished edit";
        store.Continue.TrySetResult();
        await loading;
        Assert.IsTrue(ReferenceEquals(item, vm.Sessions.Single(candidate => candidate.SessionId == item.SessionId))
            && item.IsEditing && item.EditedTitle == "Unfinished edit");
    }

    [TestMethod]
    public async Task DeletedSessionShouldNotReturnFromPendingLoad()
    {
        var store = new StreamingStore();
        var vm = CreateViewModel(store);
        Task loading = vm.LoadAsync();
        var item = vm.Sessions.Single(item => item.SessionId == store.Session.SessionId);
        vm.DeleteSessionCommand.Execute(item);
        store.Continue.TrySetResult();
        await loading;
        Assert.IsFalse(vm.Sessions.Any(candidate => candidate.SessionId == item.SessionId));
    }

    [TestMethod]
    public async Task RenamedTitleShouldNotBeOverwrittenByPendingLoad()
    {
        var store = new StreamingStore();
        var vm = CreateViewModel(store);
        Task loading = vm.LoadAsync();
        var item = vm.Sessions.Single(item => item.SessionId == store.Session.SessionId);
        item.EditedTitle = "A complete title longer than twenty characters";
        vm.SaveTitleCommand.Execute(item);
        store.Continue.TrySetResult();
        await loading;
        Assert.AreEqual("A complete title longer than twenty characters",
            vm.Sessions.Single(candidate => candidate.SessionId == item.SessionId).Title);
    }

    [TestMethod]
    public async Task ReturningToHistoryShouldNotReload()
    {
        var store = new StreamingStore();
        store.Continue.TrySetResult();
        var vm = CreateViewModel(store);
        await vm.LoadAsync();
        await vm.LoadAsync();
        Assert.AreEqual(1, store.LoadCount);
    }

    [TestMethod]
    public async Task DuplicateSummariesShouldAppearOnlyOnce()
    {
        var store = new StreamingStore();
        store.Continue.TrySetResult();
        var vm = CreateViewModel(store);
        await vm.LoadAsync();
        Assert.AreEqual(1, vm.Sessions.Count(item => item.SessionId == store.Session.SessionId));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void HistoryNavigationShouldRemainEnabledWhileLoading(bool taskHistory)
    {
        var store = new StreamingStore();
        var shell = MainViewModel.CreateForTests(CreateViewModel(store), new ChatViewModel());
        var command = taskHistory ? shell.OpenTaskHistoryCommand : shell.OpenHistoryCommand;
        try
        {
            command.Execute(null);
            shell.CloseHistoryCommand.Execute(null);
            Assert.IsTrue(command.CanExecute(null));
        }
        finally { store.Continue.TrySetResult(); }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void HistoryNavigationShouldReopenBeforeLoadingCompletes(bool taskHistory)
    {
        var store = new StreamingStore();
        var shell = MainViewModel.CreateForTests(CreateViewModel(store), new ChatViewModel());
        var command = taskHistory ? shell.OpenTaskHistoryCommand : shell.OpenHistoryCommand;
        try
        {
            command.Execute(null);
            shell.CloseHistoryCommand.Execute(null);
            command.Execute(null);
            Assert.IsTrue(shell.IsHistoryOpen);
        }
        finally { store.Continue.TrySetResult(); }
    }

    [TestMethod]
    public void ReenteringHistoryDuringLoadingShouldNotStartDuplicateRead()
    {
        var store = new StreamingStore();
        var shell = MainViewModel.CreateForTests(CreateViewModel(store), new ChatViewModel());
        try
        {
            shell.OpenHistoryCommand.Execute(null);
            shell.CloseHistoryCommand.Execute(null);
            shell.OpenHistoryCommand.Execute(null);
            shell.OpenTaskHistoryCommand.Execute(null);
            Assert.AreEqual(1, store.LoadCount);
        }
        finally { store.Continue.TrySetResult(); }
    }

    private static SessionListViewModel CreateViewModel(StreamingStore store) => new(
        CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), store));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        while (!condition())
        {
            await Task.Delay(10, cancellationTokenSource.Token);
        }
    }

    private sealed class StreamingStore : ICodingChatSessionStore
    {
        public StreamingStore()
            : this(Guid.NewGuid())
        {
        }

        public StreamingStore(Guid sessionId)
        {
            Session = new CopilotChatSession(sessionId, DateTimeOffset.Now);
        }

        public CopilotChatSession Session { get; }
        public TaskCompletionSource Continue { get; } = new();
        public int LoadCount { get; private set; }

        public async IAsyncEnumerable<CopilotChatSessionSummary> EnumerateSessionsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LoadCount++;
            var summary = new CopilotChatSessionSummary
            {
                SessionId = Session.SessionId, Title = Session.Title,
                StartedTime = Session.StartedTime, MessageCount = 0,
            };
            yield return summary;
            await Continue.Task.WaitAsync(cancellationToken);
            yield return summary;
            yield return summary with { SessionId = Guid.NewGuid() };
        }

        public Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Session);
        public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
