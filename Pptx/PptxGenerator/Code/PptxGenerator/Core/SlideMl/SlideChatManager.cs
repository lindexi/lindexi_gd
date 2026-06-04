using AgentLib;
using AgentLib.Model;

using Avalonia.Media.Imaging;
using Avalonia.Threading;

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
/// 组合 <see cref="CopilotChatManager"/>，预配置 SlideML 系统提示词与渲染工具。
/// 提供 SlideML 项目特定的聊天状态与便捷方法。
/// </summary>
public sealed class SlideChatManager : INotifyPropertyChanged
{
    private readonly CopilotChatManager _copilotChatManager;
    private Bitmap? _lastInjectedPreviewBitmap;

    public SlideChatManager(CopilotChatManager copilotChatManager, SlideRenderTool slideRenderTool)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        SlideRenderTool = slideRenderTool ?? throw new ArgumentNullException(nameof(slideRenderTool));
        SlideRenderTool.SlideRendered += OnSlideRendered;
    }

    public CopilotChatManager ChatManager => _copilotChatManager;

    /// <summary>
    /// SlideML 渲染工具，暴露为属性供外部订阅事件。
    /// </summary>
    public SlideRenderTool SlideRenderTool { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当前预览 Bitmap（由 SlideRenderTool 缓存）。
    /// </summary>
    public Bitmap? PreviewBitmap => SlideRenderTool.LatestPreviewBitmap;

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

    /// <summary>
    /// 发送 SlideML 生成请求。自动附加 SlideRenderTool。
    /// </summary>
    /// <param name="userPrompt">用户自然语言需求描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return;
        }

        var tools = new[] { SlideRenderTool.CreateTool(), SlideRenderTool.CreatePreviewTool() };

        var request = SendMessageRequest.FromText(BuildInitialUserPrompt(userPrompt), tools: tools, systemPrompt: BuildSystemPrompt());

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask.ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            InjectPreviewScreenshot();
            OnPropertyChanged(nameof(PreviewBitmap));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
        });
    }

    /// <summary>
    /// 发送继续对话请求。自动附加 SlideRenderTool。
    /// </summary>
    /// <param name="userMessage">用户的修改意见。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendContinueRequestAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        var tools = new[] { SlideRenderTool.CreateTool(), SlideRenderTool.CreatePreviewTool() };

        var request = SendMessageRequest.FromText(userMessage, tools: tools, systemPrompt: null);

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask.ConfigureAwait(false);

        InjectPreviewScreenshot();
        OnPropertyChanged(nameof(PreviewBitmap));
        OnPropertyChanged(nameof(CurrentSlideXml));
        OnPropertyChanged(nameof(RenderedXml));
        OnPropertyChanged(nameof(WarningText));
    }

    /// <summary>
    /// 统一发送消息入口，供 UI 调用。
    /// 首条消息会包裹初始提示词并切换新会话，后续消息以纯文本发送保留上下文。
    /// </summary>
    /// <param name="userMessage">用户输入文本。</param>
    /// <param name="isFirstMessage">是否为首条消息。首条消息会包裹 <see cref="BuildInitialUserPrompt"/> 并切换新会话。</param>
    /// <param name="attachPreview">是否附加当前渲染预览图。<see langword="true"/> 时将预览图作为 <see cref="DataContent"/> 加入消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SendMessageAsync(string userMessage, bool isFirstMessage, bool attachPreview, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        var tools = new[] { SlideRenderTool.CreateTool(), SlideRenderTool.CreatePreviewTool() };

        var processedText = isFirstMessage ? BuildInitialUserPrompt(userMessage) : userMessage;
        var systemPrompt = isFirstMessage ? BuildSystemPrompt() : null;

        var contents = new List<AIContent>(2) { new TextContent(processedText) };

        if (attachPreview)
        {
            var previewDataContent = await SlideRenderTool.CreatePreviewDataContentAsync(cancellationToken).ConfigureAwait(false);
            if (previewDataContent is not null)
            {
                contents.Add(previewDataContent);
            }
        }

        var request = new SendMessageRequest(contents,
            WithHistory: !isFirstMessage,
            CreateNewSession: isFirstMessage,
            Tools: tools,
            SystemPrompt: systemPrompt,
            CancellationToken: cancellationToken);

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask.ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            InjectPreviewScreenshot();
            OnPropertyChanged(nameof(PreviewBitmap));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
        });
    }

    /// <summary>
    /// 将最新的渲染预览图注入到当前 Assistant 消息的 MessageItems 中，
    /// 作为多模态截图反馈供下一轮模型调用参考。
    /// </summary>
    private void InjectPreviewScreenshot()
    {
        var bitmap = SlideRenderTool.LatestPreviewBitmap;
        if (bitmap is null || ReferenceEquals(bitmap, _lastInjectedPreviewBitmap))
        {
            return;
        }

        _lastInjectedPreviewBitmap = bitmap;

        var lastAssistantMessage = _copilotChatManager.ChatMessages
            .LastOrDefault(m => m.Role == ChatRole.Assistant);
        if (lastAssistantMessage is null)
        {
            return;
        }

        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream);
        var imageBytes = memoryStream.ToArray();

        var binaryData = BinaryData.FromBytes(imageBytes);
        var imageItem = new CopilotChatImageItem(binaryData, "image/png");
        lastAssistantMessage.MessageItems.Add(imageItem);
    }

    /// <summary>
    /// SlideRenderTool 渲染完成事件处理，转发 PropertyChanged 通知。
    /// </summary>
    private void OnSlideRendered()
    {
        OnPropertyChanged(nameof(PreviewBitmap));
        OnPropertyChanged(nameof(CurrentSlideXml));
        OnPropertyChanged(nameof(RenderedXml));
        OnPropertyChanged(nameof(WarningText));
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 构建 SlideML 排版引擎系统提示词。
    /// </summary>
    public static string BuildSystemPrompt()
    {
        return """
你是一个专业的幻灯片排版引擎。你的任务是根据用户的需求，生成一份 SlideML 格式的 XML 文档。

## 核心规则（必须遵守）
- **生成 SlideML 后必须立即调用 render_slide 工具验证排版效果，不允许跳过。**
- 调用 render_slide 之后，可以调用 get_render_preview 工具查看渲染后的页面截图，从视觉层面评估颜色、间距、对齐等。
- 如果收到渲染警告和回填后的 XML，请根据反馈修改并重新输出完整 XML，然后再次调用 render_slide。
- 适可而止，最多调用 render_slide 工具 4 次。

## SlideML 基本规则
- 画布尺寸固定为 1280×720 像素，坐标原点在左上角
- 所有尺寸单位为 px（不写单位），颜色格式为 #RRGGBB 或 #AARRGGBB
- 标签必须严格遵守定义，不要创造新标签或新属性
- 元素 Id 可以不写，引擎会自动分配

## 标签与属性
### Page
属性: Background（背景色，可选，默认 #FFFFFF）
### Panel
属性: X, Y, Width, Height（均可选）, Padding（可选，默认 0）, Background（可选）
### Rect
属性: X, Y, Width, Height（均可选）, Fill, Stroke, StrokeThickness, CornerRadius, HorizontalAlignment（Left/Center/Right）, VerticalAlignment（Top/Center/Bottom）, Opacity（0.0~1.0）
### TextElement
属性: X, Y, Width, Height（均可选）, Text（必填）, FontName（默认 Microsoft YaHei）, FontSize（默认 16）, Foreground（默认 #000000）, TextAlignment（Left/Center/Right/Justify，默认 Left）, LineHeight（默认 1.2）, HorizontalAlignment, VerticalAlignment, Opacity
### Image
属性: X, Y, Width, Height（均可选）, Source（必填，图片资源ID）, Stretch（None/Fill/Uniform/UniformToFill，默认 Uniform）, HorizontalAlignment, VerticalAlignment, Opacity

## 排版规则
1. 所有子元素相对于直接父容器定位
2. Z 序按文档出现顺序，后出现的在上层
3. 文本设置 Width 后会自动换行，不设置则单行
4. Panel 不设置 Width/Height 时自动包裹子元素
5. 子元素超出父容器的部分会被裁剪

## 禁止事项
- 不要写 ActualWidth、ActualHeight、ActualLineCount 属性
- 不要创造未定义的标签或属性
- 不要使用 XAML、CSS、XAML 、HTML 等其他语法

## 输出格式
- 直接输出 XML，不要使用 markdown 代码块包裹
- 第一行必须是 <?xml version="1.0" encoding="UTF-8"?>
- 根元素必须是 <Page>
- 只输出最终 XML，不要追加解释

## 实验目标
- 当前只需要生成单页
- 优先让版面完整、层级清晰、留白充足
- **重要：生成 SlideML 后必须调用 render_slide 工具，不可跳过此步骤**
""";
    }

    /// <summary>
    /// 构建初始用户提示词，包裹用户的自然语言需求。
    /// </summary>
    public static string BuildInitialUserPrompt(string userPrompt)
    {
        return $"""
请根据以下需求生成单页 SlideML：

{userPrompt}

要求：
1. 尽量使用浅色主题，视觉清爽
2. 标题、副标题、正文层级明显
3. 页面内容要适合 1280x720
4. 如果需要图片，可以使用占位资源 ID，如 image_001
5. 生成 XML 后必须调用 render_slide 工具验证排版效果
6. 只输出 XML，不要用 markdown 代码块包裹
""";
    }
}