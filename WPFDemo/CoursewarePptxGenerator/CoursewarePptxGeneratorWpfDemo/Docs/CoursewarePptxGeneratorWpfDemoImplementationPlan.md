# CoursewarePptxGeneratorWpfDemo 实施计划

## 目标

完成 `CoursewarePptxGeneratorWpfDemo`，使其在视觉上承接视觉稿项目的三栏工作台设计，并在功能上复用现有单页美化 Demo 的 SlideML 生成、渲染、评估与聊天能力，逐步扩展为面向整个课件的美化处理程序。

## 探索结论

### 视觉稿项目现状

参考项目：`WPFDemo/CoursewarePptxGenerator/CheawuchewalYicheredurlakearja`

关键结构：

- `MainWindow.xaml`：顶层三栏布局。
  - 左侧：页面缩略图导航。
  - 中间：标题栏、幻灯片预览、SlideML/XML 与日志区。
  - 右侧：Copilot 面板。
- `Views/LeftSidebarPanel.xaml`：静态缩略图侧边栏。
- `Views/MainContentPanel.xaml`：主预览和 XML/日志区。
- `Views/CopilotPanel.xaml`：聊天输入与操作按钮区。
- `App.xaml`：集中定义主题色、按钮、滚动条、TabControl 样式。

可复用点：

- 三栏工作台布局适合作为课件级美化工具主框架。
- 卡片式 UI、浅灰背景、蓝紫强调色、深色代码区、现代滚动条和 Tab 样式可以迁移。
- 当前视觉稿代码隐藏几乎为空，适合继续保持 View 与逻辑分离。

需要调整点：

- 当前页面和聊天内容都是硬编码，应改为 MVVM 数据绑定。
- 样式集中在 `App.xaml`，正式项目应拆分到独立资源文件。
- 缩略图应由 `ItemsControl` 或 `ListBox` 绑定集合生成，不应保留大量静态 Canvas。
- 输入框占位符实现较临时，后续应使用样式、附加属性或自定义控件。

### 单页美化项目现状

参考项目：`Pptx/PptxGenerator/Code/PptxGeneratorWpfDemo`

关键结构：

- `App.xaml.cs`：创建语言模型、渲染管道、评估器和 `SlideChatManager`。
- `MainWindow.xaml`：现有单页美化界面。
- `ViewModels/MainWindowViewModel.cs`：单页美化核心 ViewModel。
- `Ui/Views/CharUserControl.xaml`：聊天控件。
- `Core/SlideMl/Rendering/WpfSlideMlRenderEngine.cs`：WPF SlideML 渲染引擎。
- `Core/SlideMl/Rendering/SwitchableSlideMlRenderPipeline.cs`：本地/MCP 可切换渲染管道。
- `Models/WpfPreviewImage.cs`：WPF 预览图片适配模型。
- `Threading/WpfDispatcher.cs`：WPF 主线程调度器。
- `Ui/Converters/*.cs`：聊天、图片和 UI 状态转换器。

业务闭环：

1. 用户通过聊天输入单页美化需求。
2. ViewModel 调用 `SlideChatManager`。
3. 生成 SlideML。
4. 使用本地 WPF 或 MCP 渲染管道渲染预览。
5. 回填当前 SlideML、渲染 XML、警告和预览图。
6. 支持手工重新渲染。
7. 支持页面评估、提示词评估和提示词迭代。

迁移风险：

- 源项目存在本地 Agent 配置路径，目标项目不能照搬，应改为配置化或用户选择。
- `PptxGenerator.Core` 不包含 WPF 渲染引擎，目标项目必须补充 WPF 适配层。
- MCP 渲染是可选增强，第一阶段不应作为最小闭环的阻塞项。
- 源控件 `CharUserControl` 命名疑似拼写问题，迁移时建议统一为 `ChatUserControl`。

### 目标项目现状

目标项目：`WPFDemo/CoursewarePptxGenerator/CoursewarePptxGeneratorWpfDemo`

当前状态：

- `CoursewarePptxGeneratorWpfDemo.csproj`：WPF 项目，目标框架为 `net10.0-windows`。
- `App.xaml`：默认使用 `StartupUri="MainWindow.xaml"`。
- `App.xaml.cs`：默认空启动逻辑。
- `MainWindow.xaml`：空窗口。
- `MainWindow.xaml.cs`：仅调用 `InitializeComponent()`。

当前缺口：

- 缺少 MVVM 结构。
- 缺少主题资源和视觉稿控件拆分。
- 缺少聊天控件。
- 缺少 SlideML 预览、编辑、日志、评估 UI。
- 缺少 WPF 渲染适配层。
- 缺少与 `PptxGenerator.Core` 的集成。
- 缺少课件级页面集合、当前页面状态和批量美化流程。

## MVVM 设计原则

后续 UI 开发应遵循以下实践：

