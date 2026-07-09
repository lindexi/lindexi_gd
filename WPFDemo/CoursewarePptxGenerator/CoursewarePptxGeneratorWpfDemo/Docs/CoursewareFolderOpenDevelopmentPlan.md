# 打开课件文件夹功能开发计划

本文基于 `CoursewareFolderOpenWorkflowThinking.md`、`CoursewareMarkdownExportFormat.md` 和 `CoursewareOutputExportFormat.md`，细化 `CoursewarePptxGeneratorWpfDemo` 中“打开课件文件夹”功能的开发计划、设计细节和实现步骤。

目标是把当前仅记录文件夹路径的入口，升级为课件 Markdown 导出目录的本地加载入口，为后续全局主题分析、逐页美化、资源引用解析和批量输出打好基础。

---

## 一、目标范围

### 1.1 本阶段目标

本阶段只实现打开文件夹后的本地初始化能力，不调用 LLM，不执行全局主题分析，不执行逐页美化。

需要完成的能力：

1. 验证用户选择的目录是否符合课件 Markdown 导出格式。
2. 读取 `courseware.json` 和 `resources/resources.json`。
3. 加载每页 Markdown、截图路径、页面尺寸和页面标识。
4. 将加载结果转换为 UI 页面列表。
5. 为每页创建独立的 `SlideChatManager`。
6. 未生成 SlideML 前，预览区优先显示原始截图。
7. 在状态栏和页面列表中展示加载状态、缺失截图、资源缺失等提示。
8. 保持打开文件夹阶段与智能分析阶段解耦。

### 1.2 非目标范围

本阶段暂不实现以下内容：

- 自动调用 LLM 进行主题分析。
- 自动生成或批量生成 SlideML。
- 自动一致性校验和返工。
- 修改课件导出格式。
- 重新从 EasiNote 的 `Board` / `Slide` 对象提取 Markdown、截图或资源。
- 并发美化流程。

这些能力应在后续阶段基于本阶段加载出的课件输入包继续扩展。

---

## 二、当前代码现状

目标项目已经具备以下基础：

- `ViewModels/MainWindowViewModel.cs`
  - 已有 `Slides`、`SelectedSlide`、`OpenCoursewareFolderCommand`。
  - 当前 `OpenCoursewareFolder` 只保存 `CoursewareFolder` 并设置状态文本。
  - 已经通过 `SlideChatManagerFactory` 为页面创建独立 `SlideChatManager`。
- `Models/CoursewareSlideItem.cs`
  - 当前承载页面标题、摘要、状态、SlideML、渲染日志和回调 XML。
  - 还缺少源 Markdown、截图路径、页面 ID、尺寸、加载警告和输出路径等输入侧字段。
- `Views/MainContentPanel.xaml`
  - 已有“打开课件文件夹”按钮。
  - 当前预览区只绑定 `SlideChatManager.PreviewImage`，没有原始截图回退显示。
- `Converters/FilePathToImageSourceConverter.cs`
  - 已具备从文件路径加载图片到 WPF 预览的基础能力，可复用到原始截图显示。
- `Services/SlideChatManagerFactory.cs`
  - 已封装每页生成独立聊天和渲染上下文的创建逻辑。

因此，本次开发应优先补齐“导出目录读取服务”和“页面输入模型”，再最小修改 ViewModel 与 XAML 绑定。

保存/导出结果格式已经在 `CoursewareOutputExportFormat.md` 中确定。本阶段只需要在加载模型中保留后续输出所需的路径和状态字段，不实现保存命令；后续保存功能应按 `CoursewareOutput.json`、`Theme/`、`Slides/Slide_XXX/` 和 `Summary.md` 的生成结果目录格式落地。

---

## 三、总体架构设计

### 3.1 分层职责

建议新增或扩展以下层次：

```text
CoursewarePptxGeneratorWpfDemo/
  Models/
    CoursewareExportManifest.cs
    CoursewareExportSlideEntry.cs
    CoursewareResourceEntry.cs
    CoursewareInputPackage.cs
    CoursewareSlideInput.cs
    CoursewareLoadWarning.cs
    CoursewareLoadResult.cs
    CoursewareSlideItem.cs
  Services/
    CoursewareFolderLoader.cs
    CoursewareSlideSummaryService.cs
  ViewModels/
    MainWindowViewModel.cs
  Views/
    MainContentPanel.xaml
```

