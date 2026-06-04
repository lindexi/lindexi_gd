# CharUserControl 聊天列表优化计划

## 目标

将 `CharUserControl.axaml` 中 `CopilotChatMessage` 的模板从「仅展示 `Content` 文本」升级为「按 `MessageItems` 列表逐项渲染」，使每条消息能区分展示：普通文本、思考过程、工具调用/输出、审批工具、子智能体嵌套内容、以及工具输出中的图片。

---

## 现状分析

### 当前绑定方式

```xml
<!-- CharUserControl.axaml 当前状态 -->
<DataTemplate DataType="agent:CopilotChatMessage">
    <Border ...>
        <StackPanel Spacing="6">
            <TextBlock Text="{Binding Author}" ... />
            <TextBlock Text="{Binding Content}" ... />   <!-- 只显示 Content -->
        </StackPanel>
    </Border>
</DataTemplate>
```

**问题：**

- `Content` 属性是 `MessageItems` 中所有 `CopilotChatTextItem` 的拼接，丢失了项的类型信息和顺序
- 模型的思考过程（`CopilotChatReasoningItem`）无法与输出内容区分显示
- 工具调用/输出（`CopilotChatToolItem`）不可见
- 子智能体（`CopilotChatSubAgentItem`）的嵌套内容不可见
- 工具输出中如果包含图片资源（如 `ImageSource`）无法展示
- 审批工具（`CopilotChatApprovalToolItem`）的交互状态不可见

### 数据模型结构

```
CopilotChatMessage
├── Role: ChatRole
├── Author: string
├── Content: string                        ← 仅汇总 CopilotChatTextItem.Text
├── Reason: string                         ← 仅汇总 CopilotChatReasoningItem.Text
├── MessageItems: ObservableCollection<ICopilotChatMessageItem>
│   ├── CopilotChatTextItem               ← Text: string
│   ├── CopilotChatReasoningItem          ← Text: string
│   ├── CopilotChatToolItem               ← CallId / ToolName / InputText / OutputText
│   ├── CopilotChatApprovalToolItem       ← CallId / ToolName / InputText / OutputText / ApprovalState / ...
│   └── CopilotChatSubAgentItem           ← CallId / ToolName / InputText / OutputText / MessageItems (嵌套)
└── UsageDetails: UsageDetails?
```

`ICopilotChatMessageItem` 接口当前为空标记接口（无公共成员），需要采用 **TemplateSelector** 方式按具体类型分发模板。

---

## 设计方案

### 方案选择：Avalonia `DataTemplateSelector`

在 Avalonia 中，`ItemsControl.ItemTemplate` 不能像 WPF 那样通过 `DataTemplate.DataType` 自动按类型选择。需要使用 **`IDataTemplate` 的 `Match` 方法**或创建 **`CopilotChatMessageItemTemplateSelector`** 类来按具体 `ICopilotChatMessageItem` 子类型返回不同的 `IDataTemplate`。

参考：`Blog/RallreakechuFeakenalldea/AvaloniaAgentLib/View/CopilotChatMessageItemTemplateSelector.cs`（现有的参考实现）。

### 模板分发策略

| 具体类型 | 视觉风格 | 关键内容 |
|---|---|---|
| `CopilotChatTextItem` | 普通文本块，与当前 `Content` 显示风格一致 | `Text` |
| `CopilotChatReasoningItem` | 灰色/斜体，可折叠区域，左侧有"思考"标签 | `Text`（斜体、小字号、灰色背景） |
| `CopilotChatToolItem` | 可折叠卡片，头显示工具名 + 调用状态 | `ToolName`、`InputText`（折叠）、`OutputText`（含图片支持） |
| `CopilotChatApprovalToolItem` | 可折叠卡片，显示审批状态 | `ToolName`、`ApprovalStateText`、输入/输出 |
| `CopilotChatSubAgentItem` | 可折叠卡片，内嵌递归 `MessageItems` 列表 | `ToolName`、嵌套 `MessageItems` → 递归使用同套模板 |

