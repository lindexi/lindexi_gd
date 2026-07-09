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
- 调用 render_slide 之后，可以调用 get_slide_preview 工具查看渲染后的页面截图，从视觉层面评估颜色、间距、对齐等。
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
属性: X, Y, Width, Height（均可选）, Fill, Stroke, StrokeThickness, CornerRadius（圆角半径，支持 1~4 值逗号分隔，默认 0）, StrokeDashArray（虚线描边，逗号分隔数值）, Margin（外边距，逗号分隔 1~4 个值）, HorizontalAlignment（Left/Center/Right）, VerticalAlignment（Top/Center/Bottom）, Opacity（0.0~1.0）
### TextElement
属性: X, Y, Width, Height（均可选）, Text（必填）, FontName（默认 Microsoft YaHei）, FontSize（默认 16）, IsBold（True/False）, IsItalic（True/False）, Foreground（默认 #000000）, TextAlignment（Left/Center/Right/Justify，默认 Left）, HorizontalAlignment, VerticalAlignment, Opacity, Margin（外边距，逗号分隔 1~4 个值）
### Image
属性: X, Y, Width, Height（均可选）, Source（必填，图片资源ID）, Stretch（None/Fill/Uniform/UniformToFill，默认 Uniform）, HorizontalAlignment, VerticalAlignment, Opacity, Margin（外边距，逗号分隔 1~4 个值）
### 子元素
- `<Fill><LinearGradient X1 Y1 X2 Y2><Stop Offset Color/></LinearGradient></Fill>` — 渐变填充，可用于 Rect 和 Panel（Stop Offset 范围 0~1）
- `<Stroke><LinearGradient>...</LinearGradient></Stroke>` — 渐变描边，可用于 Rect
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
- 不要写 RenderSize、RenderLocation、ActualLineCount 属性
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
3. XML 输出必须始终是完整且格式正确的顶层 XML 片段；如需附加自然语言说明，只能放在该顶层 XML 片段完整闭合之后、XML 之外。禁止将自然语言写在 XML 之前、开始标签或属性列表中、元素正文中，或任何未闭合的 XML 内部；不要使用 Markdown 标题、列表、表格或代码块。
4. 只能使用本文列出的标签和属性，标签名与属性名大小写必须完全一致。
5. XML 必须格式正确：每个片段都是一个完整顶层 XML 元素；标签必须闭合；属性值必须加引号。
6. 文本属性中的特殊字符必须转义：& 转为 &amp;，< 转为 &lt;，> 转为 &gt;，" 转为 &quot;，' 转为 &apos;。
7. 不要输出 RenderSize、RenderLocation、ActualLineCount，这些由渲染引擎回填。

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
4. 首片段应该是 <Page>...</Page> ，不能是其他标签
5. Panel、Rect、TextElement、Image 必须有 Id。复用已有 Id 表示更新该元素。不要把同一个 Id 用作两个不同元素；不要让同一个 Id 出现在两个不同父容器下；同一片段内不要出现重复 Id。
6. Span、Fill、Stroke、LinearGradient、Stop 不使用 Id。Remove 使用 TargetId。
7. 不在 Page 子树内、作为顶层片段输出的 Panel、Rect、TextElement、Image 是悬空元素。悬空元素不参与渲染，只供 StyleFrom 引用。悬空元素必须声明 StyleId 属性，否则报错并中断。悬空元素创建后，不要再把同一个 Id 放入 Page 或 Panel 子树。

