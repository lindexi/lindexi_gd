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
    private readonly Command _send;
    private readonly Command _stop;
    private CancellationTokenSource? _turn;
    private string _draft = string.Empty;
    private string _workPath = ChatConversation.DefaultWorkPath;
    private string _mcpAddress = "http://127.0.0.1:7916/mcp";
    private string _workNotice = string.Empty;
    private string _composerNotice = string.Empty;
    private ChatModel? _model;

    internal ChatViewModel(ChatModels models, Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        Models = models.Options;
        _model = models.Primary;
        ModelNotice = models.NoticeKey is not { } key ? string.Empty :
            models.FailedPath is { } path ? UiText.Format(key, path) : UiText.Get(key);
        _send = new Command(async () => await SendAsync(), () => CanSend);
        _stop = new Command(Stop, () => IsBusy && _turn?.IsCancellationRequested == false);
    }

    public IReadOnlyList<ChatModel> Models { get; }
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ChatImage> Images { get; } = new();
    public ICommand SendCommand => _send;
    public ICommand StopCommand => _stop;
    public bool IsBusy => _turn is not null;
    public bool IsIdle => !IsBusy;
    public bool CanAttach => Model?.SupportsImages == true;
    public bool CanSend => IsIdle && Model is not null && (Images.Count == 0 || CanAttach) &&
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
        set { if (Set(ref _mcpAddress, value)) Raise(nameof(McpNotice)); }
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
                notice => OnUi(() => WorkNotice = notice), cancellation.Token);
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
