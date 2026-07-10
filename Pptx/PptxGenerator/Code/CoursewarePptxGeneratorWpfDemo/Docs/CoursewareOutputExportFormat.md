# 课件生成结果保存导出格式

本文定义 `CoursewarePptxGeneratorWpfDemo` 在完成课件加载、全局主题分析、逐页生成和渲染后，保存生成结果时使用的本地导出目录格式。

该格式面向以下目标：

- 保留原始课件 Markdown 导出目录的输入信息，不修改原始输入文件。
- 保存全局主题分析结果、逐页 SlideML、渲染预览图和必要诊断信息。
- 支持后续重新打开生成结果、继续编辑、重新渲染、批量导出 PPTX 或进行一致性校验。
- 避免在保存结果中泄露本机绝对路径、访问令牌或其他敏感信息。

---

## 一、设计原则

### 1.1 输入与输出分离

生成结果不应写回 `CoursewareMarkdownExport_yyyyMMdd_HHmmss` 原始导出目录中的 Markdown、截图或资源文件。

保存导出时由用户选择输出文件夹。输出文件夹可以位于原始导出目录旁边，也可以位于用户指定的其他位置，例如：

```plaintext
CoursewareOutput_20260101_121000/
└── ...
```

输出目录名称不作为格式的一部分，由用户选择或由程序按默认规则生成。

### 1.2 保存可继续编辑的中间结果

保存导出的主要目标不是只输出最终 PPTX，而是保存可继续处理的中间工程结果。

因此导出目录应至少包含：

- 每页生成后的 SlideML。
- 每页渲染后的预览截图。
- 每页生成状态、错误信息和渲染诊断。
- 全局主题分析结果。
- 资源引用和输入目录快照信息。

### 1.3 路径使用相对路径

所有索引文件中的路径字段统一使用相对生成结果根目录的路径。

如果需要引用原始输入目录中的文件，应优先记录相对原始课件根目录的路径，例如：

```json
"SourceMarkdownFile": "Slides/Slide_000/SourceMarkdown.md"
```

路径写入 JSON 时统一使用 `/` 作为分隔符。为了方便阅读和与 C# 模型命名保持一致，生成结果中的 JSON 属性、路径、文件夹名和文件名统一使用帕斯卡命名，文件扩展名保持小写。

### 1.4 版本化与兼容

生成结果格式使用独立的 `ExportVersion` 字段标识版本。

当前版本为 `1.0.0`。后续新增字段应保持向后兼容，读取端应忽略不能识别的字段。

---

## 二、目录结构

生成结果导出目录格式如下：

```plaintext
CoursewareOutput_20260101_121000/
├── CoursewareOutput.json
├── Theme/
│   ├── Theme.json
│   └── ThemeSummary.md
├── Slides/
│   ├── Slide_000/
│   │   ├── Slide.slideml
│   │   ├── Preview.png
│   │   ├── Generation.md
│   │   ├── RenderLog.txt
│   │   ├── SlideResult.json
│   │   └── SourceMarkdown.md
│   ├── Slide_001/
│   │   ├── Slide.slideml
│   │   ├── Preview.png
│   │   ├── Generation.md
│   │   ├── RenderLog.txt
│   │   ├── SlideResult.json
│   │   └── SourceMarkdown.md
│   └── ...
└── Summary.md
```

其中：

- `CoursewareOutput.json` 是生成结果总索引文件。
- `Theme/` 保存全局主题分析结果。
- `Slides/` 保存逐页生成和渲染结果。
- `Summary.md` 保存本次导出的可读摘要。

---

## 三、总索引文件 CoursewareOutput.json

`CoursewareOutput.json` 是生成结果根目录下的总索引文件，用于描述本次生成结果的格式版本、输入课件信息、主题分析文件、页面结果列表和输出状态。