职责划分：

- `CoursewareFolderLoader`
  - 只负责本地 I/O、JSON 解析、路径解析、格式校验和加载警告收集。
  - 不依赖 WPF UI。
  - 不创建 `SlideChatManager`。 // 注： 我计划将这部分逻辑进行拆分，与 UI 框架无关的逻辑应该在一个独立的程序集里面，类似 PptxGenerator.Core 的角色
- `CoursewareInputPackage`
  - 表示已经加载完成的课件输入包。
  - 包含课件根目录、课件名、页面集合、资源集合和加载警告。
- `CoursewareSlideInput`
  - 表示单页输入数据。
  - 包含页面索引、页面 ID、尺寸、Markdown 路径、截图路径、Markdown 文本和页面级警告。
- `CoursewareSlideItem`
  - 继续作为 UI 页面项。
  - 引用或复制 `CoursewareSlideInput` 的关键字段，同时持有独立 `SlideChatManager`。
- `CoursewareSlideSummaryService`
  - 从 Markdown 中提取标题和摘要。
  - 保持纯文本处理，不依赖 UI 和 LLM。
- `MainWindowViewModel`
  - 调用加载服务。
  - 把加载结果转换成 `Slides`。
  - 更新当前选中页、状态文本和预览显示。

### 3.2 数据流

打开课件文件夹后的数据流如下：

```text
用户选择目录
  -> MainContentPanel.xaml.cs 将 folderPath 传给 OpenCoursewareFolderCommand
  -> MainWindowViewModel.OpenCoursewareFolderAsync
  -> CoursewareFolderLoader.LoadAsync
  -> CoursewareInputPackage
  -> MainWindowViewModel 清空 Slides
  -> 为每个 CoursewareSlideInput 创建 CoursewareSlideItem + SlideChatManager
  -> SelectedSlide = 第一页
  -> 预览区显示 SelectedSlide.SourceScreenshotFilePath
  -> 状态显示“已加载 N 页，等待主题分析”
```

### 3.3 异步设计

打开目录涉及文件系统和 JSON 解析，应改为异步方法：

- `CoursewareFolderLoader.LoadAsync(string folderPath, CancellationToken cancellationToken)`
- `MainWindowViewModel.OpenCoursewareFolderAsync(string? folderPath)`

`OpenCoursewareFolderCommand` 可以保持 `RelayCommand`，内部触发 `_ = OpenCoursewareFolderAsync(path)`，并通过 `IsBusy` 禁用其他操作。

服务层与 UI 无关的 `await` 使用 `ConfigureAwait(false)`。回到 `ObservableCollection` 和属性更新时，通过 WPF Dispatcher 或当前 ViewModel 已有的 UI 线程上下文处理。

// 注： 采用 `ConfigureAwait(false)` 时，需要提示要分派子智能体去审查，确保上下路径里面没有 UI 线程依赖的代码。否则应该用 `ConfigureAwait(true)`  逻辑。要用子智能体去审查

---

## 四、模型设计

### 4.1 CoursewareExportManifest

用于反序列化 `courseware.json`。

建议字段：

- `int ExportVersion`
- `DateTimeOffset CreatedAt`
- `string? CoursewareName`
- `int SlideCount`
- `IReadOnlyList<CoursewareExportSlideEntry> Slides`
- `string? ResourcesFile`

设计要点：

- 使用 `System.Text.Json`。
- JSON 属性名通过 `[JsonPropertyName]` 显式映射小驼峰字段。
- 如果后续开启 AOT，可补充 `JsonSerializerContext` 源生成上下文。 // 注： 现在就要做
- 当前支持 `exportVersion = 1`。

### 4.2 CoursewareExportSlideEntry

用于反序列化 `courseware.json` 中的 `slides` 项。

建议字段：

- `int SlideIndex`
- `string? SlideId`
- `double Width`
- `double Height`
- `string? MarkdownFile`
- `string? ScreenshotFile`

校验规则：

- `slideIndex` 应与集合位置一致；不一致时记录警告，但不必阻止加载。
- `slideId` 缺失时可记录警告，并在 UI 中继续使用页码作为显示依据。
- `width`、`height` 小于等于 0 时记录警告，并回退到 1280 x 720。
- `markdownFile` 缺失或文件不存在时应阻止加载该页，严重时阻止整个课件加载。
- `screenshotFile` 缺失或文件不存在时允许降级加载，但页面状态应提示“缺失截图”。

