using AgentLib;

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// AI 聊天管理器。新代码应通过 <see cref="Pipeline"/> 使用。
    /// </summary>
    [Obsolete("请通过 Pipeline.ChatManager 使用。")]
    public CopilotChatManager ChatManager => _pipeline.ChatManager;

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

    /// <summary>
    /// 构建 SlideML 排版引擎系统提示词。
    /// </summary>
    [Obsolete("请使用 IPromptProvider.BuildSystemPrompt() 替代。")]
    public static string BuildSystemPrompt()
    {
        return new SlideMlPromptProvider().BuildSystemPrompt();
    }

    /// <summary>
    /// 构建初始用户提示词，包裹用户的自然语言需求。
    /// </summary>
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