### 输出内容类型检测

对于 `CopilotChatToolItem.OutputText` 和 `CopilotChatSubAgentItem.OutputText`，可能包含：
- 纯文本
- 图片路径/URL（如 `![image](path)` 或 base64）
- 混合内容

需要设计一个转换逻辑（或 ViewModel 层属性），将输出文本解析为 `IList<OutputPart>`，然后在模板中按类型渲染。

---

## 实施步骤

### Step 1: 创建 `CopilotChatMessageItemTemplateSelector`

**文件：** `Code/PptxGenerator/Core/Ui/CopilotChatMessageItemTemplateSelector.cs`

- 实现 `IDataTemplate`
- 在 `Match(object? data)` 中按具体类型返回 `true`
- 在 `Build(object? data)` 中返回对应类型的 `FuncDataTemplate`
- 每种 `ICopilotChatMessageItem` 子类型对应一个 `FuncDataTemplate`

**每种子类型模板的视觉设计：**

#### CopilotChatTextItem 模板

```csharp
new FuncDataTemplate<CopilotChatTextItem>((_, _) =>
    new TextBlock { 
        TextWrapping = TextWrapping.Wrap,
        FontSize = 14,
        Foreground = Brush.Parse("#FF1E293B")
    }.Bind(TextBlock.TextProperty, nameof(CopilotChatTextItem.Text)))
```

#### CopilotChatReasoningItem 模板

- 整体放在一个可展开/折叠的 `Border` 中
- 头：灰色小字 "思考过程" + 展开指示符
- 体：灰色背景、小号斜体 `TextBlock`

```csharp
new FuncDataTemplate<CopilotChatReasoningItem>((_, _) =>
    new Border { ... 灰色背景、左侧竖线 ...
        Child = new TextBlock {
            FontStyle = FontStyle.Italic,
            FontSize = 12,
            Foreground = Brush.Parse("#FF64748B"),
            TextWrapping = TextWrapping.Wrap
        }.Bind(TextBlock.TextProperty, nameof(CopilotChatReasoningItem.Text))
    })
```

#### CopilotChatToolItem 模板

- 可折叠卡片
- 头：圆角小标签显示工具名，右侧状态指示
- 体（折叠区）：
  - "输入"标签 + `InputText`（等宽字体）
  - "输出"标签 + `OutputText`（支持文本和图片）

```csharp
new FuncDataTemplate<CopilotChatToolItem>((_, _) =>
    new Border { /* 圆角边框 */ 
        Child = new StackPanel {
            Children = {
                /* 头部：工具名 */
                new TextBlock { ... }.Bind(TextBlock.TextProperty, nameof(CopilotChatToolItem.ToolName)),
                /* 体：InputText / OutputText */
                ...
            }
        }
    })
```

#### CopilotChatSubAgentItem 模板

- 类似 `CopilotChatToolItem`，但体内部递归使用同一个 `ItemsControl` 绑定 `MessageItems`
- 关键技术：在 `FuncDataTemplate` 内创建的 `ItemsControl` 需要设置 `ItemTemplate` 为同一个 `CopilotChatMessageItemTemplateSelector`

### Step 2: 修改 `CopilotChatMessage` 的 DataTemplate

**文件：** `Code/PptxGenerator/Core/Ui/Views/CharUserControl.axaml`

将原来的气泡内 `StackPanel` 改为：

```xml
<StackPanel Spacing="6">
    <!-- 作者名 -->
    <TextBlock Text="{Binding Author}" FontWeight="Bold" FontSize="12" ... />

    <!-- 按 MessageItems 逐项渲染 -->
    <ItemsControl ItemsSource="{Binding MessageItems}"
                  ItemTemplate="{StaticResource ChatMessageItemTemplateSelector}" />

    <!-- Token 用量信息（如存在） -->
    <TextBlock Text="{Binding UsageSummaryText}" 
               FontSize="10"
               Foreground="#FF94A3B8"
               IsVisible="{Binding HasUsageDetails}" />
</StackPanel>
```

