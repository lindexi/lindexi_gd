using PptxGenerator.Models;

namespace PptxGenerator.Prompt;

/// <summary>
/// SlideML 默认提示词提供者，支持运行时覆盖提示词以支持迭代优化。
/// </summary>
public sealed class SlideMlPromptProvider : ISlideMlPromptProvider
{
    private readonly SlideDocumentContext _documentContext;
    private string? _systemPromptOverride;
    private string? _userPromptTemplateOverride;

    /// <summary>
    /// 初始化 <see cref="SlideMlPromptProvider"/> 的新实例。
    /// </summary>
    /// <param name="documentContext">文档级上下文，提供画布尺寸等信息；为 <see langword="null"/> 时使用默认 1280×720。</param>
    public SlideMlPromptProvider(SlideDocumentContext? documentContext = null)
    {
        _documentContext = documentContext ?? new SlideDocumentContext();
    }

    /// <summary>
    /// 更新提示词覆盖值。传入非 <see langword="null"/> 的参数将覆盖默认提示词。
    /// 传入 <see langword="null"/> 表示该部分保持当前值不变。
    /// </summary>
    /// <param name="systemPrompt">新的系统提示词，为 <see langword="null"/> 时保持不变。</param>
    /// <param name="userPromptTemplate">新的用户提示词模板，为 <see langword="null"/> 时保持不变。
    /// 模板中使用 <c>{USER_INPUT}</c> 作为用户输入占位符。</param>
    public void UpdatePrompts(string? systemPrompt, string? userPromptTemplate)
    {
        if (systemPrompt is not null)
        {
            _systemPromptOverride = systemPrompt;
        }

        if (userPromptTemplate is not null)
        {
            _userPromptTemplateOverride = userPromptTemplate;
        }
    }

    /// <summary>
    /// 清空所有覆盖值，恢复默认提示词。
    /// </summary>
    public void ResetToDefault()
    {
        _systemPromptOverride = null;
        _userPromptTemplateOverride = null;
    }

    /// <inheritdoc />
    public string BuildSystemPrompt()
    {
        if (_systemPromptOverride is not null)
        {
            return _systemPromptOverride;
        }

        return BuildDefaultSystemPrompt();
    }

    /// <inheritdoc />
    public string BuildInitialUserPrompt(string userPrompt)
    {
        ArgumentNullException.ThrowIfNull(userPrompt);

        if (_userPromptTemplateOverride is not null)
        {
            return _userPromptTemplateOverride.Replace("{USER_INPUT}", userPrompt, StringComparison.Ordinal);
        }

        return BuildDefaultInitialUserPrompt(userPrompt);
    }

    /// <summary>
    /// 获取默认系统提示词文本。
    /// </summary>
    public string BuildDefaultSystemPrompt()
    {
        var canvasSize = $"{_documentContext.CanvasWidth}×{_documentContext.CanvasHeight}";
        return $"""
你是一个专业的幻灯片排版引擎。你的任务是根据用户的需求，生成一份 SlideML 格式的 XML 文档。

## 核心规则（必须遵守）
- **生成 SlideML 后必须立即调用 render_slide 工具验证排版效果，不允许跳过。**
- 调用 render_slide 之后，可以调用 get_render_preview 工具查看渲染后的页面截图，从视觉层面评估颜色、间距、对齐等。
- 如果收到渲染警告和回填后的 XML，请根据反馈修改并重新输出完整 XML，然后再次调用 render_slide。
- 适可而止，最多调用 render_slide 工具 4 次。

## SlideML 基本规则
- 画布尺寸固定为 {canvasSize} 像素，坐标原点在左上角
- 所有尺寸单位为 px（不写单位），颜色格式为 #RRGGBB 或 #AARRGGBB
- 标签必须严格遵守定义，不要创造新标签或新属性
- 元素 Id 可以不写，引擎会自动分配

## 标签与属性
### Page
属性: Background（背景色，可选，默认 #FFFFFF）
### Panel
属性: X, Y, Width, Height（均可选）, Padding（可选，默认 0）, Background（可选）, Layout（Absolute/Horizontal/Vertical，默认 Absolute）, Gap（流式布局间距，默认 0）, Margin（外边距，逗号分隔 1~4 个值）
### Rect
属性: X, Y, Width, Height（均可选）, Fill, Stroke, StrokeThickness, CornerRadius（单值，默认 0）, StrokeDashArray（虚线描边，逗号分隔数值）, Shadow（属性形式 "OffsetX OffsetY Blur Color"）, Margin（外边距，逗号分隔 1~4 个值）, HorizontalAlignment（Left/Center/Right）, VerticalAlignment（Top/Center/Bottom）, Opacity（0.0~1.0）
### TextElement
属性: X, Y, Width, Height（均可选）, Text（必填）, FontName（默认 Microsoft YaHei）, FontSize（默认 16）, IsBold（True/False）, IsItalic（True/False）, Foreground（默认 #000000）, TextAlignment（Left/Center/Right/Justify，默认 Left）, HorizontalAlignment, VerticalAlignment, Opacity, Margin（外边距，逗号分隔 1~4 个值）
### Image
属性: X, Y, Width, Height（均可选）, Source（必填，图片资源ID）, Stretch（None/Fill/Uniform/UniformToFill，默认 Uniform）, HorizontalAlignment, VerticalAlignment, Opacity, Margin（外边距，逗号分隔 1~4 个值）
### 子元素
- `<Fill><LinearGradient X1 Y1 X2 Y2><Stop Offset Color/></LinearGradient></Fill>` — 渐变填充，可用于 Rect 和 Panel（Stop Offset 范围 0~1）
- `<Stroke><LinearGradient>...</LinearGradient></Stroke>` — 渐变描边，可用于 Rect
- `<Shadow OffsetX OffsetY Blur Color Opacity/>` — 精细阴影子元素，可用于 Rect
- `<Span Text FontSize FontName Foreground IsBold IsItalic TextDecoration/>` — 富文本片段，可用于 TextElement

## 排版规则
1. 所有子元素相对于直接父容器定位
2. Z 序按文档出现顺序，后出现的在上层
3. 文本设置 Width 后会自动换行，不设置则单行
4. Panel 不设置 Width/Height 时自动包裹子元素
5. 子元素超出父容器的部分会被裁剪
6. Panel 设置 Layout="Horizontal"/"Vertical" 时子元素沿排列轴依次排列，排列轴上的 X/Y 被忽略，使用 Gap 和 Margin 控制间距
7. 流式布局实际间距 = max(Gap, 前元素尾Margin + 后元素头Margin)
8. 流式布局不支持 Wrap，子元素超出时产生 Warning
9. CornerRadius 仅支持单值

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
    /// 获取默认用户提示词模板文本。
    /// </summary>
    public string BuildDefaultInitialUserPrompt(string userPrompt)
    {
        ArgumentNullException.ThrowIfNull(userPrompt);

        var canvasSize = $"{_documentContext.CanvasWidth}x{_documentContext.CanvasHeight}";
        return $"""
请根据以下需求生成单页 SlideML：

{userPrompt}

要求：
1. 尽量使用浅色主题，视觉清爽
2. 标题、副标题、正文层级明显
3. 页面内容要适合 {canvasSize}
4. 如果需要图片，可以使用占位资源 ID，如 image_001
5. 生成 XML 后必须调用 render_slide 工具验证排版效果
6. 只输出 XML，不要用 markdown 代码块包裹
7. 合理使用 Panel 的 Layout 属性（Horizontal/Vertical）减少手动坐标计算
""";
    }
}
