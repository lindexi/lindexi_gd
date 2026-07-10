# 课件 Markdown 导出格式

课件 Markdown 导出结果是一组便于人工阅读和后续大模型处理的文件，包括课件索引、页面 Markdown、页面截图和课件中使用的素材。

当前导出格式版本为 `1`。

## 目录结构

典型的导出目录结构如下：

```plaintext
CoursewareMarkdownExportyyyyMMddHHmmss/
├── Courseware.json
├── Slides/
│   ├── Slide000.md
│   ├── Slide000.jpg
│   ├── Slide001.md
│   ├── Slide001.jpg
│   ├── Slide002.md
│   ├── Slide002.jpg
│   └── ...
└── Resources/
    ├── img_1.png
    ├── audio_1.mp3
    ├── video_1.mp4
    └── Resources.json
```

各部分内容如下：

- `Courseware.json`：课件总索引。
- `Slides/SlideXXX.md`：页面内容。
- `Slides/SlideXXX.jpg`：页面截图。
- `Resources/Resources.json`：素材索引。
- `Resources/{ResourceId}.ext`：图片、音频和视频素材。

## Courseware.json

`Courseware.json` 位于导出根目录，用于描述格式版本、课件名称、页面列表和资源索引位置。

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `ExportVersion` | `number` | 导出格式版本。当前为 `1` |
| `CreatedAt` | `string` | 导出时间，ISO 8601 格式 |
| `CoursewareName` | `string|null` | 课件名称 |
| `SlideCount` | `number` | 页面数量 |
| `Slides` | `array` | 页面导出信息列表 |
| `ResourcesFile` | `string` | 资源索引文件相对路径 |

`Slides` 中每一项的字段如下：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `SlideIndex` | `number` | 页面索引，0-based |
| `SlideId` | `string` | 页面唯一标识 |
| `Width` | `number` | 页面宽度 |
| `Height` | `number` | 页面高度 |
| `MarkdownFile` | `string` | 页面 Markdown 文件相对路径 |
| `ScreenshotFile` | `string` | 页面截图文件相对路径 |

示例：

```json
{
  "ExportVersion": 1,
  "CreatedAt": "2026-01-01T12:00:00+08:00",
  "CoursewareName": "示例课件",
  "SlideCount": 2,
  "Slides": [
    {
      "SlideIndex": 0,
      "SlideId": "slide-id-1",
      "Width": 1280,
      "Height": 720,
      "MarkdownFile": "Slides/Slide000.md",
      "ScreenshotFile": "Slides/Slide000.jpg"
    }
  ],
  "ResourcesFile": "Resources/Resources.json"
}
```

清单中的路径是相对于导出根目录的路径，不带前导 `/`，不包含 `.` 或 `..` 路径片段，并统一使用 `/` 作为路径分隔符。

## 页面文件

### Slides/SlideXXX.md

每个页面对应一个 Markdown 文件，文件名按页面顺序递增：

```plaintext
Slide000.md
Slide001.md
Slide002.md
```

文件名中的数字使用 0-based 页面索引，并按 `D3` 格式保留至少三位数字。超过 999 页时自然扩展，例如索引 1000 对应 `Slide1000.md`。

页面 Markdown 最多包含三个二级章节，章节之间使用 `---` 分隔：

1. `## 页面信息`
2. `## 元素简要信息`
3. `## 元素细节`，仅当页面存在元素时生成

完整结构示例如下：

````markdown
## 页面信息

- Id: slide-id-1
- 尺寸: 1280×720
- 序号(1-base): 1
- 背景图片: img_1

---

## 元素简要信息

- 文本.1: (100, 80) 400×60
- 图片.1: (100, 180) 320×180
  - Source: img_2

---

## 元素细节

### 文本.1
字号: 32px | 字体: Microsoft YaHei
#### 内容
```
页面文本内容
```
````

#### 页面信息

页面信息字段按以下顺序输出：

| 字段 | 是否必有 | 说明 |
| --- | --- | --- |
| `Id` | 是 | 页面唯一标识 |
| `尺寸` | 是 | 页面宽高，转换为整数后使用 `宽×高` 格式 |
| `序号(1-base)` | 是 | 页面在当前课件中的 1-based 序号，与 `Courseware.json` 中 0-based 的 `SlideIndex` 不同 |
| `背景图片` | 否 | 图片背景对应的图片素材 ID，例如 `img_1` |
| `背景颜色` | 否 | 纯色背景的 `#AARRGGBB` 值，例如 `#FFFFFFFF` |