### 3.1 字段说明

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `ExportVersion` | `string` | 生成结果导出格式版本。当前为 `1.0.0` |
| `CreatedAt` | `string` | 首次创建时间，ISO 8601 格式 |
| `UpdatedAt` | `string` | 最近更新时间，ISO 8601 格式 |
| `SourceExportVersion` | `number|null` | 原始课件 Markdown 导出格式版本 |
| `CoursewareName` | `string` | 课件名称 |
| `SourceRoot` | `string|null` | 原始课件目录相对引用 |
| `SourceManifestFile` | `string` | 原始 `courseware.json` 相对路径 |
| `SlideCount` | `number` | 页面数量 |
| `ThemeFile` | `string|null` | 全局主题分析结果文件路径 |
| `ThemeSummaryFile` | `string|null` | 全局主题摘要 Markdown 文件路径 |
| `Slides` | `array` | 页面生成结果列表 |
| `SummaryFile` | `string|null` | 导出摘要文件路径 |
| `Status` | `string` | 整体状态 |

`Status` 推荐取值：

| 值 | 说明 |
| --- | --- |
| `Created` | 已创建结果目录，但尚未开始生成 |
| `ThemeAnalyzed` | 已完成全局主题分析 |
| `Generating` | 正在逐页生成 |
| `PartiallyGenerated` | 部分页面已生成，部分页面失败或未生成 |
| `Generated` | 所有页面均已生成 SlideML |
| `Rendered` | 所有页面均已完成渲染预览 |
| `Failed` | 整体流程失败 |

### 3.2 slides 字段说明

`Slides` 中每一项表示一页的生成结果索引。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `SlideIndex` | `number` | 页面索引，0-based |
| `SlideId` | `string` | 原始页面唯一标识 |
| `Width` | `number` | 页面宽度 |
| `Height` | `number` | 页面高度 |
| `SourceMarkdownFile` | `string|null` | 可选复制的原始页面 Markdown 文件路径，相对生成结果根目录 |
| `SourceScreenshotFile` | `string|null` | 原始截图文件路径，相对原始课件根目录 |
| `SlideResultFile` | `string` | 页面结果索引文件路径，相对生成结果根目录 |
| `SlideMlFile` | `string|null` | 生成后的 SlideML 文件路径 |
| `PreviewFile` | `string|null` | 渲染预览图路径 |
| `GenerationFile` | `string|null` | 生成过程摘要文件路径 |
| `RenderLogFile` | `string|null` | 渲染日志文件路径 |
| `Status` | `string` | 页面状态 |
| `WarningCount` | `number` | 页面警告数量 |
| `ErrorMessage` | `string|null` | 页面失败原因 |

页面 `Status` 推荐取值：

| 值 | 说明 |
| --- | --- |
| `NotStarted` | 尚未开始生成 |
| `Generating` | 正在生成 SlideML |
| `Generated` | 已生成 SlideML |
| `Rendering` | 正在渲染预览 |
| `Rendered` | 已完成渲染预览 |
| `Failed` | 页面生成或渲染失败 |
| `Skipped` | 页面被用户跳过 |

### 3.3 示例

```json
{
  "ExportVersion": "1.0.0",
  "CreatedAt": "2026-01-01T12:10:00+08:00",
  "UpdatedAt": "2026-01-01T12:20:00+08:00",
  "SourceExportVersion": 1,
  "CoursewareName": "示例课件",
  "SourceRoot": "..",
  "SourceManifestFile": "../courseware.json",
  "SlideCount": 2,
  "ThemeFile": "Theme/Theme.json",
  "ThemeSummaryFile": "Theme/ThemeSummary.md",
  "Slides": [
    {
      "SlideIndex": 0,
      "SlideId": "slide-id-1",
      "Width": 1280,
      "Height": 720,
      "SourceMarkdownFile": "Slides/Slide_000/SourceMarkdown.md",
      "SourceScreenshotFile": "slides/slide_001.jpg",
      "SlideResultFile": "Slides/Slide_000/SlideResult.json",
      "SlideMlFile": "Slides/Slide_000/Slide.slideml",
      "PreviewFile": "Slides/Slide_000/Preview.png",
      "GenerationFile": "Slides/Slide_000/Generation.md",
      "RenderLogFile": "Slides/Slide_000/RenderLog.txt",
      "Status": "Rendered",
      "WarningCount": 0,
      "ErrorMessage": null
    }
  ],
  "SummaryFile": "Summary.md",
  "Status": "PartiallyGenerated"
}
```

