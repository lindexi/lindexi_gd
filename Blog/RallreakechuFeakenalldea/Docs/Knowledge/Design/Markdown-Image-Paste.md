# Markdown 图片粘贴与本地保存

## 场景

在 `SimpleWrite` 中编辑 Markdown 文档时，用户通过 `Ctrl+V` 粘贴剪贴板中的图片。编辑器需要：

1. 从剪贴板读取图片数据（包括位图数据和图片文件两种来源）；
2. 将图片保存到 Markdown 文件同级目录下，保留原始格式；
3. 在光标位置插入 Markdown 图片引用语法。

默认保存路径模板为 `image/{FileName}/{FileName}{Index}.{Extension}`，例如 `image/MyPost/MyPost0.png`，多张图片从 `0` 递增到 `N`。模板可由用户自定义。

## 设计原则

- **策略拥有完整粘贴能力**：策略不仅处理图片，而是接管整个粘贴流程。由策略自行决定是保存图片还是粘贴纯文本，并自行完成文件保存和文本插入。`SimpleWriteTextEditorHandler` 只负责委托，不感知任何文档类型细节。
- **保存路径模板可定制**：通过占位符模板描述路径规则，默认值合理但可替换，不写死固定格式。
- **未保存文档时静默跳过**：文档尚未保存到本地时，不提示、不报错，图片粘贴不生效，但纯文本粘贴仍正常工作。
- **两种剪贴板图片来源都处理**：位图数据（截图工具产生的 `image/png` 等）和图片文件引用（从文件资源管理器复制的图片文件）。
- **保留原始格式**：不强制统一为 PNG，原始是什么格式就保存为什么后缀。

## 整体思路

当前 `SimpleWriteTextEditorHandler.OnPaste()` 直接调用 `base.OnPaste()`，基座实现仅从剪贴板读取纯文本。改为通过粘贴策略接管整个 `OnPaste` 流程：策略自行检查剪贴板内容，如果是图片就保存文件并插入引用，如果是文本就插入文本。Handler 不做任何业务判断。

```
SimpleWriteTextEditorHandler.OnPaste()
  │
  ├─ 通过 PasteStrategySelector 获取策略
  │    └─ null → base.OnPaste() 回退
  │
  └─ strategy.PasteAsync(context)
       │  策略拥有完全能力，自行决定：
       ├─ 剪贴板有图片 → 保存图片到文件 + 插入 Markdown 引用
       ├─ 剪贴板有文本 → 插入纯文本
       └─ 剪贴板为空   → 不做任何操作
       │
       └─ 返回 true 表示已处理，false 表示未处理
```

## 剪贴板图片的两种来源

### 来源一：位图数据

截图工具（如 Win+Shift+S、Snipaste）将图片位图直接放入剪贴板。Avalonia 的 `IClipboard` 通过 `GetFormatsAsync()` 暴露可用格式，常见的图片格式包括：

| 剪贴板格式字符串 | 对应文件后缀 |
|---|---|
| `image/png` / `PNG` | `.png` |
| `image/bmp` / `BMP` | `.bmp` |
| `image/jpeg` / `JFIF` | `.jpg` |
| `image/gif` / `GIF` | `.gif` |
| `image/webp` | `.webp` |

检测时遍历格式列表，匹配第一个已知图片格式，用 `GetDataAsync(format)` 读取数据（可能是 `byte[]`、`Stream` 或其他类型），统一转为 `byte[]` 写入文件，后缀取自匹配到的格式。

### 来源二：图片文件引用

从文件资源管理器复制图片文件时，剪贴板中包含文件路径而非位图数据。此时 `GetFormatsAsync()` 不会返回 `image/*` 格式，但会包含文件引用格式（Windows 上为 `FileDrop` / `FileContents`）。

处理方式：

1. 检查剪贴板是否包含文件引用格式。
2. 读取文件路径列表，筛选出图片文件（按后缀名判断）。
3. 将图片文件复制到目标保存目录，保留原始后缀。
4. 生成 Markdown 引用并插入编辑器。

### 优先级

当剪贴板同时包含位图数据和文件引用时（某些场景可能出现），优先处理位图数据。原因：位图数据是"源"，文件引用可能指向临时路径，直接保存位图更可靠。

## 保存路径模板

### 默认模板

```
image/{FileName}/{FileName}{Index}.{Extension}
```

### 占位符说明

