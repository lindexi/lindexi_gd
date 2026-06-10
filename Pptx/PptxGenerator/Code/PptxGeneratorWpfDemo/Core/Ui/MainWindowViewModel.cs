using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PptxGenerator;

/// <summary>
/// ComboBox 中用于显示模型选项的数据项，包含供应商和模型名。
/// </summary>
public sealed record ModelDisplayItem(string Provider, string ModelName)
{
    /// <summary>
    /// 用于 ComboBox 展示的格式化字符串，格式："供应商: 模型名"。
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Provider) ? ModelName : $"{Provider}: {ModelName}";
}

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly DelegateCommand _sendMessageCommand;
    private readonly DelegateCommand _attachImageCommand;
    private readonly DelegateCommand _evaluateCommand;
    private readonly DelegateCommand _evaluatePromptCommand;
    private bool _isBusy;
    private bool _isFirstMessage = true;
    private bool _attachPreview;
    private string _inputText = "请发挥你的想象力，制作一个精美的页面介绍 SlideML —— 一种用 XML 描述幻灯片排版的标记语言，支持 Page、Panel、Rect、TextElement、Image 等标签在 1280×720 画布上自由布局。";
    private string _statusText = "等待开始";
    private string _evaluationSummaryText = string.Empty;
    private string _lastUserPrompt = string.Empty;
    private ModelDisplayItem _selectedModelItem;

    public MainWindowViewModel(SlideChatManager slideChatManager)
    {
        SlideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
        SlideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;

        var pipeline = slideChatManager.Pipeline;
        pipeline.EvaluationCompleted += OnEvaluationCompleted;
        pipeline.PromptEvaluationCompleted += OnPromptEvaluationCompleted;

        var displayItems = SlideChatManager.AvailableModels
            .Select(m => new ModelDisplayItem(m.ModelDefinition.Provider, m.ModelDefinition.ModelName))
            .ToList();
        AvailableModelItems = new ObservableCollection<ModelDisplayItem>(displayItems);

        var currentModel = slideChatManager.CurrentModel;
        _selectedModelItem = new ModelDisplayItem(
            currentModel.ModelDefinition.Provider,
            currentModel.ModelDefinition.ModelName);

        _sendMessageCommand = new DelegateCommand(() => _ = RunSendMessageAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(InputText));
        _attachImageCommand = new DelegateCommand(() => { }, () => !IsBusy);
        _evaluateCommand = new DelegateCommand(() => _ = RunEvaluateAsync(), () => !IsBusy && slideChatManager.LastEvaluationResult is null && !string.IsNullOrWhiteSpace(_lastUserPrompt));
        _evaluatePromptCommand = new DelegateCommand(() => _ = RunEvaluatePromptAsync(), () => !IsBusy);
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
            case nameof(SlideChatManager.LastEvaluationResult):
            case nameof(SlideChatManager.LastPromptEvaluationResult):
                OnPropertyChanged(e.PropertyName!);
                break;
        }
    }

    private void OnEvaluationCompleted(object? sender, SlideEvaluationResult result)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            EvaluationSummaryText = $"评估完成 | 综合评分: {result.OverallScore:F1}/10";
            _evaluateCommand.RaiseCanExecuteChanged();
        });
    }

    private void OnPromptEvaluationCompleted(object? sender, PromptEvaluationResult result)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _evaluatePromptCommand.RaiseCanExecuteChanged();
        });
    }

    /// <summary>
    /// SlideML 聊天管理器，暴露给界面直接绑定。
    /// </summary>
    public SlideChatManager SlideChatManager { get; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// 手动触发 SlideML 评估命令。
    /// </summary>
    public ICommand EvaluateCommand => _evaluateCommand;

    /// <summary>
    /// 手动触发提示词评估命令。
    /// </summary>
    public ICommand EvaluatePromptCommand => _evaluatePromptCommand;

    /// <summary>
    /// 附加图片命令。
    /// </summary>
    public ICommand AttachImageCommand => _attachImageCommand;

    /// <summary>
    /// 用户手动选择的待发送图片文件列表。
    /// </summary>
    public ObservableCollection<FileInfo> AttachedImageFiles { get; } = new();

    /// <summary>
    /// 可用的模型选项列表，用于 ComboBox 绑定。
    /// </summary>
    public ObservableCollection<ModelDisplayItem> AvailableModelItems { get; }

    /// <summary>
    /// 当前选中的模型选项。设置时触发模型切换。
    /// </summary>
    public ModelDisplayItem SelectedModelItem
    {
        get => _selectedModelItem;
        set
        {
            if (SetProperty(ref _selectedModelItem, value) && value is not null)
            {
                SlideChatManager.SetModel(value.ModelName, string.IsNullOrEmpty(value.Provider) ? null : value.Provider);
            }
        }
    }

    /// <summary>
    /// 聊天气泡消息列表。
    /// </summary>
#pragma warning disable CS0618 // 类型或成员已过时
    public ObservableCollection<CopilotChatMessage> ChatMessages => SlideChatManager.ChatManager.ChatMessages;
#pragma warning restore CS0618

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _sendMessageCommand.RaiseCanExecuteChanged();
                _evaluateCommand.RaiseCanExecuteChanged();
                _evaluatePromptCommand.RaiseCanExecuteChanged();
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
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// 评估摘要文本，在评估完成后显示综合评分。
    /// </summary>
    public string EvaluationSummaryText
    {
        get => _evaluationSummaryText;
        set => SetProperty(ref _evaluationSummaryText, value);
    }

    /// <summary>
    /// 是否在发送消息时附加当前渲染预览图。
    /// </summary>
    public bool AttachPreview
    {
        get => _attachPreview;
        set => SetProperty(ref _attachPreview, value);
    }

    /// <summary>
    /// 供 View 层调用，将选中的图片文件路径加入附件列表。
    /// </summary>
    public void AddAttachedImageFiles(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                AttachedImageFiles.Add(new FileInfo(filePath));
            }
        }
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
        EvaluationSummaryText = string.Empty;
        _lastUserPrompt = message;

        var isFirstMessage = _isFirstMessage;
        if (_isFirstMessage)
        {
            _isFirstMessage = false;
        }

        var attachPreview = _attachPreview;

        var imageFiles = AttachedImageFiles.Count > 0
            ? AttachedImageFiles.Select(f => f.FullName).ToList()
            : null;

        AttachedImageFiles.Clear();

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await SlideChatManager.SendMessageAsync(message, isFirstMessage, attachPreview, imageFiles, cancellationTokenSource.Token).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusText = "完成";
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "操作已取消。";
        }
        catch (Exception)
        {
            StatusText = "执行失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunEvaluateAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastUserPrompt) || IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在评估 SlideML...";
        try
        {
            await SlideChatManager.EvaluateAsync(_lastUserPrompt).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusText = "评估完成";
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "评估已取消。";
        }
        catch (Exception)
        {
            StatusText = "评估失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunEvaluatePromptAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在评估提示词...";
        try
        {
            await SlideChatManager.EvaluatePromptAsync().ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusText = "提示词评估完成";
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "评估已取消。";
        }
        catch (Exception)
        {
            StatusText = "提示词评估失败";
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
