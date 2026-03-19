using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System.ClientModel;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace HohedaqalcheaHalrehofewe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string Endpoint = "https://ark.cn-beijing.volces.com/api/v3";

    private ChatClientAgent? _agent;
    private TaskCompletionSource<string>? _pendingUserInputSource;
    private bool _isGameInitialized;
    private bool _isRoundRunning;
    private int _currentRound;
    private string _apiKeyFilePath = @"C:\lindexi\Work\Doubao.txt";
    private string _deploymentId = "ep-20260306101224-c8mtg";
    private string _mainWord = "青柠";
    private string _spyWord = "柠檬";
    private string _playerCountText = "6";
    private string _humanPlayerIndexText = "0";
    private string _statusText = "请先开始新局。";
    private string _playerWordText = "尚未分配";
    private string _userPromptText = "当前未轮到玩家输入。";
    private string _pendingUserInput = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Timeline.CollectionChanged += OnTimelineCollectionChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PlayerSeat> Players { get; } = [];

    public ObservableCollection<TimelineEntry> Timeline { get; } = [];

    public string ApiKeyFilePath
    {
        get => _apiKeyFilePath;
        set => SetProperty(ref _apiKeyFilePath, value);
    }

    public string DeploymentId
    {
        get => _deploymentId;
        set => SetProperty(ref _deploymentId, value);
    }

    public string MainWord
    {
        get => _mainWord;
        set => SetProperty(ref _mainWord, value);
    }

    public string SpyWord
    {
        get => _spyWord;
        set => SetProperty(ref _spyWord, value);
    }

    public string PlayerCountText
    {
        get => _playerCountText;
        set => SetProperty(ref _playerCountText, value);
    }

    public string HumanPlayerIndexText
    {
        get => _humanPlayerIndexText;
        set => SetProperty(ref _humanPlayerIndexText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string PlayerWordText
    {
        get => _playerWordText;
        set => SetProperty(ref _playerWordText, value);
    }

    public string UserPromptText
    {
        get => _userPromptText;
        set => SetProperty(ref _userPromptText, value);
    }

    public string PendingUserInput
    {
        get => _pendingUserInput;
        set => SetProperty(ref _pendingUserInput, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DataContext = this;
        UpdateUiState();
    }

    private async void OnStartNewGameClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = ReadSettings();
            InitializeAgent(settings);
            InitializeGame(settings);
            await RunRoundAsync();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            StatusText = exception.Message;
            MessageBox.Show(this, exception.Message, "无法开始新局", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateUiState();
        }
    }

    private async void OnStartNextRoundClick(object sender, RoutedEventArgs e)
    {
        if (!_isGameInitialized || _isRoundRunning)
        {
            return;
        }

        try
        {
            await RunRoundAsync();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            StatusText = exception.Message;
            MessageBox.Show(this, exception.Message, "无法开始下一回合", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateUiState();
        }
    }

    private void OnSubmitInputClick(object sender, RoutedEventArgs e)
    {
        SubmitPendingUserInput();
    }

    private void OnUserInputTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SubmitPendingUserInput();
            e.Handled = true;
        }
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

    private void InitializeAgent(GameSettings settings)
    {
        var key = File.ReadAllText(settings.ApiKeyFilePath).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("API Key 文件内容为空。");
        }

        var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
        {
            Endpoint = new Uri(Endpoint)
        });

        var chatClient = openAiClient.GetChatClient(settings.DeploymentId);
        _agent = chatClient.AsIChatClient().AsAIAgent();
    }

    private void InitializeGame(GameSettings settings)
    {
        Players.Clear();
        Timeline.Clear();

        for (var index = 0; index < settings.PlayerCount; index++)
        {
            var word = index == settings.PlayerCount - 1 ? settings.SpyWord : settings.MainWord;
            var isHuman = index == settings.HumanPlayerIndex;
            var prompt = CreatePlayerPrompt(index, word);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
            };

            Players.Add(new PlayerSeat(index, isHuman, word, messages));
        }

        _currentRound = 0;
        _isGameInitialized = true;
        PlayerWordText = $"你是第 {settings.HumanPlayerIndex} 人，词语是：{Players[settings.HumanPlayerIndex].Word}";
        UserPromptText = "点击“开始新局”后会自动进入首回合，之后可继续开始下一回合。";
        PendingUserInput = string.Empty;
        StatusText = "新局已初始化，准备进入第 0 回合。";
        AppendEntry("系统", "新局已创建。最后一名角色持有卧底词，你可以通过调整玩家角色编号决定是否由自己接管该角色。", true);
        UpdateAllPlayerStatuses("等待开始");
        UpdateUiState();
    }

    private async Task RunRoundAsync()
    {
        if (_agent is null)
        {
            throw new InvalidOperationException("AI 客户端尚未初始化。");
        }

        _isRoundRunning = true;
        UpdateUiState();

        try
        {
            AppendEntry("系统", $"第 {_currentRound} 回合开始。", true);
            await RunSpeakingPhaseAsync(_currentRound);
            await RunVotingPhaseAsync(_currentRound);
            AppendEntry("系统", $"第 {_currentRound} 回合结束。你可以开始下一回合。", true);
            UpdateAllPlayerStatuses("等待下一回合");
            UserPromptText = "当前未轮到玩家输入。点击“开始下一回合”继续。";
            PendingUserInput = string.Empty;
            StatusText = "本回合已完成。";
            _currentRound++;
        }
        finally
        {
            _pendingUserInputSource = null;
            _isRoundRunning = false;
            UpdateUiState();
        }
    }

    private async Task RunSpeakingPhaseAsync(int round)
    {
        StatusText = $"第 {round} 回合发言阶段进行中。";
        BroadcastJudgeMessage($"第 {round} 回合开始，请开始你们的发言");

        foreach (var player in Players)
        {
            SetCurrentPlayer(player, $"第 {round} 回合发言中");

            var output = player.IsHuman
                ? await WaitForHumanInputAsync(player, $"你是第 {player.Index} 人，请输入你的发言。", $"第 {player.Index} 人 发言")
                : await RunAiTurnAsync(player, "发言");

            BroadcastPlayerSpeech(player, output);
        }
    }

    private async Task RunVotingPhaseAsync(int round)
    {
        StatusText = $"第 {round} 回合投票阶段进行中。";
        AppendEntry("系统", $"第 {round} 回合发言结束，进入投票阶段。", true);

        foreach (var player in Players)
        {
            var prompt = $"第 {round} 回合结束，请根据以上各人发言，请你猜测谁可能是卧底。你是第 {player.Index} 人。";
            player.Messages.Add(new ChatMessage(ChatRole.System, prompt));
            SetCurrentPlayer(player, $"第 {round} 回合投票中");

            var output = player.IsHuman
                ? await WaitForHumanInputAsync(player, $"你是第 {player.Index} 人，请输入你的投票或判断理由。", $"第 {player.Index} 人 投票")
                : await RunAiTurnAsync(player, "投票");

            player.Messages.Add(new ChatMessage(ChatRole.Assistant, output));
        }
    }

    private async Task<string> RunAiTurnAsync(PlayerSeat player, string actionName)
    {
        if (_agent is null)
        {
            throw new InvalidOperationException("AI 客户端尚未初始化。");
        }

        TimelineEntry? reasoningEntry = null;
        TimelineEntry? outputEntry = null;

        await foreach (var update in _agent.RunReasoningStreamingAsync(player.Messages))
        {
            if (!string.IsNullOrEmpty(update.Reasoning))
            {
                reasoningEntry ??= AppendEntry($"第 {player.Index} 人 思考", string.Empty, false);
                reasoningEntry.Append(update.Reasoning);
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                outputEntry ??= AppendEntry($"第 {player.Index} 人 {actionName}", string.Empty, false);
                outputEntry.Append(update.Text);
            }
        }

        var output = outputEntry?.Content.Trim();
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException($"第 {player.Index} 人未生成有效的 {actionName} 内容。");
        }

        return output;
    }

    private void BroadcastJudgeMessage(string message)
    {
        foreach (var player in Players)
        {
            player.Messages.Add(new ChatMessage(new ChatRole("裁判"), message));
        }
    }

    private void BroadcastPlayerSpeech(PlayerSeat speaker, string output)
    {
        foreach (var player in Players)
        {
            if (ReferenceEquals(player, speaker))
            {
                player.Messages.Add(new ChatMessage(ChatRole.Assistant, output));
            }
            else
            {
                player.Messages.Add(new ChatMessage(ChatRole.User, $"第 {speaker.Index} 人: {output}"));
            }
        }
    }

    private async Task<string> WaitForHumanInputAsync(PlayerSeat player, string prompt, string entryHeader)
    {
        _pendingUserInputSource = new TaskCompletionSource<string>();
        UserPromptText = prompt;
        PendingUserInput = string.Empty;
        StatusText = $"正在等待第 {player.Index} 人输入。";
        UpdateUiState();

        await Dispatcher.InvokeAsync(() => UserInputTextBox.Focus(), DispatcherPriority.Normal);
        var output = (await _pendingUserInputSource.Task).Trim();
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("玩家输入不能为空。");
        }

        AppendEntry(entryHeader, output, false);
        return output;
    }

    private void SubmitPendingUserInput()
    {
        if (_pendingUserInputSource is null || _pendingUserInputSource.Task.IsCompleted)
        {
            return;
        }

        var text = PendingUserInput.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "请输入内容后再提交。", "输入为空", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _pendingUserInputSource.SetResult(text);
        PendingUserInput = string.Empty;
        UserPromptText = "输入已提交，正在继续流程。";
        UpdateUiState();
    }

    private void SetCurrentPlayer(PlayerSeat currentPlayer, string statusText)
    {
        foreach (var player in Players)
        {
            player.StatusText = ReferenceEquals(player, currentPlayer) ? statusText : "等待中";
        }

        StatusText = statusText;
    }

    private void UpdateAllPlayerStatuses(string statusText)
    {
        foreach (var player in Players)
        {
            player.StatusText = statusText;
        }
    }

    private TimelineEntry AppendEntry(string header, string content, bool insertSeparator)
    {
        var entry = new TimelineEntry(header)
        {
            Content = content,
        };

        Timeline.Add(entry);

        if (insertSeparator)
        {
            Timeline.Add(new TimelineEntry("------------"));
        }

        return entry;
    }

    private string CreatePlayerPrompt(int index, string word)
    {
        return $"""
                你正在参与一个“谁是卧底”的游戏。每人每轮只能说一句话描述自己拿到的词语（不能直接说出那个词语），既不能让卧底发现，也要给同伴以暗示。当你能够确定某个人是“卧底”的时候，你可以去指认他，如果指认错误，你将会出局，请谨慎指认。如果指认成功，你将赢得比赛。如果你发现自己是卧底，也可以指认自己。
                你是第 {index} 人
                你拿到的词语是："{word}"
                在发言阶段只输出一句公开发言；在投票阶段给出你的判断和理由。
                """;
    }

    private void UpdateUiState()
    {
        var awaitingUserInput = _pendingUserInputSource is { Task.IsCompleted: false };
        StartNewGameButton.IsEnabled = !_isRoundRunning;
        StartNextRoundButton.IsEnabled = _isGameInitialized && !_isRoundRunning;
        SubmitInputButton.IsEnabled = awaitingUserInput;
        UserInputTextBox.IsEnabled = awaitingUserInput;
        ApiKeyFilePathTextBox.IsEnabled = !_isRoundRunning;
        DeploymentIdTextBox.IsEnabled = !_isRoundRunning;
        MainWordTextBox.IsEnabled = !_isRoundRunning;
        SpyWordTextBox.IsEnabled = !_isRoundRunning;
        PlayerCountTextBox.IsEnabled = !_isRoundRunning;
        HumanPlayerIndexTextBox.IsEnabled = !_isRoundRunning;
    }

    private void OnTimelineCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Timeline.Count == 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(() => TimelineListBox.ScrollIntoView(Timeline[^1]), DispatcherPriority.Background);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}