### 4.3 CoursewareResourceEntry

用于反序列化 `resources/resources.json`。

建议字段：

- `string? ImageId`
- `string? SourceFileName`
- `string? ExportFile`
- `string? ResolvedFilePath`
- `bool Exists`

设计要点：

- `exportFile` 是相对 `resources` 目录的路径。
- 解析时使用 `Path.Join(resourcesDirectory, exportFile)`。
- 资源文件缺失不应阻止课件加载，但需要记录警告。
- 不把本机绝对路径写回 JSON 或 Markdown 文档。

### 4.4 CoursewareInputPackage

表示完成加载后的课件输入包。

建议字段：

- `DirectoryInfo RootDirectory`
- `string CoursewareName`
- `int SlideCount`
- `IReadOnlyList<CoursewareSlideInput> Slides`
- `FileInfo? ResourcesIndexFile`
- `IReadOnlyList<CoursewareResourceEntry> Resources`
- `IReadOnlyList<CoursewareLoadWarning> Warnings`

设计要点：

- `SlideCount` 以实际可加载的 `Slides.Count` 为准，同时保留 manifest 中的 `slideCount` 作为校验信息。
- `CoursewareName` 为空时使用文件夹名。
- `Warnings` 用于 UI 状态提示和后续诊断日志。

### 4.5 CoursewareSlideInput

表示单页加载输入。

建议字段：

- `int SlideIndex`
- `int PageNumber`
- `string SlideId`
- `double Width`
- `double Height`
- `FileInfo MarkdownFile`
- `FileInfo? ScreenshotFile`
- `string MarkdownText`
- `IReadOnlyList<CoursewareLoadWarning> Warnings`

设计要点：

- `PageNumber = SlideIndex + 1`。
- `ScreenshotFile` 允许为空。
- Markdown 文本读取失败时该页不可加载，应作为错误处理。
- 后续主题分析和逐页美化都应从这里读取 Markdown，而不是再访问 EasiNote 对象。

### 4.6 CoursewareLoadWarning

表示非阻塞问题。

建议字段：

- `CoursewareLoadWarningLevel Level`
- `string Code`
- `string Message`
- `string? RelativePath`
- `int? SlideIndex`

建议级别：

- `Info`
- `Warning`
- `Error`

使用方式：

- `Warning` 用于截图缺失、资源缺失、索引不一致等可降级问题。
- `Error` 用于 manifest 无法解析、Markdown 缺失、页面集合为空等阻塞问题。
- 如果存在阻塞错误，`CoursewareFolderLoader` 应抛出精确异常或返回失败结果。

### 4.7 CoursewareLoadResult

可以选择引入加载结果包装，避免异常承载所有控制流。

建议字段：

- `bool IsSuccess`
- `CoursewareInputPackage? Package`
- `IReadOnlyList<CoursewareLoadWarning> Warnings`
- `string? ErrorMessage`

第一版也可以只让 `CoursewareFolderLoader.LoadAsync` 成功返回 `CoursewareInputPackage`，阻塞错误抛出 `InvalidDataException`。为了保持简单，推荐第一版使用异常处理阻塞错误，使用 `Warnings` 承载非阻塞问题。

---

## 五、加载服务设计

### 5.1 CoursewareFolderLoader 公共 API

建议新增：

```text
Services/CoursewareFolderLoader.cs
```

公共方法：

- `Task<CoursewareInputPackage> LoadAsync(string folderPath, CancellationToken cancellationToken = default)`

职责：

1. 校验 `folderPath` 非空且目录存在。
2. 查找根目录下 `courseware.json`。
3. 使用 `System.Text.Json` 反序列化 manifest。
4. 校验 `exportVersion`。
5. 校验 `slideCount` 与 `slides.Count`。
6. 逐页解析 Markdown 和截图路径。
7. 读取每页 Markdown 文本。
8. 读取资源索引文件。
9. 校验资源文件存在性。
10. 返回 `CoursewareInputPackage`。

### 5.2 路径安全与路径解析

所有 manifest 中的路径都应视为相对导出根目录的路径。

实现时需要注意：

