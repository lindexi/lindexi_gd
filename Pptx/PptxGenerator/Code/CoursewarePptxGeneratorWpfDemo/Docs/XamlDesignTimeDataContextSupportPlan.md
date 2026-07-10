# XAML 设计时 DataContext 支持改造方案（审批稿）

> 适用范围：`CoursewarePptxGeneratorWpfDemo`（现有可运行 MVVM 示例），目标是让 XAML 在 Blend/Visual Studio 设计器中具备完整的设计时上下文和可视化数据。

## 1. 结论

当前 `CoursewarePptxGeneratorWpfDemo` 的主要问题不是“缺少控件”，而是设计器上下文缺失：

- `MainWindow.xaml` 有 `d`/`mc` 命名空间，但没有 `d:DataContext`
- `Views\LeftSidebarPanel.xaml` / `Views\MainContentPanel.xaml` / `Views\CopilotPanel.xaml` 完全依赖运行时 `DataContext`，设计器中通常为空
- `MainWindowViewModel` 为运行时 VM，构造函数依赖 `SlideChatManagerFactory` 和 `SlideChatManager`，不适合直接让设计器创建

已有“旧项目”`PptxGeneratorWpfDemo` 仅有 `d:DataContext` 类型提示（`IsDesignTimeCreatable=False`），对当前复杂 UI 的可视化价值有限。

## 2. 设计目标

1. 设计器内可正常解析并显示绑定（避免大量空值/设计时报错）
2. 支持在 Visual Studio/Blend 打开单文件时也能看到“近真实”界面
3. 不影响运行时 DI/启动链路，不改动核心业务逻辑
4. 采用最小侵入式改造，后续可逐步增强

## 3. 推荐实现方式

### 方案 A：快速补齐（低风险）

给 XAML 补 `d:DataContext`，使用类型提示（与运行时 VM 同名）

```xaml
xmlns:designTime="clr-namespace:..."
d:DataContext="{d:DesignInstance Type=...MainWindowViewModel, IsDesignTimeCreatable=False}"
```

**优点**：改动小，零运行时风险。

**缺点**：设计器里仍偏空，列表、日志、聊天消息等没有真实样式样本。

### 方案 B：推荐（推荐）

新增 `DesignTime` 命名空间下独立设计时 VM，并仅用于设计器：

- `DesignTimeMainWindowViewModel`
- （可选）`DesignTimeCoursewareSlideItem`
- （可选）`DesignTimeCommand`

核心点：

- XAML 设计时 DataContext 指向设计时 VM（`IsDesignTimeCreatable=True`）
- 提供假数据：
  - 页面集合（Slides）
  - 选中页（SelectedSlide）
  - 课程夹显示文本
  - SlideML/XML/日志字段
  - MCP 服务状态文本
  - 模型列表与当前选中模型
  - 输入文本示例
- 运行时 VM 不需改造，保持 `sealed` 和真实依赖不变

**优点**：

- 可视化效果最好（UI 可真正预览）
- 不污染业务 VM 与服务创建逻辑

**注意**：

- 不建议在运行时模型上做“只有设计器用”的字段新增
- 现有 `CoursewareSlideItem` 中有 `SlideChatManager` 这种运行时依赖，建议避免设计时实例化时构造真实对象，使用独立设计时 DTO 更安全

## 4. 关键文件落点

### 4.1 XAML 需要补充 `d:DataContext`

- `MainWindow.xaml`
- `Views\LeftSidebarPanel.xaml`
- `Views\MainContentPanel.xaml`
- `Views\CopilotPanel.xaml`

说明：

- `MainWindow` 给窗体级上下文
- 每个 UserControl 也加自己的 `d:DataContext`，便于单独打开 UserControl 时也能有设计时效果

### 4.2 新增设计时 VM（建议命名）

- `CoursewarePptxGeneratorWpfDemo/DesignTime/DesignTimeMainWindowViewModel.cs`
- 如需，`CoursewarePptxGeneratorWpfDemo/DesignTime/DesignTimeCoursewareSlideItem.cs`

### 4.3 设计时命令

- 使用最小 `ICommand` Stub，避免走真实业务
- 命令执行仅可选做 `Debug` 输出或无操作

## 5. 实施步骤（可直接交付）

1. 新建 `DesignTime` 目录与设计时 VM 类
2. 实现 `DesignTimeMainWindowViewModel`：准备可观察集合 + 示例字符串 + 只读状态属性 + 空实现命令
3. 在 `MainWindow.xaml`、`LeftSidebarPanel.xaml`、`MainContentPanel.xaml`、`CopilotPanel.xaml` 添加 `d:DataContext` 与 `designTime` 命名空间
4. 统一补 `mc:Ignorable="d"` 和 `d:DesignWidth/d:DesignHeight`（如缺失）
5. 仅在设计时绑定生效的情况下验证：
   - 课件列表可见且有多页示例
   - 中间预览/日志/XML 区有占位显示
   - Copilot 区能看到模型项、MCP 状态、输入框等
6. 可选优化：后续再补 `DesignTimeChatMessage` 的完整假消息模型，逐步将聊天区域做完整可视化

## 6. 风险与边界

- 聊天区域绑定到 `CopilotChatManager.ChatMessages`，若未补完整类型时会出现模板不渲染，需逐步补齐
- 本轮优先提升可编辑性与布局预览，不追求 `Copilot` 历史消息全部模拟为真实形态
- `CheawuchewalYicheredurlakearja` 项目当前多数 UI 为硬编码，暂不建议与本次方案混合改造；优先处理 `CoursewarePptxGeneratorWpfDemo`

## 7. 预期结果（审批项）

- 设计器不再因为缺失上下文而展示大量空值/绑定告警
- 开发期间可在 XAML 直接预览关键视图布局，减少“边写代码边猜绑定”成本
- 项目运行时行为不变（DI、命令、渲染管道等原逻辑保持）