| 占位符 | 含义 | 示例 |
|---|---|---|
| `{FileName}` | 当前文档文件名（无后缀名） | `MyPost` |
| `{Index}` | 图片序号，从 0 开始递增 | `0`、`1`、`2` |
| `{Extension}` | 图片原始格式后缀（含点） | `.png`、`.jpg` |

### 示例

设文档路径为 `C:\Blog\MyPost.md`，粘贴 3 张 PNG 图片：

| 图片 | 相对路径 |
|---|---|
| 第 1 张 | `image/MyPost/MyPost0.png` |
| 第 2 张 | `image/MyPost/MyPost1.png` |
| 第 3 张 | `image/MyPost/MyPost2.png` |

如果用户自定义模板为 `assets/{FileName}_{Index}{Extension}`（不建子目录），则：

| 图片 | 相对路径 |
|---|---|
| 第 1 张 | `assets/MyPost_0.png` |
| 第 2 张 | `assets/MyPost_1.png` |

### 编号递增规则

新粘贴的图片从目标目录中已有文件的最大编号 +1 开始，避免覆盖。具体做法：

1. 根据模板渲染出目录路径，检查目录是否存在。
2. 扫描目录中已有的文件，提取编号，取最大值。
3. 新图片从 `最大编号 + 1` 开始编号。如果目录不存在或为空，从 `0` 开始。

### 模板配置方式

模板字符串作为 `MarkdownPasteStrategy` 的构造参数传入，默认值为 `image/{FileName}/{FileName}{Index}.{Extension}`。后续可通过应用配置（`SimpleWriteAppConfiguration`）持久化用户自定义模板。

## 策略模式架构

与项目中 `IAutoIndentStrategy` + `AutoIndentStrategySelector` 的模式保持一致。区别在于：粘贴策略拥有**完整粘贴能力**，不仅处理图片，也处理纯文本。策略内部自行完成文件保存和文本插入，Handler 不参与任何业务逻辑。

```
SimpleWriteTextEditorHandler.OnPaste()
  └─ PasteStrategySelector.GetStrategy(definition, documentFile)
       ├─ Markdown + 已保存 → MarkdownPasteStrategy（完整粘贴能力）
       └─ 其他 / 未保存      → null（Handler 回退到 base.OnPaste()）
```

### 策略接口

策略接口不叫"图片粘贴策略"，而是通用的"粘贴策略"。策略拥有完全能力：自行读取剪贴板、判断内容类型、保存文件、插入文本。

```csharp
namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// 粘贴策略接口。拥有完整的粘贴能力，自行决定如何处理剪贴板内容，
/// 包括保存图片文件、插入引用文本或纯文本。
/// </summary>
internal interface IPasteStrategy
{
    /// <summary>
    /// 执行粘贴操作。策略自行完成所有逻辑（保存文件、插入文本等）。
    /// </summary>
    /// <param name="context">粘贴上下文，提供剪贴板、文档信息、文本插入能力</param>
    /// <returns>true 表示已处理；false 表示未处理，调用方应回退到默认行为</returns>
    Task<bool> PasteAsync(PasteContext context);
}
```

### 粘贴上下文

策略需要的所有外部依赖通过 `PasteContext` 传入，避免策略直接依赖 `TextEditor` 控件类型：

```csharp
namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// 粘贴上下文。为策略提供剪贴板访问、文档信息和文本插入能力。
/// </summary>
internal sealed record PasteContext(
    IClipboard Clipboard,
    FileInfo DocumentFile,
    Action<string> InsertText);
```

- `Clipboard`：Avalonia 剪贴板，用于读取剪贴板内容和格式。
- `DocumentFile`：当前文档文件信息，用于推导图片保存目录。
- `InsertText`：文本插入委托。策略调用此委托将引用文本或纯文本插入编辑器，无需直接操作 `TextEditor` 控件。Handler 在构造 `PasteContext` 时传入 `PerformInput`。

### 策略选择器

```csharp
namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// 根据文档高亮定义和文档路径，选择合适的粘贴策略。
/// </summary>
internal static class PasteStrategySelector
{
    private static readonly MarkdownPasteStrategy _markdownStrategy = new();

    /// <summary>
    /// 获取粘贴策略。返回 null 表示当前文档不支持自定义粘贴，调用方应回退到默认行为。
    /// </summary>
    public static IPasteStrategy? GetStrategy(DocumentHighlightDefinition definition, FileInfo? documentFile)
    {
        // 文档未保存时，无法确定图片保存目录，返回 null
        // Handler 回退到 base.OnPaste()，纯文本粘贴仍然正常
        if (documentFile is null)
        {
            return null;
        }

        return definition.Category switch
        {
            DocumentHighlightCategory.Markdown => _markdownStrategy,
            _ => null,
        };
    }
}
```

