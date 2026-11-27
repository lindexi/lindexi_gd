using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.ViewModels;

internal sealed class ChatViewModel : Notify
{
    private readonly ChatConversation _conversation = new();
    private readonly Dispatcher _dispatcher;
    private readonly ChatSessionStore _sessions;
    private Guid _sessionId = Guid.NewGuid();
    private IReadOnlyList<SessionSummary> _history = Array.Empty<SessionSummary>();
    private bool _historyOpen;
    private bool _historyLoading;
    private string _historySearch = string.Empty;
    private string _historyNotice = string.Empty;
    private readonly Command _send;
    private readonly Command _stop;
    private CancellationTokenSource? _turn;
    private string _draft = string.Empty;
    private string _workPath = ChatConversation.DefaultWorkPath;
    private string _mcpAddress = "http://127.0.0.1:7916/mcp";
    private string _workNotice = string.Empty;
    private string _composerNotice = string.Empty;
    private string? _failedStartupMcpAddress;
    private ChatModel? _model;

    internal ChatViewModel(ChatModels models, Dispatcher dispatcher, ChatSessionStore? sessions = null)
    {
        _dispatcher = dispatcher;
        _sessions = sessions ?? new ChatSessionStore(ChatSessionStore.DefaultDirectory);
        Models = models.Options;
        _model = models.Primary;
        ModelNotice = models.NoticeKey is not { } key ? string.Empty :
            models.FailedPath is { } path ? UiText.Format(key, path) : UiText.Get(key);
        _send = new Command(async () => await SendAsync(), () => CanSend);
        _stop = new Command(Stop, () => IsBusy && _turn?.IsCancellationRequested == false);
    }

    internal async Task CheckStartupMcpAsync()
    {
        var configuredAddress = McpAddress;
        if (ChatModels.TryHttpUri(configuredAddress, out var address) && !await McpTools.ProbeAsync(address))
        {
            _failedStartupMcpAddress = configuredAddress;
            Raise(nameof(WelcomeMcpNotice));
        }
    }

    public string WelcomeMcpNotice => _failedStartupMcpAddress == McpAddress ? UiText.Get("WelcomeMcpFailed") : string.Empty;

    public IReadOnlyList<ChatModel> Models { get; }
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ChatImage> Images { get; } = new();
    public ICommand SendCommand => _send;
    public ICommand StopCommand => _stop;
    public bool IsBusy => _turn is not null;
    public bool IsIdle => !IsBusy;
    public bool CanAttach => Model?.SupportsImages == true;
    public bool IsChatOpen => !IsHistoryOpen;
    public bool IsHistoryOpen
    {
        get => _historyOpen;
        private set
        {
            if (!Set(ref _historyOpen, value)) return;
            Raise(nameof(IsChatOpen));
            RefreshSend();
        }
    }
    public bool IsHistoryLoading { get => _historyLoading; private set => Set(ref _historyLoading, value); }
    public string HistoryNotice { get => _historyNotice; private set => Set(ref _historyNotice, value); }
    public string HistorySearch
    {
        get => _historySearch;
        set { if (Set(ref _historySearch, value)) Raise(nameof(History)); }
    }
    public IReadOnlyList<SessionSummary> History => _history.Where(session =>
        session.Title.Contains(HistorySearch.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
    public bool CanSend => IsIdle && IsChatOpen && Model is not null && (Images.Count == 0 || CanAttach) &&
        (!string.IsNullOrWhiteSpace(Draft) || Images.Count != 0);
    public string ModelNotice { get; private set; }
    public string WorkNotice { get => _workNotice; private set => Set(ref _workNotice, value); }
    public string ComposerNotice { get => _composerNotice; internal set => Set(ref _composerNotice, value); }
    public string McpNotice => string.IsNullOrWhiteSpace(McpAddress) || ChatModels.TryHttpUri(McpAddress, out _)
        ? string.Empty : UiText.Get("McpInvalid");

    public ChatModel? Model
    {
        get => _model;
        set
        {
            if (!Set(ref _model, value)) return;
            if (value is not null)
            {
                ModelNotice = string.Empty;
                Raise(nameof(ModelNotice));
            }
            Raise(nameof(CanAttach));
            RefreshSend();
        }
    }

    public string Draft
    {
        get => _draft;
        set { if (Set(ref _draft, value)) RefreshSend(); }
    }

    public string WorkPath
    {
        get => _workPath;
        set { if (Set(ref _workPath, value)) WorkNotice = string.Empty; }
    }

    public string McpAddress
    {
        get => _mcpAddress;
        set
        {
            if (!Set(ref _mcpAddress, value)) return;
            Raise(nameof(McpNotice));
            Raise(nameof(WelcomeMcpNotice));
        }
    }

    internal async Task AddImagesAsync(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            try
            {
                string? mediaType = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    _ => null,
                };
                if (mediaType is null)
                {
                    ComposerNotice = UiText.Get("ImageFormat");
                    continue;
                }
                Images.Add(new ChatImage(Path.GetFileName(path), mediaType, await File.ReadAllBytesAsync(path)));
                RefreshSend();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                ChatDiagnostics.Failure("AttachImage", exception, "ImagePath", path);
                ComposerNotice = UiText.Format("ImageFailed", Path.GetFileName(path));
            }
        }
    }