- 使用 `Path.Join(rootDirectory.FullName, relativePath)` 拼接路径。
- 使用 `Path.GetFullPath` 规范化路径。
- 校验规范化后的路径仍位于导出根目录之下，避免 `../` 路径越界。
- 对 `resources.json` 中的 `exportFile`，基准目录是资源索引所在目录或 `resources` 目录。

建议内部提供方法：

- `ResolvePathUnderRoot(DirectoryInfo rootDirectory, string relativePath)`
- `ResolvePathUnderDirectory(DirectoryInfo baseDirectory, string relativePath)`

如果发现路径越界，应作为阻塞错误处理。

### 5.3 JSON 解析

遵循项目约定，优先使用 `System.Text.Json`。

建议配置：

- `PropertyNameCaseInsensitive = true`
- `ReadCommentHandling = JsonCommentHandling.Skip` 可选
- 不引入 `Newtonsoft.Json`

后续可新增源生成上下文：

- `CoursewareExportJsonSerializerContext`

第一版若只面向 WPF Demo，可先使用 `JsonSerializer.DeserializeAsync<T>`，后续再补充源生成上下文。

### 5.4 版本校验

当前仅支持 `exportVersion = 1`。

行为建议：

- `exportVersion` 缺失或不是 1：阻止加载。
- 错误消息展示为：“不支持的课件导出格式版本：{version}，当前仅支持 1”。

### 5.5 页面校验

页面级校验规则：

- `slides` 为空：阻止加载。
- `markdownFile` 为空：阻止加载该页；如果最终没有可加载页面，则阻止整个课件加载。
- Markdown 文件不存在：阻止加载该页；记录错误。
- Markdown 读取失败：阻止加载该页；记录错误。
- `screenshotFile` 为空或不存在：允许加载；记录警告。
- 页面尺寸无效：允许加载；回退默认尺寸 1280 x 720；记录警告。

第一版建议只要任意页面 Markdown 缺失就阻止整个加载，这样可以避免 UI 页面列表与导出索引不一致。后续如果需要更强容错，再改为跳过坏页。

### 5.6 资源校验

资源索引校验规则：

- `resourcesFile` 为空：允许无资源模式；记录警告。
- 资源索引文件不存在：允许无资源模式；记录警告。
- 资源索引 JSON 无法解析：允许无资源模式还是阻止加载需要权衡。

推荐第一版行为：

- `resourcesFile` 缺失或文件不存在：允许加载。
- `resources.json` 存在但无法解析：阻止加载。因为这代表导出目录结构损坏，继续加载可能导致后续资源引用不可控。
- 单个资源文件缺失：允许加载并记录警告。

---

## 六、UI 页面模型扩展

### 6.1 CoursewareSlideItem 扩展字段

当前 `CoursewareSlideItem` 主要偏向生成后状态。建议扩展为同时承载输入侧状态。

新增字段：

- `string SlideId`
- `int SlideIndex`
- `double Width`
- `double Height`
- `string SourceMarkdownFilePath`
- `string SourceMarkdownText`
- `string? SourceScreenshotFilePath`
- `bool HasSourceScreenshot`
- `string? ErrorMessage`
- `IReadOnlyList<CoursewareLoadWarning> LoadWarnings`
- `string? GeneratedSlideMlFilePath`
- `string? GeneratedPreviewFilePath`

现有字段保留：

- `SlideChatManager`
- `PageNumber`
- `Title`
- `Summary`
- `Status`
- `SlideMl`
- `RenderingLog`
- `CallbackXml`

设计考虑：

- 第一版可以继续使用不可变 `init` 属性，打开文件夹时一次性创建完整页面项。
- 后续逐页美化需要更新 `Status`、`SlideMl`、`RenderingLog` 等字段时，可能需要把 `CoursewareSlideItem` 改为继承 `ObservableObject`，或新增 `CoursewareSlideItemViewModel`。
- 如果本次只实现加载，暂时不必大改为可变模型，避免扩大变更范围。

### 6.2 页面标题提取

新增 `CoursewareSlideSummaryService`，从 Markdown 提取页面标题和摘要。

标题提取规则：

1. 优先提取第一个以 `# ` 开头的 Markdown 一级标题。
2. 如果一级标题是默认格式 `Slide N`，可以继续查找正文中的第一个二级标题或粗体文本。
3. 如果仍提取不到，使用“第 N 页”。

摘要提取规则：

