using AgentLib;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// SlideML 生成管道编排器，管理 生成 → 渲染 → 评估 的完整生命周期。
/// 替代 <see cref="SlideChatManager"/> 的编排职责，提供事件驱动的评估反馈。
/// </summary>
public sealed class SlideGenerationPipeline : INotifyPropertyChanged
{
    private readonly CopilotChatManager _copilotChatManager;
    private readonly IPromptProvider _promptProvider;
    private readonly ISlideEvaluator? _slideEvaluator;
    private readonly IPromptEvaluator? _promptEvaluator;

    /// <summary>
    /// 初始化管道编排器。
    /// </summary>
    /// <param name="copilotChatManager">AI 聊天管理器。</param>
    /// <param name="promptProvider">提示词提供者。</param>
    /// <param name="slideRenderTool">SlideML 渲染工具。</param>
    /// <param name="slideEvaluator">SlideML 评估器，为 <see langword="null"/> 时跳过评估。</param>
    /// <param name="promptEvaluator">提示词评估器，为 <see langword="null"/> 时跳过提示词评估。</param>
    public SlideGenerationPipeline(
        CopilotChatManager copilotChatManager,
        IPromptProvider promptProvider,
        SlideRenderTool slideRenderTool,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        SlideRenderTool = slideRenderTool ?? throw new ArgumentNullException(nameof(slideRenderTool));
        _slideEvaluator = slideEvaluator;
        _promptEvaluator = promptEvaluator;

        SlideRenderTool.SlideRendered += OnSlideRendered;
    }

    /// <summary>
    /// SlideML 渲染工具。
    /// </summary>
    public SlideRenderTool SlideRenderTool { get; }

    /// <summary>
    /// AI 聊天管理器。
    /// </summary>
    public CopilotChatManager ChatManager => _copilotChatManager;

    /// <summary>
    /// 当前预览 Bitmap。
    /// </summary>
    public BitmapSource? PreviewBitmap => SlideRenderTool.LatestPreviewBitmap;

    /// <summary>
    /// 最近一次渲染的 SlideML XML。
    /// </summary>
    public string CurrentSlideXml => SlideRenderTool.LatestSlideXml;

    /// <summary>
    /// 最近一次渲染后回填的 XML。
    /// </summary>
    public string RenderedXml => SlideRenderTool.LatestRenderedXml;

    /// <summary>
    /// 最近一次渲染的警告列表。
    /// </summary>
    public string WarningText => SlideRenderTool.LatestWarnings;

    private SlideEvaluationResult? _lastSlideEvaluation;
    /// <summary>
    /// 最近一次 SlideML 评估结果。
    /// </summary>
    public SlideEvaluationResult? LastSlideEvaluation
    {
        get => _lastSlideEvaluation;
        private set
        {
            _lastSlideEvaluation = value;
            OnPropertyChanged(nameof(LastSlideEvaluation));
        }
    }

    private PromptEvaluationResult? _lastPromptEvaluation;
    /// <summary>
    /// 最近一次提示词评估结果。
    /// </summary>
    public PromptEvaluationResult? LastPromptEvaluation
    {
        get => _lastPromptEvaluation;
        private set
        {
            _lastPromptEvaluation = value;
            OnPropertyChanged(nameof(LastPromptEvaluation));
        }
    }

    private bool _isEvaluating;
    /// <summary>
    /// 是否正在执行评估。
    /// </summary>
    public bool IsEvaluating
    {
        get => _isEvaluating;
        private set
        {
            _isEvaluating = value;
            OnPropertyChanged(nameof(IsEvaluating));
        }
    }

    /// <summary>
    /// 是否已配置评估器。
    /// </summary>
    public bool CanEvaluate => _slideEvaluator is not null;

    /// <summary>
    /// 是否已配置提示词评估器。
    /// </summary>
    public bool CanEvaluatePrompt => _promptEvaluator is not null;