保存时应尝试把已打开课件中的原始页面 Markdown 复制到对应页面目录中，并通过 `SourceMarkdownFile` 记录复制后的相对路径。该复制结果是可选内容；如果源文件不存在、复制失败或用户选择不复制，则 `SourceMarkdownFile` 写入 `null`，不应阻止保存生成结果。

---

## 四、全局主题目录 Theme

### 4.1 Theme/Theme.json

`Theme.json` 保存全课件主题分析结果，用于后续逐页美化、重新生成和一致性校验。

建议字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `SchemaVersion` | `string` | 主题结果结构版本。当前为 `1.0.0` |
| `AnalyzedAt` | `string` | 主题分析时间，ISO 8601 格式 |
| `CoursewareName` | `string` | 课件名称 |
| `Summary` | `string` | 主题摘要 |
| `StyleKeywords` | `array` | 风格关键词 |
| `PrimaryColors` | `array` | 主色列表 |
| `AccentColors` | `array` | 强调色列表 |
| `BackgroundColors` | `array` | 背景色列表 |
| `FontSuggestions` | `array` | 字体建议 |
| `LayoutPrinciples` | `array` | 版式原则 |
| `SafeArea` | `object|null` | 安全区建议 |
| `GenerationPromptSummary` | `string|null` | 用于逐页生成的主题提示摘要 |

颜色统一使用十六进制字符串，支持 `#AARRGGBB` 或 `#RRGGBB` 格式，例如 `#FF2563EB` 或 `#2563EB`。

`SafeArea` 建议字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Left` | `number` | 左侧安全边距 |
| `Top` | `number` | 顶部安全边距 |
| `Right` | `number` | 右侧安全边距 |
| `Bottom` | `number` | 底部安全边距 |
| `Unit` | `string` | 单位，例如 `px` |

### 4.2 Theme/ThemeSummary.md

`ThemeSummary.md` 是给用户阅读的主题摘要文档。

建议包含：

- 课件整体风格判断。
- 颜色和字体建议。
- 页面布局建议。
- 后续逐页生成需要遵守的规则。
- 主题分析过程中的不确定点或警告。

该文件只作为可读摘要，不作为机器读取的唯一数据源。机器读取应以 `Theme.json` 为准。

---

## 五、单页结果目录 Slides/Slide_XXX

每页生成结果保存到独立目录中，目录名使用 0-based 页面索引：

```plaintext
Slides/Slide_000/
Slides/Slide_001/
Slides/Slide_002/
```

这种结构便于单页重新生成、单页重新渲染和排查问题。

### 5.1 Slide.slideml

`Slide.slideml` 保存当前页生成后的 SlideML 内容。

要求：

- 文件内容为完整可渲染的 SlideML。
- 不包含本机绝对路径。
- 图片引用优先使用相对原始课件根目录的资源路径，例如 `resources/img_1.png`。
- 如果后续支持资源 ID，也应在渲染前能解析到具体资源文件。

### 5.2 Preview.png

`Preview.png` 保存当前页 SlideML 渲染后的预览图。

要求：

- 推荐使用 PNG，避免二次压缩影响人工检查。
- 图片尺寸应与页面尺寸一致，例如 1280 x 720。
- 渲染失败时可以不生成该文件，但应在 `SlideResult.json` 中记录失败状态和错误信息。

### 5.3 Generation.md

`Generation.md` 保存当前页生成过程的可读摘要。

建议包含：

- 页面标题和摘要。
- 使用的全局主题摘要。
- 输入 Markdown 摘要。
- 资源引用摘要。
- 生成出的 SlideML 说明。
- 模型返回的主要说明或警告。

不建议在该文件中保存完整敏感配置、访问令牌或本机绝对路径。

### 5.4 RenderLog.txt

`RenderLog.txt` 保存当前页渲染日志。

建议包含：

- 渲染开始和结束时间。
- 使用的渲染方式，例如本地 WPF 渲染或 MCP 渲染。
- 渲染警告。
- 渲染失败的错误摘要。

### 5.5 SlideResult.json

`SlideResult.json` 是单页结果索引文件，用于恢复单页状态。

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `SlideIndex` | `number` | 页面索引，0-based |
| `SlideId` | `string` | 原始页面唯一标识 |
| `UpdatedAt` | `string` | 最近更新时间，ISO 8601 格式 |
| `Status` | `string` | 页面状态 |
| `SourceMarkdownFile` | `string|null` | 可选复制的原始 Markdown 文件路径，相对生成结果根目录 |
| `SourceScreenshotFile` | `string|null` | 原始截图文件路径，相对原始课件根目录 |
| `SlideMlFile` | `string|null` | SlideML 文件路径，相对单页目录或结果根目录 |
| `PreviewFile` | `string|null` | 预览图路径，相对单页目录或结果根目录 |
| `GenerationFile` | `string|null` | 生成摘要文件路径 |
| `RenderLogFile` | `string|null` | 渲染日志文件路径 |
| `Warnings` | `array` | 页面警告列表 |
| `ErrorMessage` | `string|null` | 页面失败原因 |
| `ModelInfo` | `object|null` | 生成模型摘要信息 |

`ModelInfo` 只记录用于诊断的非敏感信息，例如：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Provider` | `string|null` | 模型提供方名称 |
| `Model` | `string|null` | 模型名称 |
| `PromptVersion` | `string|null` | 提示词版本 |