1. 跳过元数据区，例如 `- SlideIndex:`、`- SlideId:`、`- Size:`、`- Screenshot:` 和 `---`。
2. 从正文中取前 2 到 3 行非空文本。
3. 合并后按 80 到 120 个字符截断。
4. 如果正文为空，显示“已加载页面 Markdown，等待美化。”。

### 6.3 页面状态

建议统一页面状态文案：

- `已加载`：Markdown 和截图都存在。
- `已加载，缺失截图`：Markdown 存在但截图不存在。
- `资源部分缺失`：该页可能引用的资源缺失。第一版如果没有建立页到资源的引用关系，可只在全局状态显示。
- `待美化`：进入主题分析后尚未生成。
- `生成中`：后续逐页美化时使用。
- `已生成`：已有 SlideML。
- `失败`：后续生成或渲染失败时使用。

第一版打开目录后可将页面状态设置为：

- 有截图：`已加载`
- 无截图：`已加载，缺失截图`

主状态文本设置为：

- 无警告：`已加载 {N} 页，等待主题分析`
- 有警告：`已加载 {N} 页，存在 {WarningCount} 个警告，等待主题分析`

---

## 七、ViewModel 改造设计

### 7.1 构造函数注入

`MainWindowViewModel` 当前注入 `SlideChatManagerFactory` 和初始 `SlideChatManager`。

建议增加：

- `CoursewareFolderLoader coursewareFolderLoader`
- `CoursewareSlideSummaryService slideSummaryService`

如果暂时未引入 DI 容器，可在 `App.xaml.cs` 中创建并传入。

### 7.2 OpenCoursewareFolder 改为异步加载

当前方法：

```text
OpenCoursewareFolder(string? folderPath)
```

建议调整：

```text
private async Task OpenCoursewareFolderAsync(string? folderPath)
```

命令执行逻辑：

1. 空路径或目录不存在时直接返回。
2. 设置 `IsBusy = true`。
3. 设置 `StatusText = "正在加载课件导出目录..."`。
4. 调用 `CoursewareFolderLoader.LoadAsync`。
5. 为每页输入创建独立 `SlideChatManager`。
6. 清空 `Slides`。
7. 添加页面项。
8. 设置 `CoursewareFolder`。
9. 选中第一页。
10. 更新状态文本。
11. 捕获 `InvalidDataException`、`IOException`、`JsonException` 等精确异常并显示失败信息。
12. 最终设置 `IsBusy = false`。

### 7.3 页面创建逻辑

新增私有方法：

- `Task<CoursewareSlideItem> CreateSlideItemAsync(CoursewareSlideInput input)`
- `string CreateSlideStatus(CoursewareSlideInput input)`

页面项创建逻辑：

1. 调用 `_slideChatManagerFactory.CreateAsync()`。
2. 调用 `CoursewareSlideSummaryService` 提取标题和摘要。
3. 把输入字段写入 `CoursewareSlideItem`。
4. 初始 `SlideMl` 为空。
5. 初始 `RenderingLog` 为“尚未执行渲染。”。
6. 初始 `CallbackXml` 为空。

### 7.4 SelectedSlide 同步逻辑

当前 `SyncSelectedSlideContext` 会把 `EditableSlideXml` 设置为 `SelectedSlide?.SlideMl`，并根据 `SlideChatManager.PreviewImage` 显示预览。

需要补充：

- 当当前页没有 `SlideChatManager.PreviewImage` 时，XAML 预览区显示 `SelectedSlide.SourceScreenshotFilePath`。
- `HasPreviewImage` 的语义可以拆分为：
  - `HasRenderedPreviewImage`
  - `HasSourceScreenshot`
  - `HasAnyPreviewImage`

推荐第一版新增 ViewModel 属性：

- `bool HasRenderedPreviewImage => SlideChatManager.PreviewImage is not null`
- `bool HasSourceScreenshot => SelectedSlide?.HasSourceScreenshot == true`
- `bool HasAnyPreviewImage => HasRenderedPreviewImage || HasSourceScreenshot`

并在以下场景触发属性变更：

- `SelectedSlide` 变化。
- `SlideChatManager.PreviewImage` 变化。

### 7.5 错误处理

打开目录失败时：