    /// <summary>
    /// SlideML 评估完成后触发。
    /// </summary>
    public event EventHandler<SlideEvaluationResult>? EvaluationCompleted;

    /// <summary>
    /// 提示词评估完成后触发。
    /// </summary>
    public event EventHandler<PromptEvaluationResult>? PromptEvaluationCompleted;

    /// <summary>
    /// 渲染完成后触发（转发自 <see cref="SlideRenderTool.SlideRendered"/>）。
    /// </summary>
    public event Action? SlideRendered;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 发送 SlideML 生成请求。等效于 <see cref="SendMessageAsync"/> 的首条消息模式。
    /// </summary>
    /// <param name="userPrompt">用户自然语言需求描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(userPrompt, isFirstMessage: true, attachPreview: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 统一发送消息入口。
    /// 首条消息会包裹初始提示词，后续消息以纯文本发送。
    /// </summary>
    /// <param name="userMessage">用户输入文本。</param>
    /// <param name="isFirstMessage">是否为首条消息。</param>
    /// <param name="attachPreview">是否附加当前渲染预览图。</param>
    /// <param name="attachedImageFiles">用户手动选择的图片文件路径列表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendMessageAsync(
        string userMessage,
        bool isFirstMessage,
        bool attachPreview,
        IReadOnlyList<string>? attachedImageFiles = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        var tools = new[] { SlideRenderTool.CreateTool(), SlideRenderTool.CreatePreviewTool() };

        var processedText = isFirstMessage
            ? _promptProvider.BuildInitialUserPrompt(userMessage)
            : userMessage;
        var systemPrompt = isFirstMessage ? _promptProvider.BuildSystemPrompt() : null;

        var initialCapacity = 1 + (attachedImageFiles?.Count ?? 0) + (attachPreview ? 1 : 0);
        var contents = new List<AIContent>(initialCapacity) { new TextContent(processedText) };

        if (attachedImageFiles is { Count: > 0 })
        {
            foreach (var imageFile in attachedImageFiles)
            {
                if (!string.IsNullOrWhiteSpace(imageFile) && File.Exists(imageFile))
                {
                    var dataContent = await DataContent.LoadFromAsync(imageFile, cancellationToken: cancellationToken);
                    contents.Add(dataContent);
                }
            }
        }

        if (attachPreview)
        {
            var previewDataContent = await SlideRenderTool.CreatePreviewDataContentAsync(cancellationToken);
            if (previewDataContent is not null)
            {
                contents.Add(previewDataContent);
            }
        }

        var request = new SendMessageRequest(contents)
        {
            WithHistory = true,
            CreateNewSession = false,
            Tools = tools,
            SystemPrompt = systemPrompt,
            CancellationToken = cancellationToken,
        };

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask;

        bool doNotRender = string.IsNullOrEmpty(CurrentSlideXml);
        if (doNotRender)
        {
            var toolRequest = request with
            {
                Contents = [new TextContent("请调用 render_slide 工具进行渲染，根据渲染结果优化界面")],
                SystemPrompt = "**重要：生成 SlideML 后必须调用 render_slide 工具，不可跳过此步骤**",
            };
            await _copilotChatManager.SendMessage(toolRequest).RunTask;
        }

        OnPropertyChanged(nameof(PreviewBitmap));
        OnPropertyChanged(nameof(CurrentSlideXml));
        OnPropertyChanged(nameof(RenderedXml));
        OnPropertyChanged(nameof(WarningText));