不应记录 API Key、完整连接字符串或其他敏感认证信息。

示例：

```json
{
  "SlideIndex": 0,
  "SlideId": "slide-id-1",
  "UpdatedAt": "2026-01-01T12:20:00+08:00",
  "Status": "Rendered",
  "SourceMarkdownFile": "SourceMarkdown.md",
  "SourceScreenshotFile": "slides/slide_001.jpg",
  "SlideMlFile": "Slide.slideml",
  "PreviewFile": "Preview.png",
  "GenerationFile": "Generation.md",
  "RenderLogFile": "RenderLog.txt",
  "Warnings": [],
  "ErrorMessage": null,
  "ModelInfo": {
    "Provider": "AzureOpenAI",
    "Model": "example-model",
    "PromptVersion": "courseware-slide-v1"
  }
}
```

---

## 六、导出摘要 Summary.md

`Summary.md` 保存一次生成流程的总体可读摘要。

建议包含：

- 课件名称和页面数量。
- 生成开始和结束时间。
- 成功、失败、跳过的页面数量。
- 全局警告摘要。
- 后续建议操作，例如重新生成失败页或检查缺失资源。

---

## 七、保存时机

### 7.1 自动保存

推荐在以下节点自动保存：

1. 创建输出目录后，写入初始 `CoursewareOutput.json`。
2. 全局主题分析完成后，写入 `Theme/Theme.json` 和 `Theme/ThemeSummary.md`。
3. 单页 SlideML 生成成功后，立即写入 `Slide.slideml` 和 `SlideResult.json`。
4. 单页渲染完成后，立即写入 `Preview.png`、`RenderLog.txt` 和 `SlideResult.json`。
5. 页面失败时，立即写入错误状态，避免进程退出后丢失诊断信息。

### 7.2 手动保存

手动保存命令应刷新以下文件：

- `CoursewareOutput.json`
- 当前页 `SlideResult.json`
- 当前页 `Slide.slideml`
- 当前页 `Generation.md`
- `Summary.md`

如果用户只修改当前页 SlideML，手动保存不应重写其他页面的结果文件。

---

## 八、路径和安全约定

### 8.1 相对路径基准

索引文件中路径基准如下：

| 文件 | 路径字段基准 |
| --- | --- |
| `CoursewareOutput.json` | 生成结果根目录 |
| `Theme/Theme.json` | 生成结果根目录 |
| `Slides/Slide_000/SlideResult.json` | 优先使用单页目录，也可使用生成结果根目录，但同一版本内应保持一致 |
| `Summary.md` | 生成结果根目录 |

第一版建议：

- 总索引使用相对生成结果根目录的路径。
- 单页索引中的 `SlideMlFile`、`PreviewFile`、`GenerationFile`、`RenderLogFile` 使用相对单页目录的文件名。
- 可选复制到生成结果目录的输入文件路径使用相对生成结果根目录的路径；未复制的原始输入引用使用相对原始课件根目录的路径。

