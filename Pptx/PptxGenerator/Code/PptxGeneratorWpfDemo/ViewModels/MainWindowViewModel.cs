using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using Microsoft.Extensions.AI;
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
using System.Windows;
using System.Windows.Input;
using PptxGenerator.Evaluation;

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
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly AsyncDelegateCommand _sendMessageCommand;
    private readonly AsyncDelegateCommand _attachImageCommand;
    private readonly AsyncDelegateCommand _evaluateCommand;
    private readonly AsyncDelegateCommand _evaluatePromptCommand;
    private readonly AsyncDelegateCommand _rerenderCommand;
    private readonly AsyncDelegateCommand<CopilotChatMessage> _restartFromMessageCommand;
    private bool _isBusy;
    private bool _isIterating;
    private bool _isFirstMessage = true;
    private bool _attachPreview;
    private string _inputText = "请发挥你的想象力，制作一个精美的页面介绍 SlideML —— 一种用 XML 描述幻灯片排版的标记语言，支持 Page、Panel、Rect、TextElement、Image 等标签在 1280×720 画布上自由布局。";
    private string _statusText = "等待开始";
    private string _iterationStatusText = string.Empty;
    private string _evaluationSummaryText = string.Empty;
    private string _lastUserPrompt = string.Empty;
    private string _editableSlideXml = string.Empty;
    private ModelDisplayItem _selectedModelItem;
    private bool _isStreamingMode = true;
    private string _mcpServiceUrl = "http://127.0.0.1:64773/mcp";

    public MainWindowViewModel(
        SlideChatManager slideChatManager,
        IMainThreadDispatcher? dispatcher = null)
    {
        SlideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
        _dispatcher = dispatcher ?? WpfDispatcher.Instance;
        SlideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;

        var displayItems = SlideChatManager.AvailableModels
            .Select(m => new ModelDisplayItem(m.ModelDefinition.Provider, m.ModelDefinition.ModelName))
            .ToList();
        AvailableModelItems = new ObservableCollection<ModelDisplayItem>(displayItems);

        var currentModel = slideChatManager.CurrentModel;
        _selectedModelItem = new ModelDisplayItem(
            currentModel.ModelDefinition.Provider,
            currentModel.ModelDefinition.ModelName);

        _sendMessageCommand = new AsyncDelegateCommand(RunSendMessageAsync, HandleUnexpectedCommandException, () => !IsBusy && !string.IsNullOrWhiteSpace(InputText));
        _attachImageCommand = new AsyncDelegateCommand(() => Task.CompletedTask, HandleUnexpectedCommandException, () => !IsBusy);
        _evaluateCommand = new AsyncDelegateCommand(RunEvaluateAsync, HandleUnexpectedCommandException, () => !IsBusy && slideChatManager.LastEvaluationResult is null && !string.IsNullOrWhiteSpace(_lastUserPrompt));
        _evaluatePromptCommand = new AsyncDelegateCommand(RunEvaluatePromptAsync, HandleUnexpectedCommandException, () => !IsBusy && !IsIterating && slideChatManager.Pipeline.CanRunIteration);
        _rerenderCommand = new AsyncDelegateCommand(RunRerenderAsync, HandleUnexpectedCommandException, () => !IsBusy && !string.IsNullOrWhiteSpace(_editableSlideXml));
        _restartFromMessageCommand = new AsyncDelegateCommand<CopilotChatMessage>(RunRestartFromMessageAsync, HandleUnexpectedCommandException, CanRestartFromMessage);
    }

    /// <summary>
    /// 订阅 SlideChatManager 的属性变更，在工具调用过程中即可实时刷新 UI 绑定。
    /// </summary>
    private void OnSlideChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        VerifyAccess();
        ApplySlideChatManagerPropertyChange(e.PropertyName);
    }

    private void ApplySlideChatManagerPropertyChange(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(SlideChatManager.PreviewImage):
            case nameof(SlideChatManager.RenderedXml):
            case nameof(SlideChatManager.WarningText):
                OnPropertyChanged(propertyName);
                break;
            case nameof(SlideChatManager.LastEvaluationResult):
                OnPropertyChanged(propertyName);
                if (SlideChatManager.LastEvaluationResult is { } evaluationResult)
                {
                    EvaluationSummaryText = $"评估完成 | 综合评分: {evaluationResult.OverallScore:F1}/10";
                }

                _evaluateCommand.RaiseCanExecuteChanged();
                break;
            case nameof(SlideChatManager.LastPromptEvaluationResult):
                OnPropertyChanged(propertyName);
                _evaluatePromptCommand.RaiseCanExecuteChanged();
                break;
            case nameof(SlideChatManager.CurrentSlideXml):
                _editableSlideXml = SlideChatManager.CurrentSlideXml;
                OnPropertyChanged(nameof(EditableSlideXml));
                OnPropertyChanged(nameof(SlideChatManager.CurrentSlideXml));
                break;
        }
    }

    /// <summary>
    /// SlideML 聊天管理器，暴露给界面直接绑定。
    /// </summary>
    public SlideChatManager SlideChatManager { get; }

    /// <summary>
    /// 聊天会话管理器，供界面直接绑定聊天消息集合。
    /// </summary>
    public CopilotChatManager CopilotChatManager => SlideChatManager.Pipeline.ChatManager;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// 从指定用户消息重新开始流式生成命令。
    /// </summary>
    public ICommand RestartFromMessageCommand => _restartFromMessageCommand;

    /// <summary>
    /// 手动触发 SlideML 评估命令。
    /// </summary>
    public ICommand EvaluateCommand => _evaluateCommand;

    /// <summary>
    /// 重新渲染当前编辑的 SlideML 命令。
    /// </summary>
    public ICommand RerenderCommand => _rerenderCommand;

    /// <summary>
    /// 可编辑的 SlideML XML 文本，供用户手动修改后重新渲染。
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
    /// 提示词迭代优化命令（改造原提示词评估按钮）。
    /// </summary>
    public ICommand EvaluatePromptCommand => _evaluatePromptCommand;

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
                _restartFromMessageCommand.RaiseCanExecuteChanged();
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
    /// 是否使用流式模式发送消息。
    /// </summary>
    public bool IsStreamingMode
    {
        get => _isStreamingMode;
        set
        {
            if (SetProperty(ref _isStreamingMode, value))
            {
                _restartFromMessageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 外部 MCP 服务地址。为空时不使用 MCP 渲染。
    /// </summary>
    public string McpServiceUrl
    {
        get => _mcpServiceUrl;
        set => SetProperty(ref _mcpServiceUrl, value);
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
        VerifyAccess();
        if (IsBusy || string.IsNullOrWhiteSpace(InputText))
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
        _isFirstMessage = false;
        var attachPreview = _attachPreview;
        var isStreamingMode = _isStreamingMode;
        var imageFiles = AttachedImageFiles.Count > 0
            ? AttachedImageFiles.Select(f => f.FullName).ToList()
            : null;
        AttachedImageFiles.Clear();

        string finalStatus;
        try
        {
            await SlideChatManager.SendMessageAsync(
                message,
                isFirstMessage,
                attachPreview,
                imageFiles,
                useStreaming: isStreamingMode);
            finalStatus = "完成";
        }
        catch (OperationCanceledException)
        {
            finalStatus = "操作已取消。";
        }
        catch (Exception)
        {
            finalStatus = "执行失败";
        }

        StatusText = finalStatus;
        IsBusy = false;
    }

    private async Task RunRestartFromMessageAsync(CopilotChatMessage? message)
    {
        VerifyAccess();
        if (!CanRestartFromMessage(message))
        {
            return;
        }

        var targetMessage = message!;
        IsBusy = true;
        StatusText = "正在重新开始生成...";
        EvaluationSummaryText = string.Empty;
        _lastUserPrompt = targetMessage.Content;

        var completed = false;
        string finalStatus;
        try
        {
            await SlideChatManager.RestartFromMessageAsync(targetMessage);
            completed = true;
            finalStatus = "重新生成完成";
        }
        catch (OperationCanceledException)
        {
            finalStatus = "重新生成已取消。";
        }
        catch (Exception)
        {
            finalStatus = "重新生成失败";
        }

        if (completed)
        {
            _isFirstMessage = false;
        }

        StatusText = finalStatus;
        IsBusy = false;
    }

    private bool CanRestartFromMessage(CopilotChatMessage? message)
    {
        return message is { IsPresetInfo: false }
            && message.Role == ChatRole.User
            && !IsBusy
            && IsStreamingMode;
    }

    private async Task RunEvaluateAsync()
    {
        VerifyAccess();
        if (string.IsNullOrWhiteSpace(_lastUserPrompt) || IsBusy)
        {
            return;
        }

        var userPrompt = _lastUserPrompt;
        IsBusy = true;
        StatusText = "正在评估 SlideML...";

        string finalStatus;
        try
        {
            await SlideChatManager.EvaluateAsync(userPrompt);
            finalStatus = "评估完成";
        }
        catch (OperationCanceledException)
        {
            finalStatus = "评估已取消。";
        }
        catch (Exception)
        {
            finalStatus = "评估失败";
        }

        StatusText = finalStatus;
        IsBusy = false;
    }

    private async Task RunEvaluatePromptAsync()
    {
        VerifyAccess();
        var useSinglePromptEvaluation = false;
        string? userPrompt = null;
        FileInfo? originalScreenshotFile = null;

        if (IsBusy || IsIterating)
        {
            return;
        }

        if (!SlideChatManager.Pipeline.CanRunIteration)
        {
            useSinglePromptEvaluation = true;
        }
        else if (string.IsNullOrWhiteSpace(_lastUserPrompt))
        {
            StatusText = "请先生成 SlideML 页面。";
            return;
        }
        else
        {
            userPrompt = _lastUserPrompt;
            originalScreenshotFile = AttachedOriginalScreenshot.FirstOrDefault();
            IsBusy = true;
            IsIterating = true;
            IterationStatusText = "正在运行提示词迭代优化...";
            StatusText = "提示词迭代优化中...";
        }

        if (useSinglePromptEvaluation)
        {
            await RunSinglePromptEvaluationAsync();
            return;
        }

        if (userPrompt is null)
        {
            return;
        }

        IterationResult? iterationResult = null;
        string finalStatus;
        try
        {
            IPreviewImage? originalScreenshot = originalScreenshotFile is null
                ? null
                : new FilePreviewImage(originalScreenshotFile);

            iterationResult = await SlideChatManager.RunPromptIterationAsync(
                userPrompt,
                originalScreenshot,
                cancellationToken: CancellationToken.None);
            finalStatus = iterationResult is null
                ? "迭代不可用"
                : iterationResult.IsConverged ? "迭代收敛" : "迭代完成";
        }
        catch (OperationCanceledException)
        {
            finalStatus = "迭代已取消。";
        }
        catch (Exception)
        {
            finalStatus = "迭代失败";
        }

        StatusText = finalStatus;
        if (iterationResult is not null)
        {
            IterationStatusText = iterationResult.IsConverged
                ? $"✅ 迭代收敛 | {iterationResult.TotalRounds} 轮 | 最终评分: {iterationResult.FinalScore:F1}/10"
                : $"⏹ 迭代完成 | {iterationResult.TotalRounds} 轮 | 最终评分: {iterationResult.FinalScore:F1}/10";
        }

        IsBusy = false;
        IsIterating = false;
    }

    private async Task RunSinglePromptEvaluationAsync()
    {
        VerifyAccess();
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在评估提示词...";

        string finalStatus;
        try
        {
            await SlideChatManager.EvaluatePromptAsync();
            finalStatus = "提示词评估完成";
        }
        catch (OperationCanceledException)
        {
            finalStatus = "评估已取消。";
        }
        catch (Exception)
        {
            finalStatus = "提示词评估失败";
        }

        StatusText = finalStatus;
        IsBusy = false;
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

    /// <summary>
    /// 使用当前编辑的 SlideML XML 重新渲染页面预览。
    /// </summary>
    private async Task RunRerenderAsync()
    {
        VerifyAccess();
        if (IsBusy || string.IsNullOrWhiteSpace(_editableSlideXml))
        {
            return;
        }

        var slideXml = _editableSlideXml;
        IsBusy = true;
        StatusText = "正在重新渲染...";

        string finalStatus;
        try
        {
            var renderTool = SlideChatManager.SlideMlRenderTool;
            var renderResult = await renderTool.RenderPipeline
                .RenderAsync(slideXml);

            await renderTool.ApplyRenderResultAsync(renderResult);
            finalStatus = "重新渲染完成";
        }
        catch (OperationCanceledException)
        {
            finalStatus = "重新渲染已取消。";
        }
        catch (Exception)
        {
            finalStatus = "重新渲染失败";
        }

        StatusText = finalStatus;
        IsBusy = false;
    }

    /// <summary>
    /// 尝试连接 MCP 服务并切换到 MCP 渲染管道。
    /// 当 MCP 服务不可用或无渲染工具时，保持使用本地渲染引擎。
    /// 供界面输入框失焦时调用。
    /// </summary>
    public async Task TryConnectMcpRenderAsync()
    {
        VerifyAccess();
        var pipeline = SlideChatManager.SlideMlRenderTool.RenderPipeline as SwitchableSlideMlRenderPipeline;
        var serviceUrl = McpServiceUrl;

        if (pipeline is null)
        {
            return;
        }

        await pipeline.TryEnableMcpAsync(serviceUrl);
    }

    private void HandleUnexpectedCommandException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        StatusText = "执行失败";
    }

    private void VerifyAccess()
    {
        if (!_dispatcher.CheckAccess())
        {
            throw new InvalidOperationException("主窗口 ViewModel 只能由所属的 UI Dispatcher 调用。");
        }
    }
}