背景图片和背景颜色根据实际背景类型择一输出。无法识别为已收集图片素材或纯色画刷的背景，不会产生对应字段。

#### 元素简要信息

元素简要信息按页面元素树顺序输出，每个元素使用以下格式：

```markdown
- {可读元素名}: ({X}, {Y}) {Width}×{Height}
```

其中：

- 坐标原点位于页面左上角，`X`、`Y`、`Width`、`Height` 均转换为整数输出。
- 坐标和尺寸使用页面坐标系。组合内子元素也输出相对于页面的绝对位置，而不是相对于组合的局部位置。
- 组合元素使用缩进列表表示层级，每深入一级增加两个空格。
- 页面没有元素时输出 `(无元素)`，且不输出 `## 元素细节` 章节。

##### 元素可读名称

元素名称采用 `{类型}.{序号}` 格式，例如 `文本.1`、`图片.2`、`组合.1`。序号在单个页面内按归并后的类型独立递增，从 `1` 开始。

常见类型名称如下：

| 类型名称 | 包含的典型元素 |
| --- | --- |
| `文本` | 文本元素 |
| `图片` | 图片元素 |
| `形状` | 形状元素 |
| `组合` | 组合元素 |
| `音频` | 音频元素 |
| `视频` | 视频元素 |
| `表格` | 表格元素 |

其他业务元素同样使用中文类型名称加序号。多个底层元素类型可能归并为同一个可读类型并共享序号空间。

##### 图片、音频和视频引用

图片、音频和视频在元素行下方通过素材 ID 引用 `Resources.json` 中的资源，不输出原始文件名或本机路径。

图片：

```markdown
- 图片.1: (100, 180) 320×180
  - Source: img_2
```

音频：

```markdown
- 音频.1: (100, 120) 240×48
  - Source: audio_1
  - Play: AutoPlay | Loop | CrossSlidePlay
```

视频：

```markdown
- 视频.1: (100, 120) 480×270
  - Source: video_1
  - Thumbnail: img_3
```

字段说明：

| 字段 | 适用元素 | 说明 |
| --- | --- | --- |
| `Source` | 图片、音频、视频 | 元素主体素材 ID。只有素材路径已经登记到课件素材目录时才输出 |
| `Thumbnail` | 视频 | 视频缩略图对应的图片素材 ID；没有缩略图或未登记时省略 |
| `Play` | 音频 | 将值为真的播放行为按 `AutoPlay`、`Loop`、`CrossSlidePlay` 顺序使用竖线分隔；没有这些行为时省略 |

视频的播放行为不放在简要信息中，而是在有内容时放入该视频的元素细节。

#### 元素细节

元素细节按页面元素树的深度优先顺序生成，组合元素本身和组合内子元素都会参与遍历。只有存在可读内容的元素才输出对应的 `### {可读元素名}` 章节；图片、形状、组合等没有额外可读内容的元素通常只出现在简要信息中。

有内容的元素之后继续处理后续元素时，使用 `-------` 分隔细节内容。没有可读内容的元素不会生成三级标题，因此在包含组合和其他无细节元素的页面中，分隔线不一定紧邻两个三级标题。

当前支持的细节内容包括文本类内容、表格和视频播放信息。

##### 文本类元素

能够提取富文本信息的元素使用以下格式：

````markdown
### 文本.1
字号: 42px | 字体: 黑体
#### 内容
```
任务一·提取信息·理清思路
```
````

- `字号` 和 `字体` 来自元素的主要字体信息；无法取得有效字号时不输出该行。
- 只有存在非空文本时才输出 `#### 内容`。
- 文本内容放在无语言标记的 Markdown 代码块中，保留原始换行，避免正文被误解析为 Markdown 标题或列表。
- 如果内容某一行的行首已经包含三个或更多连续反引号，外层代码块会自动改用更多反引号，保证代码块边界不冲突。

##### 表格元素

表格细节输出为标准 Markdown 表格，原表格第一行作为表头：

```markdown
### 表格.1
| 姓名 | 分数 |
| --- | --- |
| 小明 | 95 |
| 小红 | 98 |
```

单元格中的 `|` 会转义为 `\|`，LF 换行符 `\n` 会替换为空格，以避免破坏 Markdown 表格结构。