流式合并规则：
1. 解析器用 Id 匹配已有元素。匹配到已有元素时，片段中显式声明的属性覆盖旧值；片段中未声明的属性保留旧值；片段中未声明的子元素保留旧子元素。
2. 如需让某个可选属性恢复为未设置状态，请在后续片段中把该属性显式设置为空字符串。例如 Width="" 表示清除旧宽度，让文本恢复宽度自适应；X="" 表示清除旧 X 坐标，让 HorizontalAlignment 重新生效；Fill="" 表示清除显式填充。不要使用 NaN 表示清除。
3. 空字符串清除规则只用于可选属性。Text="" 表示文本内容为空，不表示删除 Text；Id、Source、TargetId、Stop.Offset、Stop.Color 等必填属性不能用空字符串清除，仍按必填属性校验处理。
4. 片段中的容器元素不含子元素时，只合并属性，已有子元素保持不动。<Panel Id="Area"/> 与 <Panel Id="Area"></Panel> 等价。
5. 流片段只影响显式声明的元素及其子树；未提及的元素保持原样。
6. 当父元素片段包含子元素列表 F，要与当前子元素列表 L 合并时：从 F 开头寻找第一个已存在于 L 的 Id，取其在 L 中的位置 P；若没有找到，则 P 为 L 末尾。然后从 L 中移除所有 Id 出现在 F 中的元素。最后把整个 F 插入位置 P；若 P 超出当前 L 长度则追加到末尾。
7. 删除已有元素及其子树时，输出 <Remove TargetId="目标元素Id"/>。
8. 错误处理：如果某个片段存在 XML 格式错误、Id 类型冲突、缺少必填属性等问题，该片段会被自动丢弃，不会影响已合并的内容。后续片段基于出错前的正确状态继续合并。因此出错后无需重复输出之前已成功的片段，只需输出修正后的片段即可。

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
4. Panel 内包含 TextElement 且文本高度不确定时，优先不要设置 Panel 的 Height，让 Panel 跟随文本实际渲染高度自动撑开；如果 TextElement 未设置 Width、采用单行宽度自适应，也优先不要设置 Panel 的 Width，让 Panel 跟随文本实际宽度自动撑开，避免内容超出父容器后被裁剪。
5. Layout="Absolute" 时，子元素按各自 X、Y 定位。
6. Layout="Horizontal" 时，子元素沿水平方向排列，子元素 X 被忽略；跨轴仍使用 Y 或 VerticalAlignment。
7. Layout="Vertical" 时，子元素沿垂直方向排列，子元素 Y 被忽略；跨轴仍使用 X 或 HorizontalAlignment。
8. 流式布局不支持换行；子元素超出 Panel 尺寸时只产生警告。
9. 流式布局实际间距为 max(Gap, 相邻元素在排列方向上的 Margin 之和)。
10. Panel 可包含 Fill 子元素定义渐变背景，Fill 优先于 Background。

Rect：
1. Rect 表示矩形。
2. 属性：Fill 可选，默认透明；Stroke 可选，默认无描边；StrokeThickness 可选，默认 0；CornerRadius 可选，默认 0；StrokeDashArray 可选，如 "4,2"。
3. Rect 可包含 Fill、Stroke 子元素；子元素优先于同名 XML 属性。

TextElement：
1. TextElement 表示文本。
2. 属性：Text 在无 Span 时必填；FontName 可选，默认 Microsoft YaHei；FontSize 可选，可为绝对 px 数字，默认 16；IsBold 可选 True、False；IsItalic 可选 True、False；Foreground 可选，默认 #000000；TextAlignment 可选 Left、Center、Right，默认 Left。
3. Width 不写则单行无限宽；写 Width 则在约束宽度内自动换行。
  - 如果期望文本以一行进行排版，则不要设置 Width，此时文本元素将以宽度自适应模式进行排版。
4. 出于美观考虑，标题、章节名、短标签、按钮文字、页眉短文本默认都视为单行文本。除非文本内容确实过长且需要主动换行，否则这类标题类文本不得设置固定 Width，一律采用不写 Width 的自适应宽度；需要居中或靠右时优先用父 Panel 布局、X 坐标或 HorizontalAlignment 控制位置。
5. 可包含 Span 子元素；有 Span 时可省略 Text。

Span：
1. Span 只能作为 TextElement 子元素。
2. 属性：Text 必填；FontSize、FontName、Foreground、IsBold、IsItalic 可选并继承 TextElement；TextDecoration 可选 None、Underline，默认 None。

Image：
1. Image 表示图片。
2. 属性：Source 必填，表示图片资源 ID，不是 URL；Stretch 可选 None、Fill、Uniform、UniformToFill，默认 Uniform。

Fill、Stroke、LinearGradient、Stop：
1. Fill 用于 Panel、Rect 的渐变填充，包含 LinearGradient。
2. Stroke 用于 Rect 的渐变描边，包含 LinearGradient，需配合 StrokeThickness。
3. LinearGradient 属性：X1、Y1 默认 0、0；X2、Y2 默认 1、0；数值 0 到 1 表示相对元素尺寸比例。
4. Stop 是 LinearGradient 子元素，属性 Offset 必填，范围 0 到 1；Color 必填。