- `View` 只负责布局、样式、数据模板和绑定。
- `.xaml.cs` 仅保留初始化、必要的 WPF 事件桥接和与 UI 平台强相关的代码。
- 业务逻辑放在 ViewModel 或服务中。
- 用户操作使用 `ICommand` 绑定。
- 页面集合、聊天消息、渲染日志使用可观察集合绑定。
- 当前选中课件页使用 `SelectedSlide` 或类似属性绑定。
- 预览图、SlideML、回填 XML、警告、评估结果都从 ViewModel 暴露。
- 与 UI 无关的异步方法传递 `CancellationToken`，并在库/服务层使用 `ConfigureAwait(false)`。
- 新增公共 API 必须提供 XML 文档注释。

## 建议目录结构

建议在目标项目中逐步建立以下结构：

```text
CoursewarePptxGeneratorWpfDemo/
  Docs/
    CoursewarePptxGeneratorWpfDemoImplementationPlan.md
  Resources/
    Brushes.xaml
    ButtonStyles.xaml
    TextBoxStyles.xaml
    ScrollViewerStyles.xaml
    TabStyles.xaml
    CardStyles.xaml
    ChatStyles.xaml
  Models/
    CoursewareSlideItem.cs
    ChatMessageItem.cs
    ModelDisplayItem.cs
    WpfPreviewImage.cs
  Services/
    CoursewareGenerationService.cs
    SlideChatManagerFactory.cs
    AgentConfigurationProvider.cs
  Threading/
    WpfDispatcher.cs
  ViewModels/
    MainWindowViewModel.cs
    CoursewareSlideItemViewModel.cs
    CopilotPanelViewModel.cs
  Views/
    LeftSidebarPanel.xaml
    MainContentPanel.xaml
    CopilotPanel.xaml
    ChatUserControl.xaml
  Ui/
    Converters/
    Selectors/
  Core/
    SlideMl/
      Rendering/
```

说明：

- `Resources` 用于从视觉稿迁移和拆分主题资源。
- `Views` 用于承接视觉稿三栏布局。
- `ViewModels` 用于承载页面集合、聊天、渲染、评估和命令。
- `Services` 用于隔离模型创建、配置读取、课件级流程编排。
- `Core/SlideMl/Rendering` 用于放置 WPF 侧渲染适配代码。
- `Docs` 用于保存设计和开发计划。

## 分阶段实施计划

### 阶段一：搭建可运行的视觉框架

目标：让目标项目具备视觉稿主界面结构，但暂不接入真实 AI 业务。

步骤：

1. 拆分并迁移视觉稿主题资源。
   - 新建 `Resources/Brushes.xaml`、`ButtonStyles.xaml`、`ScrollViewerStyles.xaml`、`TabStyles.xaml` 等文件。
   - 在 `App.xaml` 中通过 `MergedDictionaries` 引入。

2. 迁移三栏布局。
   - 改造 `MainWindow.xaml` 为三栏 Grid。
   - 新增 `Views/LeftSidebarPanel.xaml`、`MainContentPanel.xaml`、`CopilotPanel.xaml`。
   - 保持代码隐藏无业务逻辑。

3. 建立基础 ViewModel。
   - 新增 `MainWindowViewModel`。
   - 提供设计时或运行时假数据：页面标题、缩略图、当前 SlideML、日志、聊天消息。
   - 将静态视觉稿内容替换为绑定数据。

4. 建立数据模板。
   - 左侧缩略图使用 `ListBox` 或 `ItemsControl`。
   - 聊天消息使用 `ItemsControl` 和消息模板。
   - 日志和 XML 区域使用绑定文本。

验收标准：

- 项目可编译、可启动。
- 主界面视觉接近视觉稿。
- 页面缩略图、当前页面信息、聊天消息来自绑定数据，而不是硬编码 UI。

### 阶段二：跑通单页美化最小闭环

目标：在新界面中复用单页美化能力，先完成当前选中页的生成和渲染。

步骤：

1. 添加 `PptxGenerator.Core` 项目引用。
2. 迁移 WPF 渲染适配层。
   - `WpfDispatcher`
   - `WpfPreviewImage`
   - `WpfSlideMlRenderEngine`
   - 必要的 Brush、Image、Preview converters
3. 新增 `SlideChatManagerFactory` 或等价服务。
   - 负责创建渲染管道、评估器和 `SlideChatManager`。
   - 避免把大量创建逻辑放入 `App.xaml.cs`。
4. 改造启动入口。
   - 移除 `StartupUri`。
   - 在 `App.xaml.cs` 中创建 ViewModel 并赋给主窗口，或引入轻量 DI。
5. 在 `MainWindowViewModel` 中接入生成命令。
   - `SendMessageCommand`
   - `RerenderCommand`
   - `EvaluateCommand` 可先保留入口，按阶段接入。