##### 视频元素

视频只在至少存在一项播放行为、停止页码或播放打点时输出细节：

```markdown
### 视频.1
- Play: AutoPlay | CrossSlidePlay
- StopPlayPageNumber: 3
- PlayPoints:
  - 00:01:00
  - 00:02:00.500
```

字段说明：

| 字段 | 输出条件 | 说明 |
| --- | --- | --- |
| `Play` | 至少启用一种播放行为 | 与音频相同，按 `AutoPlay`、`Loop`、`CrossSlidePlay` 顺序合并 |
| `StopPlayPageNumber` | 开启跨页播放，或停止页码不是默认值 `0` | 视频跨页播放的停止页码，直接输出课件中保存的值 |
| `PlayPoints` | 至少存在一个播放打点 | 按时间升序输出 |

播放打点使用 `hh:mm:ss` 格式；存在毫秒时使用 `hh:mm:ss.fff`。小时部分可超过 `23`。

### Slides/SlideXXX.jpg

每个页面对应一张普通整页截图，文件名与 Markdown 文件同名，扩展名为 `.jpg`。

截图用于人工对照 Markdown 内容，也可以作为后续大模型视觉输入。

## 资源文件

### Resources/Resources.json

`Resources.json` 是统一的素材索引，记录 Markdown 中使用的图片、页面背景、音频、视频和视频缩略图资源 ID 与导出文件之间的关系。

视频缩略图作为 `image` 类型资源登记，通过页面 Markdown 中视频元素的 `Thumbnail` 字段与视频关联。`Resources.json` 不额外记录缩略图和视频之间的关系。

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `ResourceId` | `string` | 素材 ID，例如 `img_1`、`audio_1`、`video_1` |
| `ResourceType` | `string` | 素材类型：`image`、`audio` 或 `video` |
| `ExportFile` | `string` | 导出到 `Resources` 文件夹中的文件名，相对 `Resources` 目录 |

示例：

```json
[
  {
    "ResourceId": "img_1",
    "ResourceType": "image",
    "ExportFile": "img_1.png"
  },
  {
    "ResourceId": "video_1",
    "ResourceType": "video",
    "ExportFile": "video_1.mp4"
  }
]
```

图片、音频和视频分别使用 `img_N`、`audio_N`、`video_N` 形式的素材 ID。图片编号同时包含普通图片、页面背景和视频缩略图。

### 素材引用

页面 Markdown 中的 `背景图片`、`Source` 和 `Thumbnail` 值都是素材 ID。可以按以下步骤找到对应素材：

1. 从 `Courseware.json` 的 `ResourcesFile` 找到 `Resources/Resources.json`。
2. 在资源索引中按 `ResourceId` 查找对应项。
3. 将该项的 `ExportFile` 解释为相对于 `Resources` 目录的文件名。
4. 根据 `ResourceType` 判断资源是 `image`、`audio` 还是 `video`。

例如页面 Markdown 中：

```markdown
- 背景图片: img_1
- 图片.1: (100, 180) 320×180
  - Source: img_2
```

资源索引中：

```json
[
  {
    "ResourceId": "img_1",
    "ResourceType": "image",
    "ExportFile": "img_1.png"
  },
  {
    "ResourceId": "img_2",
    "ResourceType": "image",
    "ExportFile": "img_2.png"
  }
]
```

则背景图片文件为 `Resources/img_1.png`，`图片.1` 的主体文件为 `Resources/img_2.png`。

### Resources/{ResourceId}.ext

素材文件使用 `ResourceId + 原扩展名` 命名，原扩展名会保留大小写，例如：

```plaintext
img_1.png
audio_1.mp3
video_1.mp4
```

如果原始资源没有扩展名，则直接使用 `ResourceId` 作为文件名。

## 兼容性约定

- `ExportVersion` 用于标识导出格式版本，消费者应按版本解析。当前为 `1`。
- 页面文件名和 `SlideIndex` 均使用 0-based 索引，例如第一页对应 `Slide000.md` 和 `Slide000.jpg`。
- `Courseware.json` 中的路径字段是相对于导出根目录的路径，不带前导 `/`，不包含 `.` 或 `..` 路径片段，并固定使用 `/` 作为路径分隔符。
- `Resources.json` 中的 `ExportFile` 是相对于 `Resources` 目录的资源文件名。
- `Resources.json` 不记录原始资源路径。