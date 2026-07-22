using System.Diagnostics;
using System.Text.Json;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;
using CoursewarePptxGeneratorWpfDemo.Models;
using Microsoft.Extensions.AI;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Uses an independent AgentLib conversation to compile a validated executable courseware design system.
/// </summary>
public sealed class CopilotCoursewareThemeAgent : ICoursewareThemeAgent
{
    private const int MaximumRequestCount = 2;
    private const string SystemPrompt = """
        你是全课件设计系统编译器。用户消息包含经过本地校验的完整课件 Markdown、确定性结构化事实和可选视觉样本清单。最终产物必须是可执行、可验证的 CoursewareDesignSystem，而不是小型主题建议。
        必须阅读全部 slides，依次使用设计系统草稿工具，并在每次成功写入后使用最新 DraftId 和 Revision。局部失败只修正对应分区。
        必须提交画布与网格、字体、颜色、间距与效果、组件、素材策略、动态页面类型、全部 SlideId 唯一映射、可编译 SlideML 模板、无障碍与一致性规则、证据和假设。
        模板只使用当前 SlideML 标签与属性；设计令牌使用 {{token:id}}，内容或素材槽位使用 {{slot:id}}。
        只有用户消息确实附带截图时才允许提交 visualObservations；每条观察必须绑定 visualSamples 中的 SlideId、页码和 SampleRole。没有图片时不得声称看到了真实配色、Logo、阴影、渐变、素材内容或视觉质量。
        不得输出本地路径、隐藏推理或未登记 ResourceId。最终必须调用 complete_courseware_design_system。
        """;

    private readonly ICopilotChatManagerFactory _chatManagerFactory;
    private readonly CoursewareDesignSystemValidator _validator;

    public CopilotCoursewareThemeAgent(ICopilotChatManagerFactory chatManagerFactory, CoursewareDesignSystemValidator validator)
    {
        ArgumentNullException.ThrowIfNull(chatManagerFactory);
        ArgumentNullException.ThrowIfNull(validator);
        _chatManagerFactory = chatManagerFactory;
        _validator = validator;
    }

    public async Task<CoursewareDesignSystemAgentResult> AnalyzeAsync(
        CoursewareAnalysisInput analysisInput,
        CoursewareInputPackage inputPackage,
        CoursewareStructuredFactReport structuredFacts,
        IReadOnlyList<CoursewareVisualSample> visualSamples,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysisInput);
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(structuredFacts);
        ArgumentNullException.ThrowIfNull(visualSamples);
        CoursewareAnalysisInputValidator.ValidateForTransmission(analysisInput, cancellationToken);

