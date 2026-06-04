using AgentLib.Model;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PptxGenerator;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly DelegateCommand _sendMessageCommand;
    private bool _isBusy;
    private bool _isFirstMessage = true;
    private bool _attachPreview;
    private string _inputText = "请发挥你的想象力，制作一个精美的页面介绍 SlideML —— 一种用 XML 描述幻灯片排版的标记语言，支持 Page、Panel、Rect、TextElement、Image 等标签在 1280×720 画布上自由布局。";
    private string _statusText = "等待开始";

    public MainWindowViewModel(SlideChatManager slideChatManager)
    {
        SlideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
        SlideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;
        _sendMessageCommand = new DelegateCommand(() => _ = RunSendMessageAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(InputText));
    }

    /// <summary>
    /// 订阅 SlideChatManager 的属性变更，在工具调用过程中即可实时刷新 UI 绑定。
    /// </summary>
    private void OnSlideChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SlideChatManager.PreviewBitmap):
            case nameof(SlideChatManager.CurrentSlideXml):
            case nameof(SlideChatManager.RenderedXml):
            case nameof(SlideChatManager.WarningText):
                OnPropertyChanged(e.PropertyName!);
                break;
        }
    }

    /// <summary>
    /// SlideML 聊天管理器，暴露给界面直接绑定，避免无意义的属性转发。
    /// </summary>
    public SlideChatManager SlideChatManager { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// 聊天气泡消息列表，绑定到 CopilotChatManager 的消息集合。
    /// </summary>
    public ObservableCollection<CopilotChatMessage> ChatMessages => SlideChatManager.ChatManager.ChatMessages;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _sendMessageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                _sendMessageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// 是否在发送消息时附加当前渲染预览图。
    /// </summary>
    public bool AttachPreview
    {
        get => _attachPreview;
        set => SetProperty(ref _attachPreview, value);
    }

    private async Task RunSendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var message = InputText;
        InputText = string.Empty;

        IsBusy = true;
        StatusText = "正在生成页面...";

        var isFirstMessage = _isFirstMessage;
        if (_isFirstMessage)
        {
            _isFirstMessage = false;
        }

        var attachPreview = _attachPreview;

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await SlideChatManager.SendMessageAsync(message, isFirstMessage, attachPreview, cancellationTokenSource.Token).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = "完成";
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "操作已取消。";
        }
        catch (Exception ex)
        {
            StatusText = "执行失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