### MarkdownPasteStrategy

策略内部完成全部工作：检查剪贴板 → 保存图片 → 插入引用。不把任何中间结果交回 Handler。

```csharp
namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// Markdown 文档的粘贴策略。拥有完整粘贴能力：
/// 优先处理图片（保存文件 + 插入引用），无图片时回退到纯文本粘贴。
/// 默认保存模板：image/{FileName}/{FileName}{Index}.{Extension}
/// </summary>
internal sealed class MarkdownPasteStrategy : IPasteStrategy
{
    private readonly string _pathTemplate;

    public MarkdownPasteStrategy() : this(DefaultPathTemplate)
    {
    }

    public MarkdownPasteStrategy(string pathTemplate)
    {
        _pathTemplate = pathTemplate;
    }

    public const string DefaultPathTemplate = "image/{FileName}/{FileName}{Index}.{Extension}";

    public async Task<bool> PasteAsync(PasteContext context)
    {
        // 1. 尝试从位图数据读取图片
        // 2. 尝试从文件引用读取图片
        // 3. 有图片 → 根据模板计算保存路径，写入文件，生成 Markdown 引用，调用 context.InsertText 插入
        // 4. 无图片 → 读取纯文本，调用 context.InsertText 插入
        // 5. 返回 true 表示已处理
    }
}
```

### OnPaste 重写

Handler 只做两件事：拿策略、调策略。不做任何文档类型判断，不感知 Markdown。

```csharp
// SimpleWriteTextEditorHandler.cs
protected override void OnPaste()
{
    if (GetClipboard() is not { } clipboard)
    {
        return;
    }

    var strategy = PasteStrategySelector.GetStrategy(
        SimpleWriteTextEditor.DocumentHighlightDefinition,
        SimpleWriteTextEditor.DocumentFilePath);

    if (strategy is null)
    {
        base.OnPaste();
        return;
    }

    var documentFile = SimpleWriteTextEditor.DocumentFilePath;
    if (documentFile is null)
    {
        base.OnPaste();
        return;
    }

    var context = new PasteContext(clipboard, documentFile, PerformInput);

    _ = Dispatcher.UIThread.InvokeAsync(async () =>
    {
        bool handled = await strategy.PasteAsync(context);
        if (!handled)
        {
            // 策略未处理，回退到默认纯文本粘贴
            string? text = await clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                OnPastePlainText(text);
            }
        }
    });
}
```

Handler 中不包含任何 Markdown 判断逻辑，不包含任何图片保存逻辑，不包含任何模板渲染逻辑。只负责"拿策略 → 调策略 → 没处理就回退"。

## DocumentFilePath 传递

`SimpleWriteTextEditorHandler` 通过 `SimpleWriteTextEditor` 访问文档信息。在 `SimpleWriteTextEditor` 上新增 `DocumentFilePath` 属性，由 `EditorViewModel` 负责设置和同步。

```csharp
// SimpleWriteTextEditor.cs
internal FileInfo? DocumentFilePath { get; set; }
```

设置时机：

1. `EditorViewModel.CreateTextEditor` 中创建编辑器时设置。
2. `EditorViewModel.SaveEditorModelAsAsync` 另存为成功后同步更新。

```csharp
// EditorViewModel.CreateTextEditor 中
textEditor.DocumentFilePath = editorModel.FileInfo;
```

```csharp
// EditorViewModel.SaveEditorModelAsAsync 中，保存成功后
if (editorModel.TextEditor is SimpleWriteTextEditor te)
{
    te.DocumentFilePath = editorModel.FileInfo;
}
```

## Markdown 引用插入

策略保存图片后，通过 `context.InsertText` 将 Markdown 图片语法插入编辑器。引用路径使用正斜杠 `/`，保证跨平台兼容性。多张图片时，每张图片引用之间用换行符分隔。

```markdown
![](image/MyPost/MyPost0.png)
![](image/MyPost/MyPost1.png)
```

纯文本粘贴时，策略通过 `context.InsertText` 直接插入文本，行为与 `base.OnPaste()` 一致。

## 关键文件