### 8.2 禁止保存的内容

保存导出结果时，不应写入：

- 本机磁盘绝对路径。
- API Key、访问令牌、连接字符串。
- 用户账号、密码或认证 Cookie。
- 完整模型请求头。
- 与课件内容无关的本机环境信息。

### 8.3 路径校验

读取生成结果时，应对所有相对路径执行规范化和越界校验。

建议规则：

- 生成结果文件必须位于生成结果根目录下。
- 原始输入引用必须位于原始课件根目录下。
- 出现 `../` 时应先规范化，再判断是否仍在允许目录内。
- 路径越界应作为阻塞错误处理。

---

## 九、与原始 Markdown 导出格式的关系

原始课件 Markdown 导出格式负责保存输入：

```plaintext
courseware.json
slides/slide_001.md
slides/slide_001.jpg
resources/resources.json
resources/img_1.png
```

生成结果保存导出格式负责保存输出：

```plaintext
CoursewareOutput_20260101_121000/CoursewareOutput.json
CoursewareOutput_20260101_121000/Theme/Theme.json
CoursewareOutput_20260101_121000/Slides/Slide_000/Slide.slideml
CoursewareOutput_20260101_121000/Slides/Slide_000/Preview.png
```

两者关系如下：

| 原始导出 | 生成结果 |
| --- | --- |
| `courseware.json` | `CoursewareOutput.json` 中的 `SourceManifestFile` |
| `slides/slide_001.md` | 可选复制为 `SourceMarkdownFile` 指向的 `SourceMarkdown.md` |
| `slides/slide_001.jpg` | `SourceScreenshotFile` |
| `resources/resources.json` | SlideML 资源引用和后续渲染资源解析依据 |
| 无 | `Theme/Theme.json` |
| 无 | `Slides/Slide_000/Slide.slideml` |
| 无 | `Slides/Slide_000/Preview.png` |

生成结果可以依赖原始导出目录继续存在。第一版只建议可选复制每页 Markdown 作为 `SourceMarkdown.md`，不建议复制原始截图和资源文件，避免输出目录体积过大。

如果后续需要生成可独立分发的完整包，可以增加 `portable` 模式，将原始输入文件复制到生成结果目录中，并在索引中标记资源已内嵌。

---

## 十、兼容性约定

- `ExportVersion` 用于标识生成结果导出格式版本，当前为 `1.0.0`。
- 读取端应忽略无法识别的新增字段。
- 写入端应尽量保留读取到但自身不理解的字段，避免破坏未来版本数据。
- 页面目录名和页面索引字段均使用 0-based 页面索引。
- `SlideId` 来自原始导出目录，不应在保存生成结果时重新生成。
- `SlideML` 文件扩展名统一使用 `.slideml`。
- 渲染预览图第一版统一使用 `.png`。
- 日志和摘要文件优先使用 UTF-8 编码。

---

## 十一、当前限制

第一版保存导出格式不覆盖以下能力：

- 不保存完整模型请求和响应原文。
- 不保存用户的模型认证配置。
- 不保证输出目录脱离原始课件导出目录后仍可完整恢复。
- 不定义最终 PPTX 内部结构。
- 不定义自动返工、一致性评分和多版本对比格式。
- 不定义多人协作冲突合并规则。

这些能力可在后续版本中通过新增字段和新增目录扩展。

---

## 十二、建议第一版落地范围

第一版建议优先实现以下最小闭环：

1. 让用户选择输出目录，或按默认规则创建输出目录。
2. 写入 `CoursewareOutput.json`。
3. 保存 `Theme/Theme.json` 和 `Theme/ThemeSummary.md`。
4. 每页保存 `Slide.slideml`。
5. 每页保存 `Preview.png`。
6. 每页保存 `SlideResult.json`。
7. 保存 `Summary.md`。

暂缓实现：

- 完整模型对话归档。
- 可独立分发的 portable 输出包。
- 多版本美化结果对比。

该范围可以先支持“加载原始导出目录 -> 生成 SlideML -> 渲染预览 -> 保存结果 -> 下次继续打开或排查”的核心工作流。
