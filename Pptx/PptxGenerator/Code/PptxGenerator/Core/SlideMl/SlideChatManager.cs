using AgentLib;
using AgentLib.Model;

using Avalonia.Media.Imaging;

using Microsoft.Extensions.AI;

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PptxGenerator;

/// <summary>
/// 组合 <see cref="CopilotChatManager"/>，预配置 SlideML 系统提示词与渲染工具。
/// 提供 SlideML 项目特定的聊天状态与便捷方法。
/// </summary>
public sealed class SlideChatManager : INotifyPropertyChanged
{
    private readonly CopilotChatManager _copilotChatManager;
    private readonly SlideRenderTool _slideRenderTool;

    public SlideChatManager(CopilotChatManager copilotChatManager, SlideRenderTool slideRenderTool)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        _slideRenderTool = slideRenderTool ?? throw new ArgumentNullException(nameof(slideRenderTool));
    }

    public CopilotChatManager ChatManager => _copilotChatManager;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当前预览 Bitmap（由 SlideRenderTool 缓存）。
    /// </summary>
    public Bitmap? PreviewBitmap => _slideRenderTool.LatestPreviewBitmap;

    /// <summary>
    /// 最近一次渲染的 SlideML XML。
    /// </summary>
    public string CurrentSlideXml => _slideRenderTool.LatestSlideXml;

    /// <summary>
    /// 最近一次渲染后回填的 XML。
    /// </summary>
    public string RenderedXml => _slideRenderTool.LatestRenderedXml;

    /// <summary>
    /// 最近一次渲染的警告列表。
    /// </summary>
    public string WarningText => _slideRenderTool.LatestWarnings;

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

        var tools = new[] { _slideRenderTool.CreateTool() };

        var request = SendMessageRequest.FromText(BuildInitialUserPrompt(userPrompt), tools: tools, systemPrompt: BuildSystemPrompt());

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask.ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // 刷新状态
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

        var tools = new[] { _slideRenderTool.CreateTool() };

        var request = SendMessageRequest.FromText(userMessage, tools: tools, systemPrompt: null);

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask.ConfigureAwait(false);

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
- 不要使用 XAML、CSS、HTML 等其他语法

## 输出格式
- 直接输出 XML，不要使用 markdown 代码块包裹
- 第一行必须是 <?xml version="1.0" encoding="UTF-8"?>
- 根元素必须是 <Page>
- 只输出最终 XML，不要追加解释

## 实验目标
- 当前只需要生成单页
- 优先让版面完整、层级清晰、留白充足
- 生成 SlideML 后必须调用 render_slide 工具验证排版效果
- 如果收到渲染警告和回填后的 XML，请根据反馈修改并重新输出完整 XML
- 适可而止，最多调用 render_slide 工具 4 次
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
5. 只输出 XML
""";
    }
}