        // 生成完成后自动触发评估
        if (_slideEvaluator is not null && !string.IsNullOrWhiteSpace(CurrentSlideXml))
        {
            var context = new PipelineContext { UserPrompt = userMessage };
            context.SnapshotFromRenderTool(SlideRenderTool);
            _ = EvaluateContextAsync(context, userMessage, cancellationToken);
        }
    }

    /// <summary>
    /// 手动触发 SlideML 评估（评估当前已生成的 SlideML）。
    /// </summary>
    /// <param name="userPrompt">用户原始需求文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>评估结果。</returns>
    public async Task<SlideEvaluationResult?> EvaluateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (_slideEvaluator is null)
        {
            return null;
        }

        var context = new PipelineContext { UserPrompt = userPrompt };
        context.SnapshotFromRenderTool(SlideRenderTool);

        if (string.IsNullOrWhiteSpace(context.SlideXml))
        {
            var result = SlideEvaluationResult.Failed("尚未生成 SlideML，无法评估。");
            LastSlideEvaluation = result;
            return result;
        }

        return await EvaluateContextAsync(context, userPrompt, cancellationToken);
    }

    /// <summary>
    /// 手动触发提示词评估。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>评估结果。</returns>
    public async Task<PromptEvaluationResult?> EvaluatePromptAsync(CancellationToken cancellationToken = default)
    {
        if (_promptEvaluator is null)
        {
            return null;
        }

        IsEvaluating = true;
        try
        {
            var systemPrompt = _promptProvider.BuildSystemPrompt();
            var userPromptTemplate = _promptProvider.BuildInitialUserPrompt("{USER_INPUT}");

            var result = await _promptEvaluator.EvaluateAsync(systemPrompt, userPromptTemplate, cancellationToken);

            LastPromptEvaluation = result;

            AppendEvaluationMessage(result);

            PromptEvaluationCompleted?.Invoke(this, result);
            return result;
        }
        finally
        {
            IsEvaluating = false;
        }
    }

    private async Task<SlideEvaluationResult> EvaluateContextAsync(
        PipelineContext context, string userPrompt, CancellationToken cancellationToken)
    {
        IsEvaluating = true;
        try
        {
            byte[]? previewImageBytes = null;
            if (context.PreviewBitmap is { } bitmap)
            {
                using var memoryStream = new MemoryStream();
                SaveBitmapSourceAsPng(bitmap, memoryStream);
                previewImageBytes = memoryStream.ToArray();
            }

            var result = await _slideEvaluator!.EvaluateAsync(
                    userPrompt,
                    context.SlideXml ?? string.Empty,
                    context.RenderedXml ?? string.Empty,
                    context.Warnings ?? string.Empty,
                    previewImageBytes,
                    cancellationToken);

            context.SlideEvaluation = result;
            LastSlideEvaluation = result;

            AppendEvaluationMessage(result);

            EvaluationCompleted?.Invoke(this, result);
            return result;
        }
        finally
        {
            IsEvaluating = false;
        }
    }

    private void OnSlideRendered()
    {
        OnPropertyChanged(nameof(PreviewBitmap));
        OnPropertyChanged(nameof(CurrentSlideXml));
        OnPropertyChanged(nameof(RenderedXml));
        OnPropertyChanged(nameof(WarningText));
        SlideRendered?.Invoke();
    }

    /// <summary>
    /// 将评估结果作为用户消息追加到聊天列表中，使评估报告在聊天气泡中可见。
    /// </summary>
    private void AppendEvaluationMessage(SlideEvaluationResult result)
    {
        var message = CopilotChatMessage.CreateUser(result.ToDisplayText());
        message.IsPresetInfo = true;
        _copilotChatManager.ChatMessages.Add(message);
    }

    /// <summary>
    /// 将提示词评估结果作为用户消息追加到聊天列表中。
    /// </summary>
    private void AppendEvaluationMessage(PromptEvaluationResult result)
    {
        var message = CopilotChatMessage.CreateUser(result.ToDisplayText());
        message.IsPresetInfo = true;
        _copilotChatManager.ChatMessages.Add(message);
    }

    /// <summary>
    /// 将 <see cref="BitmapSource"/> 保存为 PNG 到流。
    /// </summary>
    private static void SaveBitmapSourceAsPng(BitmapSource bitmap, Stream stream)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