- 不清空当前已经加载的页面，避免用户因选错目录丢失当前工作状态。
- 更新 `StatusText` 为明确错误，例如“加载课件目录失败：缺少 courseware.json”。
- 可选地弹出 MessageBox，但推荐第一版只更新状态文本，避免 ViewModel 直接依赖 UI 弹窗。

如果加载成功但有警告：

- 完成页面初始化。
- `StatusText` 显示警告数量。
- 后续可在日志页或专门面板中展示详细警告。

---

## 八、预览区设计

### 8.1 显示优先级

中间预览区应按以下优先级显示：

1. 已生成 SlideML 并完成渲染时，显示 `SlideChatManager.PreviewImage`。
2. 尚未生成或尚未渲染时，显示导出目录中的原始截图 `SelectedSlide.SourceScreenshotFilePath`。
3. 两者都不存在时，显示空状态提示。

### 8.2 XAML 实现方案

当前 `MainContentPanel.xaml` 中只有一个绑定 `SlideChatManager.PreviewImage` 的 `Image`。

建议改为三个叠层元素：

- 渲染预览图 Image：绑定 `SlideChatManager.PreviewImage`。
- 原始截图 Image：绑定 `SelectedSlide.SourceScreenshotFilePath` 并使用 `FilePathToImageSourceConverter`。
- 空状态 StackPanel：绑定 `HasAnyPreviewImage` 的反向可见性。

可见性规则：

- 渲染预览图：`HasRenderedPreviewImage = true`。
- 原始截图：`HasRenderedPreviewImage = false && HasSourceScreenshot = true`。
- 空状态：`HasAnyPreviewImage = false`。

如果当前项目缺少布尔组合转换器，可新增：

- `BooleanToVisibilityConverter`
- `InverseBooleanToVisibilityConverter` 已存在，可继续复用。
- 对“没有渲染但有截图”的组合，推荐在 ViewModel 提供 `ShowSourceScreenshot` 属性，避免 XAML MultiBinding 复杂化。

### 8.3 页面尺寸

当前预览容器固定为 1280 x 720。

建议第一版保留固定 16:9 容器，因为导出格式当前主要是普通课件截图。

同时在 `CoursewareSlideItem` 中保存 `Width` 和 `Height`，后续可以将 Viewbox 内部 Grid 的 `Width`、`Height` 绑定到当前页尺寸。

---

## 九、资源上下文设计

### 9.1 第一版资源加载

打开文件夹时读取 `resources/resources.json`，建立资源集合：

- `ImageId`
- `SourceFileName`
- `ExportFile`
- `ResolvedFilePath`
- `Exists`

本阶段只做加载和校验，不直接传给 LLM。

### 9.2 后续提示词使用

后续全局主题分析或逐页美化时，可以将资源上下文组织为：

```text
可用图片资源：
- img_1：picture.png，引用路径 resources/img_1.png
- img_2：diagram.jpg，引用路径 resources/img_2.jpg
```

注意：

- 提示词中应使用相对路径或资源 ID，不应暴露用户本机绝对路径。
- 渲染前需要把 SlideML 中的相对路径解析到导出目录。

### 9.3 渲染路径解析扩展

当前 `CoursewareWpfSlideMlRenderEngine` 更偏向当前工作目录、`Images`、`Assets` 等固定路径。

后续建议扩展为：

- 在渲染上下文中增加 `ResourceRootDirectory` 或 `CoursewareRootDirectory`。
- 渲染图片时优先解析：
  1. 绝对路径。
  2. 相对课件根目录路径。
  3. 相对资源目录路径。
  4. 当前工作目录兼容路径。

本阶段可先只记录资源路径，不修改渲染器。

---

## 十、阶段实施步骤

### 阶段 1：新增读取模型

目标：定义导出目录读取所需的纯数据模型。

涉及文件：

- `Models/CoursewareExportManifest.cs`
- `Models/CoursewareExportSlideEntry.cs`
- `Models/CoursewareResourceEntry.cs`
- `Models/CoursewareInputPackage.cs`
- `Models/CoursewareSlideInput.cs`
- `Models/CoursewareLoadWarning.cs`
- `Models/CoursewareLoadWarningLevel.cs`

验收标准：

- 模型命名符合 C# 规范。
- 公共类型和公共成员具备 XML 文档注释。
- JSON 字段通过 `System.Text.Json.Serialization.JsonPropertyNameAttribute` 映射。

### 阶段 2：实现 CoursewareFolderLoader