6. 在主内容区显示预览图、当前 SlideML、回填 XML 和渲染警告。

验收标准：

- 输入单页美化需求后可以生成 SlideML。
- 可以渲染预览图。
- 可以显示当前 SlideML、回填 XML 和警告。
- 可以手工修改 SlideML 并重新渲染。

### 阶段三：扩展为课件级工作流

目标：从单页闭环扩展为多页课件美化。

步骤：

1. 建立课件页模型。
   - 每页包含页码、标题、原始内容、当前 SlideML、预览图、渲染状态、评估状态。
2. 建立当前页选择逻辑。
   - 左侧选择页后，中间区域切换当前页内容。
   - 聊天上下文可感知当前页。
3. 增加批量美化命令。
   - 美化当前页。
   - 美化选中页。
   - 美化全部页面。
4. 增加取消能力。
   - 长时间生成或批量处理时支持取消。
   - 取消时返回非成功状态并更新 UI。
5. 增加处理进度和日志。
   - 全局进度。
   - 当前页状态。
   - 每页渲染和评估日志。

验收标准：

- 可以管理多页课件。
- 可以选择单页查看和编辑。
- 可以对多页执行美化处理。
- UI 能清晰展示每页处理状态和错误信息。

### 阶段四：接入评估、提示词迭代和增强能力

目标：补齐现有单页 Demo 的增强能力，并适配课件级流程。

步骤：

1. 接入页面评估。
   - 当前页评估。
   - 批量评估。
   - 评估结果绑定到 UI。
2. 接入提示词评估和迭代。
   - 保留单页评估入口。
   - 后续扩展为课件整体提示词评估。
3. 接入图片附件。
   - 复用或改造现有聊天控件图片附件能力。
   - 附件与当前页上下文关联。
4. 接入 MCP 渲染。
   - 迁移 `SwitchableSlideMlRenderPipeline`。
   - 迁移 MCP 渲染结果 DTO。
   - 添加连接状态、失败提示和本地回退。
5. 增加模型配置入口。
   - 模型列表、当前模型选择、配置来源。
   - 禁止硬编码本地私有路径。

验收标准：

- 当前页可执行 AI 评估。
- 提示词评估和迭代可用。
- 图片附件可参与聊天上下文。
- MCP 渲染可启用，失败时能回退本地渲染。

### 阶段五：整理工程质量和用户体验

目标：完成可维护、可扩展的课件美化 Demo。

步骤：

1. 统一资源和样式。
   - 消除主要内联颜色和重复样式。
   - 补齐 TextBox、ComboBox、卡片、缩略图样式。
2. 本地化用户可见文本。
   - 后续如需要正式产品化，将用户可见字符串迁移到资源文件。
3. 完善错误处理。
   - 显示可理解的错误信息。
   - 保留调试所需日志。
   - 不吞异常。
4. 完善取消和并发保护。
   - 防止重复点击导致并发生成同一页。
   - 批处理时控制并发数量。
5. 增加测试。
   - 优先测试服务层和状态流转。
   - UI 逻辑尽量通过 ViewModel 测试覆盖。

验收标准：

- 编译无错误。
- 主流程可稳定运行。
- UI 样式集中维护。
- 关键服务和 ViewModel 状态流转具备测试覆盖。

## 实施优先级

建议按以下顺序推进：

1. 视觉框架和 MVVM 假数据。
2. 单页生成与本地渲染闭环。
3. 多页课件状态管理。
4. 批量美化与取消。
5. 评估、提示词迭代、附件和 MCP。
6. 样式整理、错误处理和测试。

## 关键决策

- 第一版需要支持 MCP 渲染，并保持和 `PptxGeneratorWpfDemo` 相同的 MCP 能力。
- Agent 配置来源与 `PptxGeneratorWpfDemo` 保持完全相同。
- 课件输入来源为一个文件夹，文件夹内容符合 `Docs/CoursewareMarkdownExportFormat.md` 格式。
- 课件级美化保留每页独立聊天上下文，避免跨页干扰。
- 批量美化第一版采用串行处理，后续可根据性能需求调整为限制并发处理。
- 不需要为课件输入文件夹增加最近打开记录。
- 课件级美化结果输出到新文件夹，输出格式为 SlideML，不输出为 Markdown。
- 每页独立聊天上下文保存到日志文件夹。
- 视觉稿静态内容只作为 UI 参考，正式实现必须数据驱动。
- 新增控件命名使用 `ChatUserControl`，不沿用疑似拼写错误的 `CharUserControl`。
- 优先复用 `PptxGenerator.Core`，避免在 WPF 项目中重复实现核心生成逻辑。

## 后续需要确认的问题

- 日志文件夹的路径规则：放在输出文件夹下，还是与输入文件夹并列。
- 输出 SlideML 文件的命名规则是否沿用输入 Markdown 文件名。

