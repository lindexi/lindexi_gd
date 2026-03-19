using System.Collections.ObjectModel;
using System.IO;

using HohedaqalcheaHalrehofewe.Commands;
using HohedaqalcheaHalrehofewe.Services;

namespace HohedaqalcheaHalrehofewe.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    private readonly UndercoverGameService _gameService;
    private readonly Dictionary<string, TimelineEntry> _timelineLookup = new();
    private TaskCompletionSource<string>? _pendingUserInputSource;
    private GameSession? _session;
    private string _apiKeyFilePath = @"C:\lindexi\Work\Doubao.txt";
    private string _deploymentId = "ep-20260306101224-c8mtg";
    private string _mainWord = "青柠";
    private string _spyWord = "柠檬";
    private string _playerCountText = "6";
    private string _humanPlayerIndexText = "0";
    private string _statusText = "准备开始新局。";
    private string _playerWordText = "尚未分配";
    private string _userPromptText = "开始新局后，轮到你时会在这里提示输入。";
    private string _pendingUserInput = string.Empty;
    private bool _isGameInitialized;
    private bool _isRoundRunning;
    private bool _isAwaitingUserInput;

    public MainWindowViewModel()
        : this(new UndercoverGameService())
    {
    }

    public MainWindowViewModel(UndercoverGameService gameService)
    {
        ArgumentNullException.ThrowIfNull(gameService);
        _gameService = gameService;
        StartNewGameCommand = new AsyncRelayCommand(StartNewGameAsync, () => !_isRoundRunning);
        StartNextRoundCommand = new AsyncRelayCommand(StartNextRoundAsync, () => _isGameInitialized && !_isRoundRunning);
        SubmitInputCommand = new RelayCommand(SubmitPendingUserInput, () => _isAwaitingUserInput && !string.IsNullOrWhiteSpace(PendingUserInput));
    }

    public ObservableCollection<PlayerSeat> Players { get; } = [];

    public ObservableCollection<TimelineEntry> Timeline { get; } = [];

    public AsyncRelayCommand StartNewGameCommand { get; }

    public AsyncRelayCommand StartNextRoundCommand { get; }

    public RelayCommand SubmitInputCommand { get; }

    public string ApiKeyFilePath
    {
        get => _apiKeyFilePath;
        set => SetAndRefresh(ref _apiKeyFilePath, value);
    }

    public string DeploymentId
    {
        get => _deploymentId;
        set => SetAndRefresh(ref _deploymentId, value);
    }

    public string MainWord
    {
        get => _mainWord;
        set => SetAndRefresh(ref _mainWord, value);
    }

    public string SpyWord
    {
        get => _spyWord;
        set => SetAndRefresh(ref _spyWord, value);
    }

    public string PlayerCountText
    {
        get => _playerCountText;
        set => SetAndRefresh(ref _playerCountText, value);
    }

    public string HumanPlayerIndexText
    {
        get => _humanPlayerIndexText;
        set => SetAndRefresh(ref _humanPlayerIndexText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string PlayerWordText
    {
        get => _playerWordText;
        private set => SetProperty(ref _playerWordText, value);
    }

    public string UserPromptText
    {
        get => _userPromptText;
        private set => SetProperty(ref _userPromptText, value);
    }

    public string PendingUserInput
    {
        get => _pendingUserInput;
        set
        {
            if (SetProperty(ref _pendingUserInput, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public bool CanEditConfiguration => !_isRoundRunning;

    public bool IsAwaitingUserInput
    {
        get => _isAwaitingUserInput;
        private set
        {
            if (SetProperty(ref _isAwaitingUserInput, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public async Task StartNewGameAsync()
    {
        try
        {
            var settings = ReadSettings();
            _session = _gameService.CreateSession(settings);
            BindSession(settings, _session);
            await RunRoundAsync();
        }
        catch (IOException exception)
        {
            ReportError(exception.Message);
        }
        catch (ArgumentException exception)
        {
            ReportError(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ReportError(exception.Message);
        }
    }

    public async Task StartNextRoundAsync()
    {
        if (_session is null)
        {
            ReportError("请先开始新局。");
            return;
        }

        try
        {
            await RunRoundAsync();
        }
        catch (IOException exception)
        {
            ReportError(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ReportError(exception.Message);
        }
    }

    private async Task RunRoundAsync()
    {
        if (_session is null)
        {
            throw new InvalidOperationException("游戏会话尚未初始化。");
        }

        _isRoundRunning = true;
        OnPropertyChanged(nameof(CanEditConfiguration));
        RefreshCommandStates();

        try
        {
            await _gameService.RunRoundAsync(
                _session,
                RequestHumanInputAsync,
                HandleTimelineDelta,
                ActivatePlayer,
                status => StatusText = status);

            UpdateAllPlayerStatuses("等待下一回合", isCurrent: false);
            UserPromptText = "当前未轮到玩家输入。点击“开始下一回合”继续。";
            PendingUserInput = string.Empty;
            StatusText = "本回合已完成。";
        }
        finally
        {
            _pendingUserInputSource = null;
            IsAwaitingUserInput = false;
            _isRoundRunning = false;
            OnPropertyChanged(nameof(CanEditConfiguration));
            RefreshCommandStates();
        }
    }

    private async Task<string> RequestHumanInputAsync(HumanTurnRequest request)
    {
        _pendingUserInputSource = new TaskCompletionSource<string>();
        IsAwaitingUserInput = true;
        UserPromptText = request.Prompt;
        PendingUserInput = string.Empty;
        ActivatePlayer(request.PlayerIndex, $"第 {(_session?.CurrentRound ?? 0)} 回合处理中");
        StatusText = $"正在等待第 {request.PlayerIndex} 人输入。";

        var output = (await _pendingUserInputSource.Task).Trim();
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("玩家输入不能为空。");
        }

        HandleTimelineDelta(new TimelineDelta(request.EntryKey, request.EntryHeader, output, request.Kind));
        IsAwaitingUserInput = false;
        return output;
    }

    private void SubmitPendingUserInput()
    {
        if (_pendingUserInputSource is null || _pendingUserInputSource.Task.IsCompleted)
        {
            return;
        }

        var input = PendingUserInput.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            StatusText = "请输入内容后再提交。";
            return;
        }

        _pendingUserInputSource.SetResult(input);
        PendingUserInput = string.Empty;
        UserPromptText = "输入已提交，正在继续流程。";
    }

    private GameSettings ReadSettings()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyFilePath))
        {
            throw new ArgumentException("请填写 API Key 文件路径。");
        }

        if (!File.Exists(ApiKeyFilePath))
        {
            throw new FileNotFoundException("找不到 API Key 文件。", ApiKeyFilePath);
        }

        if (string.IsNullOrWhiteSpace(DeploymentId))
        {
            throw new ArgumentException("请填写部署 Id。");
        }

        if (string.IsNullOrWhiteSpace(MainWord))
        {
            throw new ArgumentException("请填写平民词。");
        }

        if (string.IsNullOrWhiteSpace(SpyWord))
        {
            throw new ArgumentException("请填写卧底词。");
        }

        if (!int.TryParse(PlayerCountText, out var playerCount) || playerCount < 3)
        {
            throw new ArgumentException("玩家人数必须是不小于 3 的整数。");
        }

        if (!int.TryParse(HumanPlayerIndexText, out var humanPlayerIndex) || humanPlayerIndex < 0 || humanPlayerIndex >= playerCount)
        {
            throw new ArgumentException("玩家角色编号必须在有效范围内。");
        }

        return new GameSettings(
            ApiKeyFilePath.Trim(),
            DeploymentId.Trim(),
            MainWord.Trim(),
            SpyWord.Trim(),
            playerCount,
            humanPlayerIndex);
    }

    private void BindSession(GameSettings settings, GameSession session)
    {
        Timeline.Clear();
        Players.Clear();
        _timelineLookup.Clear();

        foreach (var player in session.Players)
        {
            Players.Add(new PlayerSeat(player.Index, player.IsHuman, player.Word)
            {
                StatusText = "等待开始",
                IsCurrent = false,
            });
        }

        _isGameInitialized = true;
        PlayerWordText = $"你是第 {settings.HumanPlayerIndex} 人 · 当前词语：{session.Players[settings.HumanPlayerIndex].Word}";
        UserPromptText = "点击“开始新局”后自动进入首回合，之后可继续开始下一回合。";
        StatusText = "新局已初始化，准备进入第 0 回合。";
        PendingUserInput = string.Empty;
        HandleTimelineDelta(new TimelineDelta("session-created", "系统", "新局已创建。最后一名角色默认持有卧底词。", TimelineKinds.System));
        RefreshCommandStates();
    }

    private void ActivatePlayer(int playerIndex, string statusText)
    {
        foreach (var player in Players)
        {
            var isCurrent = player.Index == playerIndex;
            player.IsCurrent = isCurrent;
            player.StatusText = isCurrent ? statusText : "等待中";
        }

        StatusText = statusText;
    }

    private void UpdateAllPlayerStatuses(string statusText, bool isCurrent)
    {
        foreach (var player in Players)
        {
            player.IsCurrent = isCurrent;
            player.StatusText = statusText;
        }
    }

    private void HandleTimelineDelta(TimelineDelta delta)
    {
        if (!_timelineLookup.TryGetValue(delta.EntryKey, out var entry))
        {
            entry = new TimelineEntry(delta.Header, delta.Kind);
            _timelineLookup[delta.EntryKey] = entry;
            Timeline.Add(entry);
        }

        entry.Append(delta.Text);
    }

    private void ReportError(string message)
    {
        StatusText = message;
        HandleTimelineDelta(new TimelineDelta($"error-{Guid.NewGuid():N}", "系统提示", message, TimelineKinds.System));
    }

    private bool SetAndRefresh(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        var changed = SetProperty(ref field, value, propertyName);
        if (changed)
        {
            RefreshCommandStates();
        }

        return changed;
    }

    private void RefreshCommandStates()
    {
        StartNewGameCommand.NotifyCanExecuteChanged();
        StartNextRoundCommand.NotifyCanExecuteChanged();
        SubmitInputCommand.NotifyCanExecuteChanged();
    }
}
