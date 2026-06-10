using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// SlideML 聊天管理器的兼容适配层。
/// 保留现有公开 API，内部委托给 <see cref="SlideGenerationPipeline"/>。
/// 新代码应直接使用 <see cref="SlideGenerationPipeline"/>。
/// </summary>
public sealed class SlideChatManager : INotifyPropertyChanged
{
    private readonly SlideGenerationPipeline _pipeline;
    private readonly AgentApiEndpointManager _endpointManager;

    /// <summary>
    /// 创建 <see cref="SlideChatManager"/>。
    /// </summary>
    /// <param name="copilotChatManager">AI 聊天管理器。</param>
    /// <param name="slideRenderTool">SlideML 渲染工具。</param>
    /// <param name="slideEvaluator">SlideML 评估器，为 <see langword="null"/> 时跳过评估。</param>
    /// <param name="promptEvaluator">提示词评估器，为 <see langword="null"/> 时跳过提示词评估。</param>
    public SlideChatManager(
        CopilotChatManager copilotChatManager,
        SlideRenderTool slideRenderTool,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null)
        : this(copilotChatManager, new SlideMlPromptProvider(), slideRenderTool, slideEvaluator, promptEvaluator)
    {
    }

    /// <summary>
    /// 创建 <see cref="SlideChatManager"/>（完整参数版本）。
    /// </summary>
    internal SlideChatManager(
        CopilotChatManager copilotChatManager,
        IPromptProvider promptProvider,
        SlideRenderTool slideRenderTool,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null)
    {
        _endpointManager = copilotChatManager.AgentApiEndpointManager;
        _pipeline = new SlideGenerationPipeline(copilotChatManager, promptProvider, slideRenderTool, slideEvaluator, promptEvaluator);
        _pipeline.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        _pipeline.SlideRendered += () =>
        {
            OnPropertyChanged(nameof(PreviewBitmap));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
        };
    }

    /// <summary>
    /// 获取当前可用的语言模型列表。用于 GUI 模型选择。
    /// </summary>
    public IReadOnlyList<ILanguageModel> AvailableModels => _endpointManager.GetSupportedModels();

    /// <summary>
    /// 当前选中的主模型。
    /// </summary>
    public ILanguageModel CurrentModel => _endpointManager.PrimaryModel;

    /// <summary>
    /// 当前选中的模型显示名（用于 ComboBox 绑定）。
    /// </summary>
    public string SelectedModelDisplayName => _endpointManager.PrimaryModel.ModelDefinition.ModelName;

    /// <summary>
    /// 切换到指定名称的模型。切换后评估器、生成器均使用新模型。
    /// </summary>
    /// <param name="modelName">模型名称，对应 <see cref="ModelDefinition.ModelName"/>。</param>
    /// <param name="provider">可选的提供商名称。同一模型出现在多个供应商时用于区分。</param>
    /// <returns>切换成功返回 <see langword="true"/>；未找到模型返回 <see langword="false"/>。</returns>
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

    /// <summary>
    /// 底层管道编排器。推荐新代码直接使用此属性。
    /// </summary>
    public SlideGenerationPipeline Pipeline => _pipeline;

    /// <summary>
    /// SlideML 渲染工具。
    /// </summary>
    public SlideRenderTool SlideRenderTool => _pipeline.SlideRenderTool;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当前预览 Bitmap。
    /// </summary>
    public BitmapSource? PreviewBitmap => _pipeline.PreviewBitmap;

    /// <summary>
    /// 最近一次渲染的 SlideML XML。
    /// </summary>
    public string CurrentSlideXml => _pipeline.CurrentSlideXml;

    /// <summary>
    /// 最近一次渲染后回填的 XML。
    /// </summary>
    public string RenderedXml => _pipeline.RenderedXml;

    /// <summary>
    /// 最近一次渲染的警告列表。
    /// </summary>
    public string WarningText => _pipeline.WarningText;

    /// <summary>
    /// 最近一次 SlideML 评估结果。
    /// </summary>
    public SlideEvaluationResult? LastEvaluationResult => _pipeline.LastSlideEvaluation;

    /// <summary>
    /// 最近一次提示词评估结果。
    /// </summary>
    public PromptEvaluationResult? LastPromptEvaluationResult => _pipeline.LastPromptEvaluation;

    /// <summary>
    /// 发送 SlideML 生成请求。
    /// </summary>
    /// <param name="userPrompt">用户自然语言需求描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(userPrompt, isFirstMessage: true, attachPreview: false, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 统一发送消息入口。
    /// </summary>
    /// <param name="userMessage">用户输入文本。</param>
    /// <param name="isFirstMessage">是否为首条消息。</param>
    /// <param name="attachPreview">是否附加当前渲染预览图。</param>
    /// <param name="attachedImageFiles">用户手动选择的图片文件路径列表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendMessageAsync(string userMessage, bool isFirstMessage, bool attachPreview, IReadOnlyList<string>? attachedImageFiles = null, CancellationToken cancellationToken = default)
    {
        await _pipeline.SendMessageAsync(userMessage, isFirstMessage, attachPreview, attachedImageFiles, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 手动触发 SlideML 评估。
    /// </summary>
    public Task<SlideEvaluationResult?> EvaluateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        return _pipeline.EvaluateAsync(userPrompt, cancellationToken);
    }

    /// <summary>
    /// 手动触发提示词评估。
    /// </summary>
    public Task<PromptEvaluationResult?> EvaluatePromptAsync(CancellationToken cancellationToken = default)
    {
        return _pipeline.EvaluatePromptAsync(cancellationToken);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
