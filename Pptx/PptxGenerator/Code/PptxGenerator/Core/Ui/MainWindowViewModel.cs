using Avalonia.Media.Imaging;
using Avalonia.Threading;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PptxGenerator;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly SlideGenerationService _slideGenerationService;
    private readonly DelegateCommand _generateCommand;
    private readonly DelegateCommand _continueCommand;
    private bool _isBusy;
    private string _prompt = "做一页介绍 SlideML 的实验性单页幻灯片，要求包含标题、三张卡片和一段总结，整体采用浅色科技风。";
    private string _followUpMessage = "";
    private string _statusText = "等待开始";
    private string _warningText = "";
    private string _currentSlideXml = "";
    private string _renderedXml = "";
    private Bitmap? _previewBitmap;
    private string _conversationText = "";
    private string _lastOriginalPrompt = "";

    public MainWindowViewModel(SlideGenerationService slideGenerationService)
    {
        _slideGenerationService = slideGenerationService ?? throw new ArgumentNullException(nameof(slideGenerationService));
        Iterations = new ObservableCollection<SlideGenerationIteration>();
        _generateCommand = new DelegateCommand(() => _ = RunGenerateAsync(), () => !IsBusy);
        _continueCommand = new DelegateCommand(() => _ = RunContinueAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(CurrentSlideXml) && !string.IsNullOrWhiteSpace(FollowUpMessage));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand GenerateCommand => _generateCommand;

    public ICommand ContinueCommand => _continueCommand;

    public ObservableCollection<SlideGenerationIteration> Iterations { get; }

    public string Prompt
    {
        get => _prompt;
        set => SetProperty(ref _prompt, value);
    }

    public string FollowUpMessage
    {
        get => _followUpMessage;
        set
        {
            if (SetProperty(ref _followUpMessage, value))
            {
                _continueCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string WarningText
    {
        get => _warningText;
        private set => SetProperty(ref _warningText, value);
    }

    public string CurrentSlideXml
    {
        get => _currentSlideXml;
        private set
        {
            if (SetProperty(ref _currentSlideXml, value))
            {
                _continueCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string RenderedXml
    {
        get => _renderedXml;
        private set => SetProperty(ref _renderedXml, value);
    }

    public Bitmap? PreviewBitmap
    {
        get => _previewBitmap;
        private set => SetProperty(ref _previewBitmap, value);
    }

    public string ConversationText
    {
        get => _conversationText;
        private set => SetProperty(ref _conversationText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _generateCommand.RaiseCanExecuteChanged();
                _continueCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private async Task RunGenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            StatusText = "请输入页面需求。";
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            StatusText = "正在调用模型并生成页面...";
            var session = await _slideGenerationService.GenerateAsync(Prompt, cancellationToken).ConfigureAwait(false);
            _lastOriginalPrompt = Prompt;
            await Dispatcher.UIThread.InvokeAsync(() => ApplySessionResult(session, clearFollowUp: false));
            StatusText = $"完成，共迭代 {session.Iterations.Count} 轮";
        }).ConfigureAwait(false);
    }

    private async Task RunContinueAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentSlideXml))
        {
            StatusText = "当前没有可继续修改的页面。";
            return;
        }

        if (string.IsNullOrWhiteSpace(FollowUpMessage))
        {
            StatusText = "请输入新的修改意见。";
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            StatusText = "正在继续对话并修正页面...";
            var session = await _slideGenerationService.ContinueConversationAsync(_lastOriginalPrompt, SplitConversation(ConversationText), CurrentSlideXml, FollowUpMessage, cancellationToken).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplySessionResult(session, clearFollowUp: true);
                FollowUpMessage = string.Empty;
            });
            StatusText = $"已根据最新意见完成 {session.Iterations.Count} 轮修正";
        }).ConfigureAwait(false);
    }

    private async Task ExecuteBusyAsync(Func<CancellationToken, Task> action)
    {
        IsBusy = true;
        WarningText = string.Empty;

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await action(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            StatusText = "操作已取消。";
        }
        catch (Exception ex)
        {
            StatusText = "执行失败";
            WarningText = ex.ToString();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySessionResult(SlideGenerationSessionResult session, bool clearFollowUp)
    {
        Iterations.Clear();
        foreach (var iteration in session.Iterations)
        {
            Iterations.Add(iteration);
        }

        CurrentSlideXml = session.FinalSlideXml;
        RenderedXml = session.FinalRenderResult.OutputXml;
        PreviewBitmap = session.FinalRenderResult.PreviewBitmap;
        WarningText = session.FinalRenderResult.Warnings.Count == 0
            ? "没有警告，页面看起来已经可用。"
            : string.Join(Environment.NewLine, session.FinalRenderResult.Warnings);
        ConversationText = string.Join(Environment.NewLine + Environment.NewLine, session.ConversationMessages);

        if (clearFollowUp)
        {
            FollowUpMessage = string.Empty;
        }
    }

    private static string[] SplitConversation(string conversation)
    {
        return string.IsNullOrWhiteSpace(conversation)
            ? []
            : conversation.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