### Step 3: 注册 TemplateSelector 到 XAML 资源

**文件：** `Code/PptxGenerator/App.axaml`

在 `<Application.Resources>` 中注册：

```xml
<Application.Resources>
    <vm:CopilotChatMessageItemTemplateSelector x:Key="ChatMessageItemTemplateSelector" />
</Application.Resources>
```

或者在 `CharUserControl.axaml` 的 `<UserControl.Resources>` 中注册。

### Step 4: 输出中的图片支持

如果 `CopilotChatToolItem.OutputText` 包含 Markdown 图片语法 `![alt](url)` 或直接是图片 URL：
- 方案 A：在 ViewModel 层增加一个 `OutputParts` 集合属性，将输出文本解析为 `(Type: Text|Image, Content: string)` 列表，模板按 `OutputParts` 渲染
- 方案 B：在模板中使用自定义 `MarkdownTextBlock` 控件（如果有）
- 方案 C：解析到 `ObservableCollection<IChatOutputPart>`，类似 MessageItems 再用一层 TemplateSelector

**推荐方案 A** —— 最小改动，在 `CopilotChatToolItem` 或渲染层增加解析逻辑。

### Step 5: 编译验证

- 构建项目，确保无编译错误
- 验证 Avalonia 编译绑定（`AvaloniaUseCompiledBindingsByDefault=true`）正确

### Step 6: 后续优化（可选）

- `CopilotChatReasoningItem` 可折叠动画
- `CopilotChatToolItem` 输入/输出可折叠
- `CopilotChatApprovalToolItem` 审批按钮交互

---

## 关键文件

| 文件 | 操作 | 说明 |
|---|---|---|
| `Core/Ui/CopilotChatMessageItemTemplateSelector.cs` | 新建 | 按 `ICopilotChatMessageItem` 子类型分发模板 |
| `Core/Ui/Views/CharUserControl.axaml` | 修改 | 将气泡内容改为 `ItemsControl` 绑定 `MessageItems` |
| `App.axaml` 或 `CharUserControl.axaml` | 修改 | 注册 `TemplateSelector` 资源 |
| `Core/Ui/ChatBubbleConverters.cs` | 不变 | 气泡对齐/颜色转换器不变 |

---

## 风险与待定项

- **`ICopilotChatMessageItem` 是空接口**：无法定义公共属性，所有绑定依赖具体类型，TemplateSelector 需要按 5 种具体类型分发
- **编译绑定兼容**：`FuncDataTemplate` 内使用 `Bind()` 扩展方法时需注意编译绑定是否可用；如果不可用则使用反射绑定
- **`CopilotChatSubAgentItem` 嵌套递归**：`CopilotChatSubAgentItem` 内部又有 `MessageItems`，需要递归使用同一套 TemplateSelector。在 `FuncDataTemplate` 中创建 `ItemsControl` 时，需要动态引用 TemplateSelector 实例（通过静态单例）
- **图片渲染**：工具输出中的图片如何处理取决于实际输出格式，建议先实现文本渲染，图片作为后续增强
- **性能**：`ObservableCollection` 逐项变化时，`ItemsControl` 会逐个更新，目前消息量不大，不是瓶颈

---

## 待确认问题

1. `CopilotChatToolItem.OutputText` 中图片的实际格式是什么？（Markdown `![alt](url)` / base64 / 文件路径？）
2. 是否需要支持 `ApprovalToolItem` 的审批交互按钮？（审批是在 UI 侧完成还是在 Chat 流内完成？）
3. `CopilotChatReasoningItem` 是否默认折叠？（预计模型推理思考过程较长，建议默认折叠）

---

> 创建时间: 2025-XX-XX | 状态: 待审阅