| 文件 | 职责 |
|---|---|
| `SimpleWrite/Business/TextEditors/PasteStrategies/IPasteStrategy.cs` | 粘贴策略接口，拥有完整粘贴能力 |
| `SimpleWrite/Business/TextEditors/PasteStrategies/PasteContext.cs` | 粘贴上下文，封装剪贴板、文档信息、文本插入委托 |
| `SimpleWrite/Business/TextEditors/PasteStrategies/PasteStrategySelector.cs` | 策略选择器，按文档类型返回策略或 null |
| `SimpleWrite/Business/TextEditors/PasteStrategies/MarkdownPasteStrategy.cs` | Markdown 粘贴策略实现，自行完成图片保存和引用插入 |
| `SimpleWrite/Business/TextEditors/PasteStrategies/ClipboardImageReader.cs` | 剪贴板图片读取辅助类，封装位图数据和文件引用两种来源的检测与读取 |
| `SimpleWrite/Business/TextEditors/PasteStrategies/ImagePathTemplate.cs` | 路径模板渲染辅助类，负责占位符替换和编号计算 |
| `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs` | 新增 `DocumentFilePath` 属性 |
| `SimpleWrite/Business/TextEditors/SimpleWriteTextEditorHandler.cs` | 重写 `OnPaste()`，通过选择器获取策略并委托 |
| `SimpleWrite/ViewModels/EditorViewModel.cs` | 创建编辑器和另存为时设置 `DocumentFilePath` |

## 边界情况

### 1. 未保存文档

`DocumentFilePath` 为 `null` 时，`PasteStrategySelector.GetStrategy` 直接返回 `null`，Handler 回退到 `base.OnPaste()`。用户不会看到任何提示，图片粘贴不生效，但纯文本粘贴仍然正常工作。

### 2. 文件名包含非法字符

如果文档文件名包含路径非法字符（如 `?`、`*`、`<`、`>`），在模板渲染时需要做清洗。`ImagePathTemplate` 负责将非法字符替换为 `_`。

### 3. 文件名过长

Windows 路径最大长度为 260 字符（传统限制）。如果文件名很长，加上模板前缀和后缀可能超限。`ImagePathTemplate` 对 `{FileName}` 做截断处理，保留前 50 个字符。

### 4. 剪贴板中同时有图片和文本

当剪贴板同时包含图片和文本时（例如从浏览器复制带文字的图片），策略优先处理图片。图片粘贴成功后不再插入纯文本。

### 5. 剪贴板中的文件不是图片

从文件资源管理器复制的文件中可能混有非图片文件。按后缀名筛选，只处理已知图片格式（`.png`、`.jpg`、`.jpeg`、`.gif`、`.bmp`、`.webp`、`.ico`、`.tiff`），忽略其他文件。如果筛选后没有图片文件，回退到纯文本粘贴。

### 6. 连续粘贴的编号

每次粘贴操作独立计算起始编号：扫描目标目录已有文件，取最大编号 +1。如果之前粘贴了 3 张（编号 0-2），删除了第 2 张，再粘贴时会从 3 开始（因为扫描的是目录中实际存在的文件，0 和 1 仍在，最大编号为 1，新编号为 2）。

## 扩展指南

### 新增文档类型的自定义粘贴

1. 实现 `IPasteStrategy` 接口，定义该文档类型的粘贴行为（如 HTML 的 `<img src="...">` 引用）。
2. 在 `PasteStrategySelector.GetStrategy` 中添加对应的 `DocumentHighlightCategory` 分支。
3. 无需修改 `SimpleWriteTextEditorHandler`。

### 自定义保存路径模板

`MarkdownPasteStrategy` 的构造函数接受模板字符串。后续可通过 `SimpleWriteAppConfiguration` 持久化用户自定义模板，在应用启动时传入。

### 支持拖拽图片

除了剪贴板粘贴，还可以复用策略中的图片处理逻辑支持从文件资源管理器拖拽图片文件到编辑器：

1. 在 `SimpleWriteTextEditorHandler` 中重写拖拽相关方法。
2. 将拖拽的文件路径交给策略处理（复用 `ClipboardImageReader` 中的文件处理代码）。

## 适用场景

- 在 Markdown 文档中通过 `Ctrl+V` 粘贴截图
- 从文件资源管理器复制图片文件后粘贴到 Markdown 文档
- 批量粘贴多张图片时自动编号保存
- 博客写作时自动生成 `image/` 目录结构
- 用户自定义图片保存路径格式
