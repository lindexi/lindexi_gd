using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using PptxGenerator.Evaluation;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly DelegateCommand _rerenderCommand;
    private bool _isBusy;
    private bool _isIterating;
    private bool _isFirstMessage = true;
    private bool _attachPreview;
    private string _editableSlideXml = string.Empty;
    private string _mcpServiceUrl = "http://127.0.0.1:64773/mcp";
    private string _inputText = "请发挥你的想象力，制作一个精美的页面介绍 SlideML —— 一种用 XML 描述幻灯片排版的标记语言，支持 Page、Panel、Rect、TextElement、Image 等标签在 1280×720 画布上自由布局。";
    private string _statusText = "等待开始";
    private string _iterationStatusText = string.Empty;
    private string _evaluationSummaryText = string.Empty;
    private string _lastUserPrompt = string.Empty;
    private ModelDisplayItem _selectedModelItem;
    private bool _isStreamingMode = true;

    public MainWindowViewModel(SlideChatManager slideChatManager)
    {
        SlideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
        SlideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;

        var pipeline = slideChatManager.Pipeline;
        pipeline.ChatManager.PropertyChanged += OnCopilotChatManagerPropertyChanged;
        pipeline.EvaluationCompleted += OnEvaluationCompleted;
        pipeline.PromptEvaluationCompleted += OnPromptEvaluationCompleted;
        pipeline.IterationCompleted += OnIterationCompleted;

        var displayItems = SlideChatManager.AvailableModels
            .Select(m => new ModelDisplayItem(m.ModelDefinition.Provider, m.ModelDefinition.ModelName))
            .ToList();
        AvailableModelItems = new ObservableCollection<ModelDisplayItem>(displayItems);

        var currentModel = slideChatManager.CurrentModel;
        _selectedModelItem = new ModelDisplayItem(
            currentModel.ModelDefinition.Provider,
            currentModel.ModelDefinition.ModelName);

        _sendMessageCommand = new DelegateCommand(() => _ = RunSendMessageAsync(), AvaloniaDispatcher.Instance, () => !IsBusy && !string.IsNullOrWhiteSpace(InputText));
        _attachImageCommand = new DelegateCommand(() => { }, AvaloniaDispatcher.Instance, () => !IsBusy);
        _evaluateCommand = new DelegateCommand(() => _ = RunEvaluateAsync(), AvaloniaDispatcher.Instance, () => !IsBusy && slideChatManager.LastEvaluationResult is null && !string.IsNullOrWhiteSpace(_lastUserPrompt));
        _evaluatePromptCommand = new DelegateCommand(() => _ = RunEvaluatePromptAsync(), AvaloniaDispatcher.Instance, () => !IsBusy && !IsIterating && slideChatManager.Pipeline.CanRunIteration);
        _rerenderCommand = new DelegateCommand(() => _ = RunRerenderAsync(), AvaloniaDispatcher.Instance, () => !IsBusy && !string.IsNullOrWhiteSpace(EditableSlideXml));

        _ = UseMcpSlideMlRender();
    }

    /// <summary>
    /// 订阅 SlideChatManager 的属性变更，在工具调用过程中即可实时刷新 UI 绑定。
    /// </summary>
    private void OnSlideChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SlideChatManager.PreviewImage):
            case nameof(SlideChatManager.CurrentSlideXml):
                EditableSlideXml = SlideChatManager.CurrentSlideXml;
                OnPropertyChanged(e.PropertyName!);
                break;
            case nameof(SlideChatManager.RenderedXml):
            case nameof(SlideChatManager.WarningText):
            case nameof(SlideChatManager.LastEvaluationResult):
            case nameof(SlideChatManager.LastPromptEvaluationResult):
                OnPropertyChanged(e.PropertyName!);
                break;
        }
    }

    private void OnCopilotChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AgentLib.CopilotChatManager.ChatMessages):
            case nameof(AgentLib.CopilotChatManager.CurrentSessionId):
                OnPropertyChanged(nameof(ChatMessages));
                break;
        }
    }

    private void OnEvaluationCompleted(object? sender, SlideEvaluationResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            EvaluationSummaryText = $"评估完成 | 综合评分: {result.OverallScore:F1}/10";
            _evaluateCommand.RaiseCanExecuteChanged();
        });
    }

    private void OnPromptEvaluationCompleted(object? sender, PromptEvaluationResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _evaluatePromptCommand.RaiseCanExecuteChanged();
        });
    }

    private void OnIterationCompleted(object? sender, IterationResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsIterating = false;
            IterationStatusText = result.IsConverged
                ? $"✅ 迭代收敛 | {result.TotalRounds} 轮 | 最终评分: {result.FinalScore:F1}/10"
                : $"⏹ 迭代完成 | {result.TotalRounds} 轮 | 最终评分: {result.FinalScore:F1}/10";
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
    /// 提示词迭代优化命令（改造原提示词评估按钮）。
    /// </summary>
    public ICommand EvaluatePromptCommand => _evaluatePromptCommand;

    /// <summary>
    /// 使用当前编辑的 SlideML 重新渲染页面预览的命令。
    /// </summary>
    public ICommand RerenderCommand => _rerenderCommand;

    /// <summary>
    /// 是否正在运行迭代闭环。
    /// </summary>
    public bool IsIterating
    {
        get => _isIterating;
        private set
        {
            if (SetProperty(ref _isIterating, value))
            {
                _evaluatePromptCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 迭代状态文本，显示当前迭代进度。
    /// </summary>
    public string IterationStatusText
    {
        get => _iterationStatusText;
        set => SetProperty(ref _iterationStatusText, value);
    }

    /// <summary>
    /// 用户提供的原始 PPT 截图文件列表，用于还原度评估对比。
    /// </summary>
    public ObservableCollection<FileInfo> AttachedOriginalScreenshot { get; } = new();

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
    public ObservableCollection<CopilotChatMessage> ChatMessages => SlideChatManager.Pipeline.ChatManager.ChatMessages;

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
                _rerenderCommand.RaiseCanExecuteChanged();
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
    /// 用户可编辑的当前 SlideML XML。
    /// </summary>
    public string EditableSlideXml
    {
        get => _editableSlideXml;
        set
        {
            if (SetProperty(ref _editableSlideXml, value))
            {
                _rerenderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// MCP 渲染服务地址。
    /// </summary>
    public string McpServiceUrl
    {
        get => _mcpServiceUrl;
        set => SetProperty(ref _mcpServiceUrl, value);
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
    /// 是否使用流式模式发送消息。
    /// </summary>
    public bool IsStreamingMode
    {
        get => _isStreamingMode;
        set => SetProperty(ref _isStreamingMode, value);
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
            await SlideChatManager.SendMessageAsync(message, isFirstMessage, attachPreview, imageFiles, useStreaming: _isStreamingMode, cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
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

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
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
        if (IsBusy || IsIterating)
        {
            return;
        }

        if (!SlideChatManager.Pipeline.CanRunIteration)
        {
            // 降级为旧版单次提示词评估
            await RunSinglePromptEvaluationAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(_lastUserPrompt))
        {
            StatusText = "请先生成 SlideML 页面。";
            return;
        }

        IsBusy = true;
        IsIterating = true;
        IterationStatusText = "正在运行提示词迭代优化...";
        StatusText = "提示词迭代优化中...";

        try
        {
            // 获取原始截图（如果附件中有图片）
            IPreviewImage? originalScreenshot = null;
            if (AttachedOriginalScreenshot.Count > 0)
            {
                originalScreenshot = new FilePreviewImage(AttachedOriginalScreenshot[0]);
            }

            var result = await SlideChatManager.RunPromptIterationAsync(
                _lastUserPrompt,
                originalScreenshot,
                cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result is not null)
                {
                    StatusText = result.IsConverged ? "迭代收敛" : "迭代完成";
                }
                else
                {
                    StatusText = "迭代不可用";
                }
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "迭代已取消。";
        }
        catch (Exception)
        {
            StatusText = "迭代失败";
        }
        finally
        {
            IsBusy = false;
            IsIterating = false;
        }
    }

    private async Task RunSinglePromptEvaluationAsync()
    {
        IsBusy = true;
        StatusText = "正在评估提示词...";
        try
        {
            await SlideChatManager.EvaluatePromptAsync().ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
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

    /// <summary>
    /// 尝试连接 MCP 服务并切换渲染管线。
    /// </summary>
    public async Task TryConnectMcpRenderAsync()
    {
        if (SlideChatManager.SlideMlRenderTool.RenderPipeline is not SwitchableSlideMlRenderPipeline pipeline)
        {
            return;
        }

        await pipeline.TryEnableMcpAsync(McpServiceUrl).ConfigureAwait(false);
    }

    private async Task RunRerenderAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(EditableSlideXml))
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在重新渲染...";
        try
        {
            var renderTool = SlideChatManager.SlideMlRenderTool;
            var renderResult = await renderTool.RenderPipeline
                .RenderAsync(EditableSlideXml)
                .ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                renderTool.ApplyRenderResult(renderResult);
                StatusText = "重新渲染完成";
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "重新渲染已取消。";
        }
        catch (Exception)
        {
            StatusText = "重新渲染失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UseMcpSlideMlRender()
    {
        await TryConnectMcpRenderAsync().ConfigureAwait(false);
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