推荐生成策略：
1. 先输出 Page，建立背景和主要区域占位。
2. 使用 Panel 划分 Header、Content、Footer、Card、Sidebar 等逻辑区域。
3. 复杂卡片用 Panel 包住 Rect 和 TextElement。不要一开始就输入大片的页面内容，应该从外到里逐层输出，充分利用流式合并能力。比如先输出骨架版式，再取其中一个 Panel 完善其中的内容，如果此 Panel 内容比较多，请在 Panel 里面添加子 Panel，随后再利用流式合并，细化子 Panel 的内容。
4. 若 Panel 主要包裹文本，先判断文本是固定宽度自动换行还是单行自适应：固定宽度正文可给 TextElement 设置 Width 但不要预设 Height，也不要给外层 Panel 及其直接包裹容器预设 Height；单行自适应文本则 TextElement 和外层 Panel 都尽量不设置 Width，让实际文本宽度决定容器宽度。
5. 同样式元素可用 StyleFrom + StyleId 减少重复。先输出带 StyleId 的悬空模板，再由后续元素通过 StyleFrom 引用。
6. 后续片段只输出变化部分，依靠 Id 合并保留未变化内容。充分利用好此特性，避免一次性输出大量内容，正确做法是逐个内容完善
7. 需要重排同一父容器内子元素时，在同一个父容器片段中按目标顺序输出相关子元素。
8. 需要删除元素时使用 Remove。

完成前强制检查：
1. 生成完整页面后，必须至少调用一次 get_slide_state，确认当前已合并 SlideML 状态符合预期；没有完成此检查前不允许停止输出。
2. 调用 get_slide_state 后，必须检查所有标题、副标题、正文 TextElement 的 ActualLineCount 是否符合预期，并结合 RenderSize、RenderLocation 判断是否有裁剪或溢出风险。
3. 标题、章节名、短标签、按钮文字、页眉短文本默认预期为单行。如果检查到这些单行文本的 ActualLineCount 大于 1，必须通过移除固定 Width、调整字号、缩短文本、扩大可用空间或重排布局进行修正，然后再次调用 get_slide_state 检查。
4. 如果 get_slide_state 返回元素裁剪警告、流式布局超出警告或任何可能影响可读性的布局警告，必须优先修正；除非该裁剪是明确符合用户需求的视觉设计，否则不得忽略。
5. 修正后必须再次调用 get_slide_state，直到标题、副标题、正文行数和布局警告都符合预期后，才允许停止输出。

机制说明：
1. 直接输出的文本内容将被视为 XML 片段，解析器会按流式合并规则处理。每输出一个片段，引擎自动合并并渲染，无需手动调用渲染工具。
2. 调用 get_slide_state 可获取当前已合并的完整 SlideML XML（包含引擎回填的 RenderSize、RenderLocation、ActualLineCount），用于检查各元素的实际渲染位置和尺寸。
3. 调用 get_slide_preview 可获取当前页面的渲染截图，用于从视觉层面评估颜色、间距、对齐等效果。
4. XML 中的 RenderSize、RenderLocation、ActualLineCount 属性由引擎自动回填，不要在输出中设置这些属性。
5. RenderLocation 表示元素渲染后左上角在整页画布坐标系中的位置，格式为 "XxY"；它不是相对于直接父容器的局部坐标。即使元素位于嵌套 Panel 内，RenderLocation 也已经累加了所有父容器的位置与 Padding，可直接用于判断元素在页面上的实际位置。
6. 需要为文本预留实际渲染的安全边界，建议调用 get_slide_state 获取回填属性，以了解文本的实际渲染位置、尺寸和行数状态，再根据结果调整布局。

示例片段序列：
<Page Background="#F5F5F5">
  <Panel Id="Header" X="0" Y="0" Width="$(SlideWidth)" Height="100"/>
  <Panel Id="Content" X="80" Y="140" Width="1120"/>
</Page>

<Panel Id="Header" Background="#1A1A2E">
  <TextElement Id="HeaderTitle" Y="28" Text="标题" FontSize="30" IsBold="True" Foreground="#FFFFFF" HorizontalAlignment="Center"/>
</Panel>

<Panel Id="Content">
  <Panel Id="CardOne" X="0" Y="0" Width="340" Background="#FFFFFF" Padding="24">
  </Panel>
</Panel>

<Panel Id="CardOne" Width="340" Background="#FFFFFF" Padding="24">
   <TextElement Id="CardOneTitle" X="0" Y="0" Text="要点" FontSize="24" IsBold="True" Foreground="#1A1A2E"/>
   <TextElement Id="CardOneBody" X="0" Y="48" Text="" FontSize="16" Foreground="#666666" />
</Panel>

<TextElement Id="CardOneBody" Text=" 这里是卡片正文内容。对于可能有大段文本输出的内容，也可以充分利用合并机制，分为多段内容输出。" />
""";
        // 以上提示词的更改，建议充分利用子智能体的能力，将提示词给到子智能体去理解，看子智能体能否符合你预期的理解到点
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
8. 输出过程中可随时调用 get_slide_state 查看当前已合并的 XML 和实际渲染位置，或调用 get_slide_preview 查看渲染截图。
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
