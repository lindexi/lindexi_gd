# 课件 Markdown 导出格式

`CoursewareMarkdownExporter` 用于将当前课件导出为便于调试、人工阅读和后续大模型处理的 Markdown 文件集合。

## 目录结构

导出目录格式如下：

```plaintext
CoursewareMarkdownExport_yyyyMMdd_HHmmss/
├── courseware.json
├── slides/
│   ├── slide_001.md
│   ├── slide_001.jpg
│   ├── slide_002.md
│   ├── slide_002.jpg
│   └── ...
└── resources/
    ├── img_1.png
    ├── img_2.jpg
    └── resources.json
```

## 文件说明

### courseware.json

`courseware.json` 是导出根目录下的总索引文件，用于描述本次导出的格式版本、课件名称、页面列表和资源索引位置。

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `exportVersion` | `number` | 导出格式版本。当前为 `1` |
| `createdAt` | `string` | 导出时间，ISO 8601 格式 |
| `coursewareName` | `string|null` | 课件名称 |
| `slideCount` | `number` | 页面数量 |
| `slides` | `array` | 页面导出信息列表 |
| `resourcesFile` | `string` | 资源索引文件相对路径 |

`slides` 中每一项字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `slideIndex` | `number` | 页面索引，0-based |
| `slideId` | `string` | 页面唯一标识 |
| `width` | `number` | 页面宽度 |
| `height` | `number` | 页面高度 |
| `markdownFile` | `string` | 页面 Markdown 文件相对路径 |
| `screenshotFile` | `string` | 页面截图文件相对路径 |

示例：

```json
{
  "exportVersion": 1,
  "createdAt": "2026-01-01T12:00:00+08:00",
  "coursewareName": "示例课件",
  "slideCount": 2,
  "slides": [
    {
      "slideIndex": 0,
      "slideId": "slide-id-1",
      "width": 1280,
      "height": 720,
      "markdownFile": "slides/slide_001.md",
      "screenshotFile": "slides/slide_001.jpg"
    }
  ],
  "resourcesFile": "resources/resources.json"
}
```

### slides/slide_XXX.md

每个页面对应一个 Markdown 文件，文件名按页面顺序递增：

```plaintext
slide_001.md
slide_002.md
slide_003.md
```

Markdown 文件包含页面基础信息和 `ReadableSlideInfoHelper.FormatSlideToMarkdown` 生成的页面内容。

格式如下：

```markdown
# Slide 1

- SlideIndex: 0
- SlideId: slide-id-1
- Size: 1280 x 720
- Screenshot: slide_001.jpg

---

页面 Markdown 内容
```

### slides/slide_XXX.jpg

每个页面对应一张普通整页截图，文件名与 Markdown 文件同名但扩展名为 `.jpg`。

截图用于人工对照 Markdown 内容，也可以作为后续大模型视觉输入。

### resources/resources.json

`resources.json` 是图片资源索引文件，记录 Markdown 中使用的图片资源 ID 和导出文件之间的关系。

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `imageId` | `string` | 图片资源 ID，例如 `img_1` |
| `sourceFileName` | `string` | 原始图片文件名，不包含完整本地路径 |
| `exportFile` | `string` | 导出到 `resources` 文件夹中的文件名，相对 `resources` 目录 |

示例：

```json
[
  {
    "imageId": "img_1",
    "sourceFileName": "picture.png",
    "exportFile": "img_1.png"
  }
]
```

### resources/img_N.ext

图片资源文件使用 `imageId + 原扩展名` 命名，例如：

```plaintext
img_1.png
img_2.jpg
```

如果原始资源没有扩展名，则直接使用 `imageId` 作为文件名。

## 兼容性约定

- `exportVersion` 用于标识导出格式版本，消费者应按版本解析。
- `courseware.json` 中的路径字段统一使用相对导出根目录的路径。
- `resources.json` 中的 `exportFile` 是相对 `resources` 目录的资源文件名。
- `resources.json` 不记录完整本地路径，只记录原始文件名，避免导出文件泄露本机路径信息。
- 页面文件名以 1-based 页码命名，`slideIndex` 字段仍使用 0-based 索引。
- 新增字段应保持向后兼容；消费者应忽略无法识别的字段。

## 当前限制

- 当前只导出普通整页截图，不导出去背景截图。
- 当前资源类型主要面向图片资源。
- 如果原始资源文件不存在，`resources.json` 仍会记录该资源，但对应资源文件不会被复制到 `resources` 目录。
- 当前不导出全局主题分析结果。
