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

namespace PptxGenerator;

/// <summary>
/// SlideML 生成管道编排器，管理 生成 → 渲染 → 评估 的完整生命周期。
/// </summary>
public sealed class SlideGenerationPipeline : INotifyPropertyChanged
{
    private readonly CopilotChatManager _copilotChatManager;
    private readonly IPromptProvider _promptProvider;
    private readonly ISlideEvaluator? _slideEvaluator;
    private readonly IPromptEvaluator? _promptEvaluator;
    private readonly IDispatcher _dispatcher;

    public SlideGenerationPipeline(
        CopilotChatManager copilotChatManager,
        IPromptProvider promptProvider,
        SlideRenderTool slideRenderTool,
        IDispatcher dispatcher,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        SlideRenderTool = slideRenderTool ?? throw new ArgumentNullException(nameof(slideRenderTool));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _slideEvaluator = slideEvaluator;
        _promptEvaluator = promptEvaluator;

        SlideRenderTool.SlideRendered += OnSlideRendered;
    }

    public SlideRenderTool SlideRenderTool { get; }

    public CopilotChatManager ChatManager => _copilotChatManager;

    public IPreviewImage? PreviewImage => SlideRenderTool.LatestPreviewImage;

    public string CurrentSlideXml => SlideRenderTool.LatestSlideXml;

    public string RenderedXml => SlideRenderTool.LatestRenderedXml;

    public string WarningText => SlideRenderTool.LatestWarnings;

    private SlideEvaluationResult? _lastSlideEvaluation;
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
    public bool IsEvaluating
    {
        get => _isEvaluating;
        private set
        {
            _isEvaluating = value;
            OnPropertyChanged(nameof(IsEvaluating));
        }
    }

    public bool CanEvaluate => _slideEvaluator is not null;
    public bool CanEvaluatePrompt => _promptEvaluator is not null;

    public event EventHandler<SlideEvaluationResult>? EvaluationCompleted;
    public event EventHandler<PromptEvaluationResult>? PromptEvaluationCompleted;
    public event Action? SlideRendered;
    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(userPrompt, isFirstMessage: true, attachPreview: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

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
                    var dataContent = await DataContent.LoadFromAsync(imageFile, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    contents.Add(dataContent);
                }
            }
        }

        if (attachPreview)
        {
            var previewDataContent = await SlideRenderTool.CreatePreviewDataContentAsync(cancellationToken)
                .ConfigureAwait(false);
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
        await requestResult.RunTask.ConfigureAwait(false);

        bool doNotRender = string.IsNullOrEmpty(CurrentSlideXml);
        if (doNotRender)
        {
            var toolRequest = request with
            {
                Contents = [new TextContent("请调用 render_slide 工具进行渲染，根据渲染结果优化界面")],
                SystemPrompt = "**重要：生成 SlideML 后必须调用 render_slide 工具，不可跳过此步骤**",
            };
            await _copilotChatManager.SendMessage(toolRequest).RunTask.ConfigureAwait(false);
        }

        _dispatcher.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
        });

        if (_slideEvaluator is not null && !string.IsNullOrWhiteSpace(CurrentSlideXml))
        {
            var context = new PipelineContext { UserPrompt = userMessage };
            context.SnapshotFromRenderTool(SlideRenderTool);
            _ = EvaluateContextAsync(context, userMessage, cancellationToken);
        }
    }

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

        return await EvaluateContextAsync(context, userPrompt, cancellationToken).ConfigureAwait(false);
    }

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

            var result = await _promptEvaluator.EvaluateAsync(systemPrompt, userPromptTemplate, cancellationToken)
                .ConfigureAwait(false);

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
            if (context.PreviewImage is { } image)
            {
                using var memoryStream = new MemoryStream();
                image.Save(memoryStream);
                previewImageBytes = memoryStream.ToArray();
            }

            var result = await _slideEvaluator!.EvaluateAsync(
                    userPrompt,
                    context.SlideXml ?? string.Empty,
                    context.RenderedXml ?? string.Empty,
                    context.Warnings ?? string.Empty,
                    previewImageBytes,
                    cancellationToken)
                .ConfigureAwait(false);

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
        OnPropertyChanged(nameof(PreviewImage));
        OnPropertyChanged(nameof(CurrentSlideXml));
        OnPropertyChanged(nameof(RenderedXml));
        OnPropertyChanged(nameof(WarningText));
        SlideRendered?.Invoke();
    }

    private void AppendEvaluationMessage(SlideEvaluationResult result)
    {
        var message = CopilotChatMessage.CreateUser(result.ToDisplayText());
        message.IsPresetInfo = true;
        _copilotChatManager.ChatMessages.Add(message);
    }

    private void AppendEvaluationMessage(PromptEvaluationResult result)
    {
        var message = CopilotChatMessage.CreateUser(result.ToDisplayText());
        message.IsPresetInfo = true;
        _copilotChatManager.ChatMessages.Add(message);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}