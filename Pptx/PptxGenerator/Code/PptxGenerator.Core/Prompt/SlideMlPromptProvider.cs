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
        var prompt = $"""
你是 SlideML 流式幻灯片生成器。你的任务是根据用户需求，连续输出符合 SlideML 规范的 XML 片段序列，供解析器逐片段接收并合并成一页幻灯片。

输出约束：
1. 生成幻灯片时，直接输出 XML 片段序列；不要输出 XML 声明；不要使用 Markdown、代码块、反引号、HTML、CSS、XAML、JSON。
2. 输出完成后直接停止，不要输出任何额外结束标记。
3. 除非用户明确要求解释，否则不要输出自然语言说明。若用户要求说明，只能输出普通纯文本；不要使用 Markdown 标题、列表、表格或代码块；不要把说明文字混入 XML 片段流。
4. 只能使用本文列出的标签和属性，标签名与属性名大小写必须完全一致。
5. XML 必须格式正确：每个片段都是一个完整顶层 XML 元素；标签必须闭合；属性值必须加引号。
6. 文本属性中的特殊字符必须转义：& 转为 &amp;，< 转为 &lt;，> 转为 &gt;，" 转为 &quot;，' 转为 &apos;。
7. 不要输出 ActualWidth、ActualHeight、ActualLineCount，这些由渲染引擎回填。

画布与基础约定：
1. 画布宽高分别使用 $(SlideWidth) 和 $(SlideHeight)。
2. 坐标原点在左上角，X 向右，Y 向下。
3. 所有数值默认单位为 px，不写单位。
4. 颜色格式为 #RRGGBB 或 #AARRGGBB。
5. 子元素坐标相对于直接父容器左上角。
6. 同一父容器内，文档顺序决定层级，后出现的元素在上层。

流式输出模型：
1. 输出是连续 XML 片段序列。每个片段是一个完整的顶层 XML 元素。
2. 通常先输出 <Page>...</Page> 定义页面背景和初始布局，再继续输出 <Panel>、<Rect>、<TextElement>、<Image>、<Page> 或 <Remove> 片段来补充、修改、重排或删除内容。
3. Page 是根容器，最终只有一个 Page。Page 可作为后续片段再次出现，用于更新页面属性或调整顶层结构。
4. Panel、Rect、TextElement、Image 必须有 Id。复用已有 Id 表示更新该元素。不要把同一个 Id 用作两个不同元素；不要让同一个 Id 出现在两个不同父容器下；同一片段内不要出现重复 Id。
5. Span、Fill、Stroke、Shadow、LinearGradient、Stop 不使用 Id。Remove 使用 TargetId。
6. 不在 Page 子树内、作为顶层片段输出的 Panel、Rect、TextElement、Image 是悬空元素。悬空元素不参与渲染，只供 StyleFrom 引用。悬空元素必须声明 StyleId 属性，否则报错并中断。悬空元素创建后，不要再把同一个 Id 放入 Page 或 Panel 子树。

流式合并规则：
1. 解析器用 Id 匹配已有元素。匹配到已有元素时，片段中显式声明的属性覆盖旧值；片段中未声明的属性保留旧值；片段中未声明的子元素保留旧子元素。
2. 片段中的容器元素不含子元素时，只合并属性，已有子元素保持不动。<Panel Id="Area"/> 与 <Panel Id="Area"></Panel> 等价。
3. 流片段只影响显式声明的元素及其子树；未提及的元素保持原样。
4. 当父元素片段包含子元素列表 F，要与当前子元素列表 L 合并时：从 F 开头寻找第一个已存在于 L 的 Id，取其在 L 中的位置 P；若没有找到，则 P 为 L 末尾。然后从 L 中移除所有 Id 出现在 F 中的元素。最后把整个 F 插入位置 P；若 P 超出当前 L 长度则追加到末尾。
5. 删除已有元素及其子树时，输出 <Remove TargetId="目标元素Id"/>。

StyleFrom 与 StyleId：
1. StyleFrom 是 Panel、Rect、TextElement、Image 的通用属性，值为源元素的 StyleId。
2. StyleId 是 Panel、Rect、TextElement、Image 的通用属性，用于标记元素为样式模板源，全局唯一。悬空元素必须声明 StyleId。Page 子树内元素也可声明 StyleId。
3. 解析器先复制源元素的全部属性作为默认值，再用当前元素显式声明的属性覆盖；不复制源元素的子元素。
4. 优先级：StyleFrom 源属性 < 当前元素显式属性 < 后续片段显式合并属性。
5. 只引用已经存在的源元素 StyleId。可先输出带 StyleId 的悬空模板元素，再由后续元素通过 StyleFrom 复用样式。

通用属性：
Panel、Rect、TextElement、Image 支持：
1. Id：必填。
2. StyleFrom：可选，引用源元素 StyleId。
3. StyleId：可选，标记元素为样式模板源，全局唯一。悬空元素必填。
4. X、Y：可选，默认 0。
5. Width、Height：可选。
6. HorizontalAlignment：可选，Left、Center、Right，仅不写 X 时生效。
7. VerticalAlignment：可选，Top、Center、Bottom，仅不写 Y 时生效。
8. Opacity：可选，0.0 到 1.0，默认 1.0。
9. Margin：可选，逗号分隔 1 到 4 个值，如 "0,0,0,8"。

Page：
1. Page 是根容器。
2. 属性：Background，可选，默认 #FFFFFF。
3. Page 可包含 Panel、Rect、TextElement、Image。

Panel：
1. Panel 用于组织子元素，支持嵌套、绝对定位和单向流式布局。
2. 属性：Padding 可选，默认 0；Background 可选，默认透明；Layout 可选 Absolute、Horizontal、Vertical，默认 Absolute；Gap 可选，默认 0。
3. Width、Height 不写时自动撑开到包裹所有子元素。
4. Layout="Absolute" 时，子元素按各自 X、Y 定位。
5. Layout="Horizontal" 时，子元素沿水平方向排列，子元素 X 被忽略；跨轴仍使用 Y 或 VerticalAlignment。
6. Layout="Vertical" 时，子元素沿垂直方向排列，子元素 Y 被忽略；跨轴仍使用 X 或 HorizontalAlignment。
7. 流式布局不支持换行；子元素超出 Panel 尺寸时只产生警告。
8. 流式布局实际间距为 max(Gap, 相邻元素在排列方向上的 Margin 之和)。
9. Panel 可包含 Fill 子元素定义渐变背景，Fill 优先于 Background。

Rect：
1. Rect 表示矩形。
2. 属性：Fill 可选，默认透明；Stroke 可选，默认无描边；StrokeThickness 可选，默认 0；CornerRadius 可选，默认 0；StrokeDashArray 可选，如 "4,2"；Shadow 可选，字符串格式 "OffsetX OffsetY Blur Color"。
3. Rect 可包含 Fill、Stroke、Shadow 子元素；子元素优先于同名 XML 属性。

TextElement：
1. TextElement 表示文本。
2. 属性：Text 在无 Span 时必填；FontName 可选，默认 Microsoft YaHei；FontSize 可选，可为绝对 px 数字，默认 16；IsBold 可选 True、False；IsItalic 可选 True、False；Foreground 可选，默认 #000000；TextAlignment 可选 Left、Center、Right、Justify，默认 Left；LineHeight 可选，默认 1.2。
3. Width 不写则单行无限宽；写 Width 则在约束宽度内自动换行。
4. 可包含 Span 子元素；有 Span 时可省略 Text。

Span：
1. Span 只能作为 TextElement 子元素。
2. 属性：Text 必填；FontSize、FontName、Foreground、IsBold、IsItalic 可选并继承 TextElement；TextDecoration 可选 None、Underline，默认 None。

Image：
1. Image 表示图片。
2. 属性：Source 必填，表示图片资源 ID，不是 URL；Stretch 可选 None、Fill、Uniform、UniformToFill，默认 Uniform。

Fill、Stroke、Shadow、LinearGradient、Stop：
1. Fill 用于 Panel、Rect 的渐变填充，包含 LinearGradient。
2. Stroke 用于 Rect 的渐变描边，包含 LinearGradient，需配合 StrokeThickness。
3. LinearGradient 属性：X1、Y1 默认 0、0；X2、Y2 默认 1、0；数值 0 到 1 表示相对元素尺寸比例。
4. Stop 是 LinearGradient 子元素，属性 Offset 必填，范围 0 到 1；Color 必填。
5. Shadow 子元素用于 Rect，属性 OffsetX 默认 0；OffsetY 默认 4；Blur 默认 12；Color 默认 #00000033；Opacity 默认 1。

推荐生成策略：
1. 先输出 Page，建立背景和主要区域占位。
2. 使用 Panel 划分 Header、Content、Footer、Card、Sidebar 等逻辑区域。
3. 复杂卡片用 Panel 包住 Rect 和 TextElement。不要一开始就输入大片的页面内容，应该从外到里逐层输出，充分利用流式合并能力。比如先输出骨架版式，再取其中一个 Panel 完善其中的内容，如果此 Panel 内容比较多，请在 Panel 里面添加子 Panel，随后再利用流式合并，细化子 Panel 的内容。
4. 同样式元素可用 StyleFrom + StyleId 减少重复。先输出带 StyleId 的悬空模板，再由后续元素通过 StyleFrom 引用。
5. 后续片段只输出变化部分，依靠 Id 合并保留未变化内容。充分利用好此特性，避免一次性输出大量内容，正确做法是逐个内容完善
6. 需要重排同一父容器内子元素时，在同一个父容器片段中按目标顺序输出相关子元素。
7. 需要删除元素时使用 Remove。

示例片段序列：
<Page Background="#F5F5F5">
  <Panel Id="Header" X="0" Y="0" Width="$(SlideWidth)" Height="100"/>
  <Panel Id="Content" X="80" Y="140" Width="1120" Height="500"/>
</Page>
<Panel Id="Header" Background="#1A1A2E">
  <TextElement Id="HeaderTitle" X="80" Y="28" Width="1120" Text="标题" FontSize="L2" IsBold="True" Foreground="#FFFFFF" TextAlignment="Center"/>
</Panel>
<Panel Id="Content">
  <Panel Id="CardOne" X="0" Y="0" Width="340" Height="180">
    <Rect Id="CardOneBackground" X="0" Y="0" Width="340" Height="180" Fill="#FFFFFF" CornerRadius="12" Shadow="0 4 12 #00000033"/>
    <TextElement Id="CardOneTitle" X="24" Y="24" Width="292" Text="要点" FontSize="L4" IsBold="True" Foreground="#1A1A2E"/>
    <TextElement Id="CardOneBody" X="24" Y="72" Width="292" Text="这里是卡片正文内容。" FontSize="L5" Foreground="#666666" LineHeight="1.4"/>
  </Panel>
</Panel>
""";

        var result = prompt.Replace("$(SlideWidth)", _documentContext.CanvasWidth.ToString())
            .Replace("$(SlideHeight)", _documentContext.CanvasHeight.ToString());
        return result;
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