目标：完成导出目录本地加载和校验。

涉及文件：

- `Services/CoursewareFolderLoader.cs`

实现内容：

1. 校验目录和 `courseware.json`。
2. 解析 manifest。
3. 校验导出版本和页面数量。
4. 解析并读取页面 Markdown。
5. 检查截图文件。
6. 解析资源索引。
7. 检查资源文件。
8. 返回 `CoursewareInputPackage`。

验收标准：

- 合法导出目录可以成功加载。
- 缺少 `courseware.json` 会给出明确错误。
- 缺少截图不阻止加载，但会产生警告。
- 缺少资源文件不阻止加载，但会产生警告。
- 路径越界会阻止加载。

### 阶段 3：实现 Markdown 标题和摘要提取

目标：从页面 Markdown 生成页面列表展示文本。

涉及文件：

- `Services/CoursewareSlideSummaryService.cs`

实现内容：

1. 提取第一个有效标题。
2. 跳过导出元数据区。
3. 生成短摘要。
4. 对空内容使用默认文案。

验收标准：

- 常规 Markdown 可以提取标题。
- 只有默认 `# Slide N` 时仍有合理标题。
- 摘要不包含 `SlideIndex`、`SlideId`、`Size`、`Screenshot` 等元数据。

### 阶段 4：扩展 CoursewareSlideItem

目标：让 UI 页面项承载导出输入状态。

涉及文件：

- `Models/CoursewareSlideItem.cs`

实现内容：

1. 增加页面 ID、索引、尺寸字段。
2. 增加 Markdown 文件路径和 Markdown 文本字段。
3. 增加原始截图路径和截图存在状态字段。
4. 增加加载警告字段。
5. 保留现有生成后状态字段。

验收标准：

- 现有页面绑定不破坏。
- 新增字段可被 ViewModel 和 XAML 使用。

### 阶段 5：改造 MainWindowViewModel 打开目录流程

目标：把打开文件夹从“保存路径”升级为“加载输入包并初始化页面列表”。

涉及文件：

- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`（如果需要传入新服务）

实现内容：

1. 注入或创建 `CoursewareFolderLoader`。
2. 注入或创建 `CoursewareSlideSummaryService`。
3. 将 `OpenCoursewareFolder` 改为异步加载。
4. 加载成功后清空默认空白页。
5. 为每页创建独立 `SlideChatManager`。
6. 选中第一页。
7. 更新状态文本。
8. 加载失败时保留当前页面列表。

验收标准：

- 打开合法导出目录后，左侧页面列表显示真实页面数量。
- 当前页摘要来自 Markdown。
- `CoursewareFolderDisplayText` 显示当前目录。
- 状态显示“已加载 N 页，等待主题分析”或带警告数量的文案。

### 阶段 6：增加原始截图预览

目标：未生成 SlideML 前也能显示导出截图。

涉及文件：

- `Views/MainContentPanel.xaml`
- `ViewModels/MainWindowViewModel.cs`
- 可能复用 `Converters/FilePathToImageSourceConverter.cs`

实现内容：

1. 新增 `HasRenderedPreviewImage`、`ShowSourceScreenshot`、`HasAnyPreviewImage` 等属性。
2. XAML 中增加原始截图 Image。
3. 空状态提示改为仅在没有渲染图和截图时显示。
4. 选中不同页面时刷新预览状态。

验收标准：

- 打开目录后选中第一页立即显示原始截图。
- 切换页面时预览切换到对应截图。
- 生成 SlideML 并渲染后优先显示渲染结果。
- 缺失截图页面显示空状态提示。

### 阶段 7：展示加载警告

目标：让用户知道导出目录中存在的非阻塞问题。

第一版建议只使用状态文本和页面状态，不新增复杂 UI。

涉及文件：

- `ViewModels/MainWindowViewModel.cs`
- `Models/CoursewareSlideItem.cs`
- `Views/LeftSidebarPanel.xaml`（可选）

实现内容：

1. 全局状态显示警告数量。
2. 缺失截图页面状态显示“已加载，缺失截图”。
3. 可选：页面摘要中追加简短警告提示。

验收标准：

- 缺失截图不会阻止加载。
- 用户能在页面列表或状态栏感知截图缺失。
- 资源缺失能在全局状态中体现。

### 阶段 8：验证与测试

目标：确保加载流程可靠且不破坏现有单页生成能力。

建议测试方式：

1. 准备合法导出目录：包含 `courseware.json`、多页 Markdown、截图和资源索引。
2. 准备缺少截图的导出目录。
3. 准备缺少资源文件的导出目录。
4. 准备缺少 `courseware.json` 的普通目录。
5. 准备 `exportVersion` 不支持的目录。
6. 准备路径包含 `../` 的非法 manifest。

验收标准：

- 项目可编译。
- 合法目录加载成功。
- 非阻塞问题显示警告。
- 阻塞问题显示明确失败状态。
- 原有聊天生成、重新渲染、MCP 切换入口不因加载流程改造而失效。

---

## 十一、后续扩展计划

本阶段完成后，可继续按以下顺序扩展：

1. **全局主题分析命令**
   - 新增“分析全局主题”或“开始全课件美化”按钮。
   - 输入来自 `CoursewareInputPackage.Slides[*].MarkdownText`。
   - 输出结构化 `CoursewareTheme`。

2. **逐页美化流程**
   - 串行遍历 `Slides`。
   - 每页使用独立 `SlideChatManager`。
   - 输入包含当前页 Markdown、截图、全局主题、前后页摘要和资源索引。
   - 输出 SlideML、渲染预览、日志和错误状态。

3. **输出目录规则**
   - 输出格式以 `CoursewareOutputExportFormat.md` 为准，生成结果根目录包含 `CoursewareOutput.json`、`Theme/`、`Slides/` 和 `Summary.md`。
   - 每页结果保存到 `Slides/Slide_XXX/`，包含 `Slide.slideml`、`Preview.png`、`Generation.md`、`RenderLog.txt`、`SlideResult.json`，并可选复制原始 Markdown 为 `SourceMarkdown.md`。
   - 所有索引文件中的路径使用相对路径，不写入本机磁盘绝对路径、API Key、访问令牌、连接字符串或其他敏感信息。
   - 保存生成结果时不修改原始 Markdown 导出目录，生成结果可以依赖原始导出目录继续存在；后续如需独立分发，再扩展 portable 模式。

4. **资源引用解析**
   - 统一约定 SlideML 中图片使用 `resources/img_1.png` 或资源 ID。
   - 渲染器根据课件根目录解析相对路径。

5. **一致性校验**
   - 批量美化后基于渲染结果和主题分析结果进行评分。
   - 第一版只标记问题页，不自动返工。

---

## 十二、风险与对策

### 12.1 路径越界风险

风险：导出 JSON 中可能包含 `../`，导致加载器读取导出目录外文件。

对策：所有相对路径都经过 `Path.GetFullPath` 规范化，并校验结果仍位于允许根目录下。

### 12.2 截图缺失导致预览为空

风险：部分导出目录中截图不存在，用户误以为加载失败。

对策：截图缺失不阻止加载，但页面状态显示“已加载，缺失截图”，预览区显示明确空状态。

### 12.3 资源路径与渲染器不一致

风险：LLM 后续生成 SlideML 时引用资源 ID，但本地渲染器无法找到图片。

对策：本阶段先加载资源索引并保存解析路径；后续在生成提示词和渲染器中统一引用规则。

### 12.4 大课件加载耗时

风险：大量页面 Markdown 和截图校验可能阻塞 UI。

对策：打开目录流程使用异步方法，加载期间设置 `IsBusy`，必要时后续增加取消按钮。

### 12.5 页面模型后续需要频繁更新

风险：当前 `CoursewareSlideItem` 使用 `init` 属性，后续生成状态更新不方便。

对策：本阶段只做加载可保持简单；进入逐页美化阶段前，将页面项升级为 `ObservableObject` 或新增 `CoursewareSlideItemViewModel`。

---

## 十三、建议优先级

推荐按以下优先级实施：

1. `CoursewareFolderLoader` 和读取模型。
2. `CoursewareSlideItem` 输入字段扩展。
3. `MainWindowViewModel.OpenCoursewareFolderAsync`。
4. 原始截图预览。
5. 加载警告展示。
6. 单元测试或手工测试样例。
7. 后续再进入全局主题分析和逐页美化。

该顺序可以先形成稳定的本地输入初始化闭环，避免过早引入 LLM 调用、主题分析和批量生成带来的复杂度。

// 注： 一口气完成，不要分开兼容