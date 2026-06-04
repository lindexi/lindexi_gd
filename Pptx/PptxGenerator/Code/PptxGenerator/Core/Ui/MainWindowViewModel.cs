using AgentLib.Model;

using Avalonia.Media.Imaging;

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
    private readonly SlideChatManager _slideChatManager;
    private readonly DelegateCommand _sendMessageCommand;
    private bool _isBusy;
    private string _inputText = string.Empty;
    private string _statusText = "等待开始";

    public MainWindowViewModel(SlideChatManager slideChatManager)
    {
        _slideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
        _sendMessageCommand = new DelegateCommand(() => _ = RunSendMessageAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(InputText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// 聊天气泡消息列表，绑定到 CopilotChatManager 的消息集合。
    /// </summary>
    public ObservableCollection<CopilotChatMessage> ChatMessages => _slideChatManager.ChatManager.ChatMessages;

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

    public Bitmap? PreviewBitmap => _slideChatManager.PreviewBitmap;

    public string CurrentSlideXml => _slideChatManager.CurrentSlideXml;

    public string RenderedXml => _slideChatManager.RenderedXml;

    public string WarningText => _slideChatManager.WarningText;

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

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await _slideChatManager.SendSlideRequestAsync(message, cancellationTokenSource.Token).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(PreviewBitmap));
                OnPropertyChanged(nameof(CurrentSlideXml));
                OnPropertyChanged(nameof(RenderedXml));
                OnPropertyChanged(nameof(WarningText));
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
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(WarningText));
            });
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