    internal void RemoveImage(ChatImage image)
    {
        Images.Remove(image);
        RefreshSend();
    }

    internal void Stop()
    {
        _turn?.Cancel();
        _stop.Refresh();
    }

    internal async Task ShowHistoryAsync()
    {
        if (IsBusy || IsHistoryLoading) return;
        IsHistoryOpen = true;
        await RunHistoryOperationAsync("ListHistory", ChatSessionStore.DefaultDirectory, async () =>
        {
            var (sessions, unreadable) = await _sessions.ListAsync();
            _history = sessions;
            Raise(nameof(History));
            if (unreadable > 0) HistoryNotice = UiText.Format("HistorySkipped", unreadable.ToString());
        });
    }

    internal async Task OpenSessionAsync(SessionSummary summary)
    {
        if (IsBusy || IsHistoryLoading) return;
        await RunHistoryOperationAsync("OpenSession", summary.Id.ToString("N"), async () =>
        {
            var session = await _sessions.LoadAsync(summary.Id);
            var messages = session.Messages.Select(ChatMessage.Restore).ToArray();
            ResetSession(summary.Id, session.Messages, session.TargetBinding);
            foreach (var message in messages) Messages.Add(message);
            IsHistoryOpen = false;
        });
    }

    internal void CloseHistory() { if (!IsHistoryLoading) IsHistoryOpen = false; }

    internal void NewSession()
    {
        if (IsBusy || IsHistoryLoading) return;
        ResetSession(Guid.NewGuid(), Array.Empty<SessionMessage>());
        IsHistoryOpen = false;
    }

    private void ResetSession(Guid id, SessionMessage[] messages, McpTargetBinding? binding = null)
    {
        _conversation.Restore(messages, binding);
        _sessionId = id;
        Messages.Clear();
        Draft = string.Empty;
        Images.Clear();
        ComposerNotice = string.Empty;
        RefreshSend();
    }

    private async Task RunHistoryOperationAsync(string operation, string target, Func<Task> action)
    {
        IsHistoryLoading = true;
        HistoryNotice = string.Empty;
        try { await action(); }
        catch (Exception exception) when (ChatSessionStore.IsStorageError(exception))
        {
            ChatDiagnostics.Failure(operation, exception, "Target", target);
            HistoryNotice = UiText.Get("HistoryReadFailed");
        }
        finally { IsHistoryLoading = false; }
    }