        var chatManager = await _chatManagerFactory.CreateAsync(AgentWorkload.ThemeAnalysis, cancellationToken).ConfigureAwait(true);
        var modelDefinition = chatManager.AgentApiEndpointManager.PrimaryModel.ModelDefinition;
        var supportsImages = modelDefinition.Capabilities?.Attachment == true && modelDefinition.Capabilities.Input.Image;
        var selectedSlideIds = visualSamples.Select(sample => sample.SlideId).ToHashSet(StringComparer.Ordinal);
        var attachedSlides = supportsImages
            ? inputPackage.Slides.Where(slide => selectedSlideIds.Contains(slide.SlideId) && slide.ScreenshotFile?.Exists == true).ToArray()
            : [];
        var attachedSlideIds = attachedSlides.Select(slide => slide.SlideId).ToHashSet(StringComparer.Ordinal);
        var attachedSamples = visualSamples.Where(sample => attachedSlideIds.Contains(sample.SlideId)).ToArray();
        var draft = new CoursewareDesignSystemDraftBuilder(
            inputPackage.Slides.Select(slide => slide.SlideId),
            inputPackage.Resources.Select(resource => resource.ResourceId ?? string.Empty),
            attachedSamples.Length > 0);
        var toolSet = new CoursewareDesignSystemToolSet(draft, _validator, attachedSamples);
        var tools = toolSet.CreateTools();
        var prompt = BuildPrompt(analysisInput.Prompt, structuredFacts, attachedSamples, supportsImages);
        var initialContents = new List<AIContent> { new TextContent(prompt) };
        foreach (var slide in attachedSlides)
        {
            initialContents.Add(await DataContent.LoadFromAsync(slide.ScreenshotFile!.FullName, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        for (var requestIndex = 0; requestIndex < MaximumRequestCount; requestIndex++)
        {
            var isRepair = requestIndex > 0;
            var requestPrompt = isRepair ? BuildRepairPrompt(prompt, draft, toolSet.Completion?.Validation) : prompt;
            var requestContents = isRepair ? new List<AIContent> { new TextContent(requestPrompt) } : initialContents;
            _ = CoursewareModelContextBudgetValidator.ValidateIfConfigured(modelDefinition, SystemPrompt, requestPrompt, tools, cancellationToken);
            progress?.Report(CreateEvent(
                isRepair ? CoursewareAnalysisStage.RepairingTheme : CoursewareAnalysisStage.DesigningTheme,
                isRepair ? "正在修正可执行设计系统" : "正在编译可执行设计系统",
                attachedSamples.Length > 0
                    ? $"正在基于完整 Markdown、结构化事实和 {attachedSamples.Length} 张受控截图操作设计系统草稿。"
                    : supportsImages ? "没有可附加的受控截图，正在使用完整 Markdown 和结构化事实。" : "模型不支持图片附件，正在使用完整 Markdown 和结构化事实。",
                CoursewareAnalysisEventState.Running));
            var request = new SendMessageRequest(requestContents)
            {
                WithHistory = false,
                CreateNewSession = true,
                AppendDefaultTools = false,
                Tools = tools,
                SystemPrompt = SystemPrompt,
                CancellationToken = cancellationToken,
            };
            var stopwatch = Stopwatch.StartNew();
            var result = chatManager.SendMessage(request);
            result.UserChatMessage.IsPresetInfo = true;
            result.AssistantChatMessage.IsPresetInfo = true;
            messageProgress?.Report(result.UserChatMessage);
            messageProgress?.Report(result.AssistantChatMessage);
            await result.RunTask.ConfigureAwait(true);
            stopwatch.Stop();
            if (toolSet.Completion is { Success: true, DesignSystem: not null } completion)
            {
                return new CoursewareDesignSystemAgentResult
                {
                    DesignSystem = completion.DesignSystem,
                    Validation = completion.Validation,
                    VisualAnalysis = new CoursewareVisualAnalysisReport
                    {
                        WasRequested = visualSamples.Count > 0,
                        ModelSupportedImages = supportsImages,
                        Samples = attachedSamples,
                        Observations = toolSet.VisualObservations,
                    },
                };
            }

            progress?.Report(CreateEvent(CoursewareAnalysisStage.ValidatingTheme, "可执行设计系统校验", $"本轮耗时 {stopwatch.Elapsed.TotalSeconds:0.0} 秒，正在按字段级诊断修订。", CoursewareAnalysisEventState.Warning));
        }

        var validation = toolSet.Completion?.Validation;
        var details = validation is null || validation.Diagnostics.Count == 0
            ? "模型未调用 complete_courseware_design_system。"
            : string.Join("；", validation.Diagnostics.Select(item => $"{item.Path}: {item.Message}"));
        throw new InvalidOperationException($"未能冻结有效的 CoursewareDesignSystem。{details}");
    }

    private static string BuildPrompt(string analysisPrompt, CoursewareStructuredFactReport structuredFacts, IReadOnlyList<CoursewareVisualSample> visualSamples, bool supportsImages)
    {
        var factsJson = JsonSerializer.Serialize(
            structuredFacts,
            typeof(CoursewareStructuredFactReport),
            CoursewareDesignJsonSerializerContext.Default);
        var visualJson = JsonSerializer.Serialize(
            visualSamples.ToArray(),
            typeof(CoursewareVisualSample[]),
            CoursewareDesignJsonSerializerContext.Default);
        return $"<analysis-envelope>\n{analysisPrompt}\n</analysis-envelope>\n<structured-facts>\n{factsJson}\n</structured-facts>\n<visual-capability supported=\"{supportsImages}\" attachedCount=\"{visualSamples.Count}\">\n{visualJson}\n</visual-capability>\n使用多工具完成并冻结设计系统。视觉附件顺序与 visualSamples 顺序一致。";
    }

    private static string BuildRepairPrompt(string originalPrompt, CoursewareDesignSystemDraftBuilder draft, CoursewareDesignSystemValidationReport? validation)
    {
        var diagnostics = validation?.Diagnostics.Count > 0
            ? string.Join("\n", validation.Diagnostics.Select(item => $"- {item.Code} | {item.Path} | {item.Message}"))
            : "- 尚未调用完成工具或草稿分区不完整。";
        return $"继续修订现有草稿，不要重新 begin。当前 DraftId={draft.DraftId}，Revision={draft.Revision}。\n只修正以下问题后再次 complete：\n{diagnostics}\n原始完整输入：\n{originalPrompt}";
    }

    private static CoursewareAnalysisEvent CreateEvent(CoursewareAnalysisStage stage, string title, string message, CoursewareAnalysisEventState state)
    {
        return new CoursewareAnalysisEvent { Stage = stage, Kind = CoursewareAnalysisEventKind.Progress, Title = title, Message = message, State = state };
    }
}
