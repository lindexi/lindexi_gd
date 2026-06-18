using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// SlideML 聊天管理器的兼容适配层。
/// 保留现有公开 API，内部委托给 <see cref="SlideGenerationPipeline"/>。
/// </summary>
public sealed class SlideChatManager : INotifyPropertyChanged
{
    private readonly SlideGenerationPipeline _pipeline;
    private readonly AgentApiEndpointManager _endpointManager;

    public SlideChatManager(
        CopilotChatManager copilotChatManager,
        SlideRenderTool slideRenderTool,
        IDispatcher dispatcher,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null)
        : this(copilotChatManager, new SlideMlPromptProvider(), slideRenderTool, dispatcher, slideEvaluator, promptEvaluator)
    {
    }

    internal SlideChatManager(
        CopilotChatManager copilotChatManager,
        IPromptProvider promptProvider,
        SlideRenderTool slideRenderTool,
        IDispatcher dispatcher,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null)
    {
        _endpointManager = copilotChatManager.AgentApiEndpointManager;
        _pipeline = new SlideGenerationPipeline(copilotChatManager, promptProvider, slideRenderTool, dispatcher, slideEvaluator, promptEvaluator);
        _pipeline.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        _pipeline.SlideRendered += () =>
        {
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
        };
    }

    public IReadOnlyList<ILanguageModel> AvailableModels => _endpointManager.GetSupportedModels();

    public ILanguageModel CurrentModel => _endpointManager.PrimaryModel;

    public string SelectedModelDisplayName => _endpointManager.PrimaryModel.ModelDefinition.ModelName;

    public bool SetModel(string modelName, string? provider = null)
    {
        ArgumentNullException.ThrowIfNull(modelName);

        var model = _endpointManager.GetModel(modelName, provider);
        if (model is null)
        {
            return false;
        }

        _endpointManager.PrimaryModel = model;
        OnPropertyChanged(nameof(SelectedModelDisplayName));
        return true;
    }

    [Obsolete("请通过 Pipeline.ChatManager 使用。")]
    public CopilotChatManager ChatManager => _pipeline.ChatManager;

    public SlideGenerationPipeline Pipeline => _pipeline;

    public SlideRenderTool SlideRenderTool => _pipeline.SlideRenderTool;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IPreviewImage? PreviewImage => _pipeline.PreviewImage;

    public string CurrentSlideXml => _pipeline.CurrentSlideXml;

    public string RenderedXml => _pipeline.RenderedXml;

    public string WarningText => _pipeline.WarningText;

    public SlideEvaluationResult? LastEvaluationResult => _pipeline.LastSlideEvaluation;

    public PromptEvaluationResult? LastPromptEvaluationResult => _pipeline.LastPromptEvaluation;

    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(userPrompt, isFirstMessage: true, attachPreview: false, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(string userMessage, bool isFirstMessage, bool attachPreview, IReadOnlyList<string>? attachedImageFiles = null, CancellationToken cancellationToken = default)
    {
        await _pipeline.SendMessageAsync(userMessage, isFirstMessage, attachPreview, attachedImageFiles, cancellationToken).ConfigureAwait(false);
    }

    public Task<SlideEvaluationResult?> EvaluateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        return _pipeline.EvaluateAsync(userPrompt, cancellationToken);
    }

    public Task<PromptEvaluationResult?> EvaluatePromptAsync(CancellationToken cancellationToken = default)
    {
        return _pipeline.EvaluatePromptAsync(cancellationToken);
    }

    [Obsolete("请使用 IPromptProvider.BuildSystemPrompt() 替代。")]
    public static string BuildSystemPrompt()
    {
        return new SlideMlPromptProvider().BuildSystemPrompt();
    }

    [Obsolete("请使用 IPromptProvider.BuildInitialUserPrompt() 替代。")]
    public static string BuildInitialUserPrompt(string userPrompt)
    {
        return new SlideMlPromptProvider().BuildInitialUserPrompt(userPrompt);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}