    private async Task SaveSessionAsync()
    {
        var messages = Messages.Select(message => message.Snapshot()).ToArray();
        var first = messages[0].Text;
        string title = first.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } words
            ? string.Join(" ", words) : UiText.Get("PriorImage");
        var info = new System.Globalization.StringInfo(title);
        if (info.LengthInTextElements > 64) title = info.SubstringByTextElements(0, 64) + "…";
        var session = new ChatSession(new SessionSummary(_sessionId, title, DateTimeOffset.UtcNow), messages, _conversation.TargetBinding);
        try { await _sessions.SaveAsync(session); }
        catch (Exception exception) when (ChatSessionStore.IsStorageError(exception))
        {
            ChatDiagnostics.Failure("SaveSession", exception, "SessionId", _sessionId.ToString("N"));
            ComposerNotice = UiText.Get("HistorySaveFailed");
        }
    }

    private async Task SendAsync()
    {
        if (!CanSend || Model is not { } model) return;
        var images = Images.ToArray();
        ChatModels.TryHttpUri(McpAddress, out var address);
        var turn = new ChatTurn(model, WorkPath, address, Draft.Trim(), images);
        var user = new ChatMessage(UiText.Get("UserRole"), true, turn.Text, images);
        var assistant = new ChatMessage(UiText.Get("AssistantRole"), false, string.Empty, Array.Empty<ChatImage>())
        {
            Status = UiText.Get("ReplyWaiting"),
        };
        using var cancellation = new CancellationTokenSource();
        _turn = cancellation;
        RefreshTurn();
        Draft = string.Empty;
        Images.Clear();
        ComposerNotice = string.Empty;
        WorkNotice = string.Empty;
        Messages.Add(user);
        Messages.Add(assistant);

        try
        {
            await _conversation.RunAsync(turn, (index, text) => OnUi(() =>
            {
                assistant.Status = string.Empty;
                assistant.AppendText(index, text);
            }), (index, tool) => OnUi(() =>
            {
                assistant.Status = string.Empty;
                assistant.UpdateTool(index, tool);
            }), notice => OnUi(() => assistant.Notices.Add(notice)),
                notice => OnUi(() => WorkNotice = notice),
                usage => OnUi(() => assistant.AddUsage(usage)), cancellation.Token);
            assistant.Status = cancellation.IsCancellationRequested ? UiText.Get("ReplyStopped") :
                assistant.Parts.Count == 0 && assistant.Notices.Count == 0 ? UiText.Get("ReplyEmpty") : string.Empty;
        }
        catch (Exception) when (cancellation.IsCancellationRequested)
        {
            assistant.Status = UiText.Get("ReplyStopped");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ChatDiagnostics.Failure("Send", exception, "ModelId", model.Id);
            assistant.Status = exception is System.ClientModel.ClientResultException { Status: 401 or 403 }
                ? UiText.Get("ReplyUnauthorized") : UiText.Get("ReplyFailed");
        }
        finally
        {
            var pending = UiText.Get("ToolPending");
            var skipped = UiText.Get(cancellation.IsCancellationRequested ? "ToolStopped" : "ToolSkipped");
            foreach (var part in assistant.Parts.OfType<ChatToolPart>())
                if (part.Activity is { } activity && activity.State == pending)
                    part.Activity = activity with { State = skipped };
            await SaveSessionAsync();
            _turn = null;
            RefreshTurn();
        }
    }

    private void OnUi(Action action)
    {
        if (!_dispatcher.HasShutdownStarted) _dispatcher.Invoke(action);
    }

    private void RefreshTurn()
    {
        Raise(nameof(IsBusy));
        Raise(nameof(IsIdle));
        _stop.Refresh();
        RefreshSend();
    }

    private void RefreshSend()
    {
        Raise(nameof(CanSend));
        _send.Refresh();
    }

    private sealed class Command : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        internal Command(Action execute, Func<bool> canExecute) => (_execute, _canExecute) = (execute, canExecute);
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute();
        public void Execute(object? parameter) { if (_canExecute()) _execute(); }
        internal void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
