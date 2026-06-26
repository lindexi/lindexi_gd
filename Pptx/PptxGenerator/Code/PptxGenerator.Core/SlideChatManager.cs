using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PptxGenerator.Evaluation;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;

namespace PptxGenerator;

/// <summary>
/// SlideML 聊天管理器的兼容适配层。
/// 保留现有公开 API，内部委托给 <see cref="SlideGenerationPipeline"/>。
/// </summary>
public sealed class SlideChatManager : INotifyPropertyChanged
{
    private readonly AgentApiEndpointManager _endpointManager;
    private ILanguageModel? _currentModel;

    public SlideChatManager(
        CopilotChatManager copilotChatManager,
        SlideMlRenderTool slideMlRenderTool,
        ISlideMlPromptProvider? promptProvider = null,
        SlideDocumentContext? slideDocumentContext = null,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null,
        IPromptOptimizer? promptOptimizer = null)
    {
        ArgumentNullException.ThrowIfNull(copilotChatManager);
        _endpointManager = copilotChatManager.AgentApiEndpointManager;
        Pipeline = new SlideGenerationPipeline(copilotChatManager, promptProvider ?? new SlideMlPromptProvider(slideDocumentContext), slideMlRenderTool, slideEvaluator, promptEvaluator, promptOptimizer);
        AttachPipelineEvents();
    }

    /// <summary>
    /// 使用 <see cref="PipelineConfiguration"/> 创建实例。
    /// </summary>
    public SlideChatManager(
        CopilotChatManager copilotChatManager,
        SlideMlRenderTool slideMlRenderTool,
        PipelineConfiguration configuration,
        ISlideMlPromptProvider? promptProvider = null,
        SlideDocumentContext? slideDocumentContext = null)
    {
        ArgumentNullException.ThrowIfNull(copilotChatManager);
        _endpointManager = copilotChatManager.AgentApiEndpointManager;
        Pipeline = new SlideGenerationPipeline(copilotChatManager, promptProvider ?? new SlideMlPromptProvider(slideDocumentContext), slideMlRenderTool, configuration);
        AttachPipelineEvents();
    }

    /// <summary>
    /// 绑定 Pipeline 的事件到当前管理器的属性变更通知。
    /// </summary>
    private void AttachPipelineEvents()
    {
        Pipeline.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        Pipeline.SlideRendered += () =>
        {
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
        };
    }

    public IReadOnlyList<ILanguageModel> AvailableModels => _endpointManager.GetSupportedModels();

    public ILanguageModel CurrentModel
    {
        get => _currentModel ?? _endpointManager.PrimaryModel;
        init => _currentModel = value;
    }

    /// <summary>
    /// 选择的模型的显示名称，用于界面绑定
    /// </summary>
    public string SelectedModelDisplayName => (_currentModel ?? _endpointManager.PrimaryModel).ModelDefinition.ModelName;

    public bool SetModel(string modelName, string? provider = null)
    {
        ArgumentNullException.ThrowIfNull(modelName);

        var model = _endpointManager.GetModel(modelName, provider);
        if (model is null)
        {
            return false;
        }

        _endpointManager.PrimaryModel = model;
        if (_currentModel is not null)
        {
            _currentModel = model;
        }
        OnPropertyChanged(nameof(SelectedModelDisplayName));
        return true;
    }

    public SlideGenerationPipeline Pipeline { get; }

    public SlideMlRenderTool SlideMlRenderTool => Pipeline.SlideMlRenderTool;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IPreviewImage? PreviewImage => Pipeline.PreviewImage;

    public string CurrentSlideXml => Pipeline.CurrentSlideXml;

    public string RenderedXml => Pipeline.RenderedXml;

    public string WarningText => Pipeline.WarningText;

    public SlideEvaluationResult? LastEvaluationResult => Pipeline.LastSlideEvaluation;

    public PromptEvaluationResult? LastPromptEvaluationResult => Pipeline.LastPromptEvaluation;

    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(userPrompt, isFirstMessage: true, attachPreview: false, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(string userMessage, bool isFirstMessage, bool attachPreview, IReadOnlyList<string>? attachedImageFiles = null, bool createNewSession = false, bool skipAutoEvaluation = false, CancellationToken cancellationToken = default)
    {
        await Pipeline.SendMessageAsync(userMessage, isFirstMessage, attachPreview, attachedImageFiles, createNewSession: createNewSession, skipAutoEvaluation: skipAutoEvaluation, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task<SlideEvaluationResult?> EvaluateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        return Pipeline.EvaluateAsync(userPrompt, cancellationToken);
    }

    public Task<PromptEvaluationResult?> EvaluatePromptAsync(CancellationToken cancellationToken = default)
    {
        return Pipeline.EvaluatePromptAsync(cancellationToken);
    }

    /// <summary>
    /// 运行提示词迭代优化闭环。
    /// </summary>
    public Task<IterationResult?> RunPromptIterationAsync(
        string userPrompt,
        IPreviewImage? originalScreenshot,
        IterationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Pipeline.RunPromptIterationAsync(userPrompt, originalScreenshot, options, cancellationToken);
    }

    /// <summary>
    /// 迭代完成事件。
    /// </summary>
    public event EventHandler<IterationResult>? IterationCompleted
    {
        add => Pipeline.IterationCompleted += value;
        remove => Pipeline.IterationCompleted -= value;
    }

    /// <summary>
    /// 单轮迭代进度事件。
    /// </summary>
    public event EventHandler<IterationRound>? IterationProgress
    {
        add => Pipeline.IterationProgress += value;
        remove => Pipeline.IterationProgress -= value;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
