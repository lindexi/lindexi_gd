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
    private string? _streamingSystemPromptOverride;
    private string? _streamingUserPromptTemplateOverride;

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
    /// <param name="streamingSystemPrompt">新的流式系统提示词，为 <see langword="null"/> 时保持不变。</param>
    /// <param name="streamingUserPromptTemplate">新的流式用户提示词模板，为 <see langword="null"/> 时保持不变。
    /// 模板中使用 <c>{USER_INPUT}</c> 作为用户输入占位符。</param>
    public void UpdatePrompts(string? systemPrompt, string? userPromptTemplate, string? streamingSystemPrompt = null, string? streamingUserPromptTemplate = null)
    {
        if (systemPrompt is not null)
        {
            _systemPromptOverride = systemPrompt;
        }

        if (userPromptTemplate is not null)
        {
            _userPromptTemplateOverride = userPromptTemplate;
        }

        if (streamingSystemPrompt is not null)
        {
            _streamingSystemPromptOverride = streamingSystemPrompt;
        }

        if (streamingUserPromptTemplate is not null)
        {
            _streamingUserPromptTemplateOverride = streamingUserPromptTemplate;
        }
    }

    /// <summary>
    /// 清空所有覆盖值，恢复默认提示词。
    /// </summary>
    public void ResetToDefault()
    {
        _systemPromptOverride = null;
        _userPromptTemplateOverride = null;
        _streamingSystemPromptOverride = null;
        _streamingUserPromptTemplateOverride = null;
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
属性: X, Y, Width, Height（均可选）, Fill, Stroke, StrokeThickness, CornerRadius（圆角半径，支持 1~4 值逗号分隔，默认 0）, StrokeDashArray（虚线描边，逗号分隔数值）, Shadow（属性形式 "OffsetX OffsetY Blur Color"）, Margin（外边距，逗号分隔 1~4 个值）, HorizontalAlignment（Left/Center/Right）, VerticalAlignment（Top/Center/Bottom）, Opacity（0.0~1.0）
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
9. CornerRadius 支持 1~4 个值逗号分隔（四角简写规则），如 "8" 四角统一，"8,0,8,0" 左上/右下圆角、右上/左下直角

## 禁止事项
- 不要写 ActualWidth、ActualHeight、ActualLineCount 属性
- 不要创造未定义的标签或属性
- 不要使用 XAML、HTML 等其他语法

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

    /// <inheritdoc />
    public string BuildStreamingSystemPrompt()
    {
        if (_streamingSystemPromptOverride is not null)
        {
            return _streamingSystemPromptOverride;
        }

        return BuildDefaultStreamingSystemPrompt();
    }

    /// <summary>
    /// 获取默认流式输出系统提示词文本。
    /// </summary>
    public string BuildDefaultStreamingSystemPrompt()
    {
        var canvasSize = $"{_documentContext.CanvasWidth}×{_documentContext.CanvasHeight}";
        return $"""
你是一个专业的幻灯片排版引擎。你的任务是根据用户的需求，以流式方式逐片段生成 SlideML 格式的 XML 文档。

## 流式输出规则
- 逐片段输出 XML，每个片段是一个完整的顶层 XML 元素（如 <Page>、<Panel>、<Rect> 等）
- 不要等待整个文档完成再输出，边思考边输出
- 每个片段必须是自包含的完整 XML 元素（自闭合标签或配对的开始/结束标签）
- 可以在任何位置中断并修正之前的输出
- 不要使用 markdown 代码块包裹 XML
- 使用 StyleFrom 属性引用已定义的模板元素（通过 StyleId）
- 使用 Remove 标签删除不需要的元素

## SlideML 基本规则
- 画布尺寸固定为 {canvasSize} 像素，坐标原点在左上角
- 所有尺寸单位为 px（不写单位），颜色格式为 #RRGGBB 或 #AARRGGBB
- 标签必须严格遵守定义，不要创造新标签或新属性
- 元素 Id 可以不写，引擎会自动分配。悬空元素（不在 Page 内的顶层元素）必须带 StyleId 属性。

## 标签与属性
### Page
属性: Background（背景色，可选，默认 #FFFFFF）
### Panel
属性: X, Y, Width, Height（均可选）, Padding, Background, Layout（Absolute/Horizontal/Vertical，默认 Absolute）, Gap, Margin
### Rect
属性: X, Y, Width, Height（均可选）, Fill, Stroke, StrokeThickness, CornerRadius, StrokeDashArray, Shadow, Margin, HorizontalAlignment, VerticalAlignment, Opacity
### TextElement
属性: X, Y, Width, Height（均可选）, Text（必填）, FontName, FontSize, IsBold, IsItalic, Foreground, TextAlignment, HorizontalAlignment, VerticalAlignment, Opacity, Margin
### Image
属性: X, Y, Width, Height（均可选）, Source（必填）, Stretch, HorizontalAlignment, VerticalAlignment, Opacity, Margin
### Remove
属性: TargetId（必填，要删除的元素 Id）
### StyleFrom
属性: 引用已定义元素的 StyleId，复制其属性作为默认值。悬空元素必须带 StyleId。

## 排版规则
1. 所有子元素相对于直接父容器定位
2. Z 序按文档出现顺序，后出现的在上层
3. 文本设置 Width 后会自动换行，不设置则单行
4. Panel 不设置 Width/Height 时自动包裹子元素
5. 子元素超出父容器的部分会被裁剪

## 禁止事项
- 不要写 ActualWidth、ActualHeight、ActualLineCount 属性
- 不要创造未定义的标签或属性
- 不要使用 XAML、HTML 等其他语法
- 不要使用 markdown 代码块包裹 XML
""";
    }

    /// <inheritdoc />
    public string BuildStreamingUserPrompt(string userPrompt)
    {
        if (_streamingUserPromptTemplateOverride is not null)
        {
            return _streamingUserPromptTemplateOverride.Replace("{USER_INPUT}", userPrompt);
        }

        return BuildDefaultStreamingUserPrompt(userPrompt);
    }

    /// <summary>
    /// 获取默认流式输出用户提示词文本。
    /// </summary>
    public string BuildDefaultStreamingUserPrompt(string userPrompt)
    {
        ArgumentNullException.ThrowIfNull(userPrompt);

        var canvasSize = $"{_documentContext.CanvasWidth}×{_documentContext.CanvasHeight}";
        return $"""
请根据以下需求以流式片段方式生成单页 SlideML：

{userPrompt}

要求：
1. 尽量使用浅色主题，视觉清爽。
2. 标题、副标题、正文层级明显，优先使用字号等级（标题 L2、正文 L3、辅助 L4）。
3. 页面内容要适合画布 {canvasSize}。
4. 若需图片可用占位资源 ID，如 image_001。
5. 先输出 Page 骨架，再逐步填充和细化。
6. 每个元素必须带 Id 且全局唯一；同类元素优先用带 StyleId 的悬空模板 + StyleFrom 减少重复。
7. 合理使用 Panel 的 Layout 属性减少手动坐标计算。
8. 输出过程中可随时调用 get_slide_state 和 get_slide_preview 查看排版效果。
9. 发现问题用后续片段修正（调整坐标/尺寸，或用 Remove 删除后重来）。
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
