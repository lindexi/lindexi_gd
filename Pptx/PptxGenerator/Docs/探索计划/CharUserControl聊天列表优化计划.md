# CharUserControl 聊天列表优化计划

## 目标

将 `CharUserControl.axaml` 中 `CopilotChatMessage` 的模板从「仅展示 `Content` 文本」升级为「按 `MessageItems` 列表逐项渲染」，使每条消息能区分展示：普通文本、思考过程、工具调用/输出、审批工具、子智能体嵌套内容、以及工具输出中的图片。同时做好设计时支持，并改造渲染链路以支持多模态截图反馈。

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
│   ├── CopilotChatImageItem              ← Data: BinaryData / MimeType: string
│   ├── CopilotChatAudioItem              ← Data: BinaryData / MimeType: string
│   ├── CopilotChatToolItem               ← CallId / ToolName / InputText / OutputText
│   ├── CopilotChatApprovalToolItem       ← CallId / ToolName / InputText / OutputText / ApprovalState / ...
│   └── CopilotChatSubAgentItem           ← CallId / ToolName / InputText / OutputText / MessageItems (嵌套)
└── UsageDetails: UsageDetails?
```

`ICopilotChatMessageItem` 接口当前为空标记接口（无公共成员），需要采用 **TemplateSelector** 方式按具体类型分发模板。

---

## 设计方案

### 方案选择：Avalonia `IDataTemplate` + 属性注入

在 Avalonia 中，`ItemsControl.ItemTemplate` 不能像 WPF 那样通过 `DataTemplate.DataType` 自动按类型选择。需要使用实现 **`IDataTemplate`** 的 **`CopilotChatMessageItemTemplateSelector`** 类来按具体 `ICopilotChatMessageItem` 子类型返回不同的 `IDataTemplate`。

**关键设计决策：模板定义在 XAML 中，通过属性注入到 TemplateSelector。**

参考实现：`Blog/RallreakechuFeakenalldea/AvaloniaAgentLib/View/CopilotChatMessageItemTemplateSelector.cs`

该参考实现的核心模式：
- `CopilotChatMessageItemTemplateSelector` 实现 `IDataTemplate`
- 暴露 `TextItemTemplate`、`ReasoningItemTemplate`、`ToolItemTemplate`、`ApprovalToolItemTemplate`、`SubAgentItemTemplate` 五个 `IDataTemplate?` 属性
- `Match(object? data)` 匹配 `ICopilotChatMessageItem`
- `Build(object? param)` 按具体类型 switch 选择对应属性，调用其 `Build(param)`
- **不在 C# 代码中构造控件**，所有模板在 XAML 中定义后赋值给选择器属性

### 模板分发策略

| 具体类型 | 视觉风格 | 关键内容 |
|---|---|---|
| `CopilotChatTextItem` | 普通文本块，与当前 `Content` 显示风格一致 | `Text` |
| `CopilotChatReasoningItem` | 灰色/斜体，左侧有"思考"标签，**始终展开不折叠** | `Text`（斜体、小字号、灰色背景） |
| `CopilotChatToolItem` | 可折叠卡片，头显示工具名，体折叠输入/输出 | `ToolName`、`InputText`（折叠）、`OutputText`（折叠） |
| `CopilotChatApprovalToolItem` | 不渲染，抛 `NotSupportedException` | — |
| `CopilotChatSubAgentItem` | 与 `CopilotChatToolItem` 相同处理，不特殊对待 | `ToolName`、`InputText`、`OutputText` |
| `CopilotChatImageItem` | 图片展示 | `Data`（`BinaryData`）+ `MimeType` |
| `CopilotChatAudioItem` | 音频提示（暂不渲染交互控件，显示占位文本） | `Data` + `MimeType` |

### 图片支持分析（SlideRenderer 链路）

经过代码链路分析，图片**不在** `CopilotChatToolItem.OutputText` 中：

```
SlideRenderTool.RenderSlideAsync()
  → SlideRenderer.RenderAsync()
    → 返回 SlideRenderResult { PreviewBitmap: Bitmap, OutputXml, Warnings }
  → 缓存: LatestPreviewBitmap = renderResult.PreviewBitmap
  → 返回给 AI 的文本: 回填后的 XML + 警告列表（纯文本）
```

- `PreviewBitmap` 是 `Avalonia.Media.Imaging.Bitmap`，缓存在 `SlideRenderTool.LatestPreviewBitmap`
- 通过 `SlideChatManager.PreviewBitmap` → `MainWindowViewModel.PreviewBitmap` 暴露给 ViewModel
- **工具返回给模型的 OutputText 是纯文本**（渲染后的 XML + 警告），不包含图片
- 图片展示应通过 ViewModel 层的 `PreviewBitmap` 属性绑定，而非解析 OutputText

因此 Step 4 中"解析 OutputText 中的 Markdown 图片"的方案不适用。图片支持分为两个层面：
1. **用户输入中的图片**：通过 `CopilotChatImageItem`（`BinaryData` + `MimeType`）已在 MessageItems 中
2. **SlideML 渲染预览图**：通过 `MainWindowViewModel.PreviewBitmap` 绑定，不在消息气泡内

### 多模态截图反馈（SlideML V2 §9）

参考 `Docs/SlideML V2 规划文档.md` 的「9. 多模态截图反馈」：

> 对支持图片输入的模型，渲染引擎在每轮渲染后附加页面截图，模型可以从视觉层面评估：颜色搭配是否协调、间距是否美观、元素是否对齐、整体设计风格是否统一。

**核心思路：** 在 `SlideRenderTool` 渲染完成后，将生成的 `PreviewBitmap`（位图截图）作为多模态图片内容，附加到下一轮模型调用的上下文中。这样模型可以"看到"渲染结果，从视觉层面自我评估并修正。

**实现方案：** 修改 `SlideChatManager.SendSlideRequestAsync`（及 `SendContinueRequestAsync`）链路，在每轮 `render_slide` 工具调用完成后，将 `PreviewBitmap` 编码为 base64，通过 `CopilotChatImageItem` 注入到当前 Assistant 消息的 `MessageItems` 中。后续轮次的模型调用会自然地将该图片作为上下文参考。

**链路改造涉及文件：**
- `SlideChatManager.cs` — 在工具结果返回后注入截图 ImageItem
- 可能需要 `SlideRenderTool` 暴露渲染完成的回调/事件

### 设计时支持

为提升开发体验，需要在 XAML 中提供设计时数据：

- 在 `CharUserControl.axaml` 中使用 `d:DataContext` 和 `d:DesignInstance` 指定设计时 DataContext
- 为 `CopilotChatMessage` 和各类 `ICopilotChatMessageItem` 提供设计时示例数据，使 Visual Studio / Rider 设计器能预览模板外观
- 如需，创建 `DesignTimeData` 静态类或资源文件，提供模拟数据

具体措施：
1. `CharUserControl.axaml` 中添加 `d:DesignWidth="800" d:DesignHeight="600"`
2. 在 `<UserControl.Resources>` 或通过 `d:DataContext` 提供设计时 ViewModel 实例
3. 对于各子类型 DataTemplate，利用 `d:DataType` 已指定类型，编译器/设计器可解析绑定

---

## 实施步骤

| 步骤 | 状态 | 说明 |
|---|---|---|
| Step 1 | ✅ 已完成 | 创建 `CopilotChatMessageItemTemplateSelector` 类 |
| Step 2 | ✅ 已完成 | 在 XAML 中定义各子类型 DataTemplate 并组装 TemplateSelector |
| Step 3 | ✅ 已完成 | 修改 `CopilotChatMessage` 的 DataTemplate |
| Step 4 | ✅ 已完成 | `BinaryDataToBitmapConverter` — 用户输入图片支持 |
| Step 5 | ✅ 已完成 | 多模态截图反馈 — 链路改造 |
| Step 6 | ✅ 已完成 | 设计时支持 |
| Step 7 | ✅ 已完成 | 编译与逻辑正确性验证 |
| Step 8 | ✅ 已完成 | CLI 端到端验证 |

> 状态图例：⬜ 待开始 | 🔄 进行中 | ✅ 已完成 | ❌ 已取消

---

### Step 1: 创建 `CopilotChatMessageItemTemplateSelector` 类

**文件：** `Code/PptxGenerator/Core/Ui/CopilotChatMessageItemTemplateSelector.cs`（新建）

参考 `Blog/RallreakechuFeakenalldea/AvaloniaAgentLib/View/CopilotChatMessageItemTemplateSelector.cs` 的模式：

- 实现 `IDataTemplate`
- 暴露可绑定的 `IDataTemplate?` 属性（由 XAML 赋值）：
  - `TextItemTemplate`
  - `ReasoningItemTemplate`
  - `ToolItemTemplate`
  - `ApprovalToolItemTemplate`
  - `SubAgentItemTemplate`
  - `ImageItemTemplate`
  - `AudioItemTemplate`
- `Match(object? data)` → `data is ICopilotChatMessageItem`
- `Build(object? param)` → 按具体类型 switch，返回对应属性的 `Build(param)` 结果
- 对于 `CopilotChatApprovalToolItem`，`Build()` 中抛出 `NotSupportedException`
- **不在 C# 中构造任何控件**，所有模板定义在 XAML 中

### Step 2: 在 XAML 中定义各子类型的 DataTemplate 并赋值给 TemplateSelector

**文件：** `Code/PptxGenerator/Core/Ui/Views/CharUserControl.axaml`

在 `<UserControl.Resources>` 中定义以下模板，并组装 TemplateSelector：

#### CopilotChatTextItem 模板

```xml
<DataTemplate x:Key="ChatTextItemTemplate" x:DataType="agent:CopilotChatTextItem">
    <TextBlock Text="{Binding Text}"
               TextWrapping="Wrap"
               FontSize="14"
               Foreground="#FF1E293B" />
</DataTemplate>
```

#### CopilotChatReasoningItem 模板

- 灰色背景、左侧竖线、斜体小字
- **始终展开，不折叠**

```xml
<DataTemplate x:Key="ChatReasoningItemTemplate" x:DataType="agent:CopilotChatReasoningItem">
    <Border Background="#FFF1F5F9"
            BorderBrush="#FFCBD5E1"
            BorderThickness="1,0,0,0"
            CornerRadius="4"
            Padding="12,8"
            Margin="0,2">
        <StackPanel Spacing="4">
            <TextBlock Text="思考过程"
                       FontSize="11"
                       Foreground="#FF94A3B8"
                       FontWeight="Medium" />
            <TextBlock Text="{Binding Text}"
                       FontStyle="Italic"
                       FontSize="12"
                       Foreground="#FF64748B"
                       TextWrapping="Wrap" />
        </StackPanel>
    </Border>
</DataTemplate>
```

#### CopilotChatToolItem 模板

- 可折叠卡片，头显示工具名，点击展开/折叠
- 折叠区：输入文本（等宽字体）+ 输出文本

```xml
<DataTemplate x:Key="ChatToolItemTemplate" x:DataType="agent:CopilotChatToolItem">
    <Border BorderBrush="#FFE2E8F0"
            BorderThickness="1"
            CornerRadius="8"
            Padding="0"
            Margin="0,2">
        <StackPanel Spacing="0">
            <!-- 头部：工具名 + 展开/折叠按钮 -->
            <Button ...>
                <StackPanel Orientation="Horizontal" Spacing="6">
                    <TextBlock Text="🔧" FontSize="12" />
                    <TextBlock Text="{Binding ToolName}" FontSize="12" FontWeight="SemiBold" Foreground="#FF334155" />
                    <TextBlock Text="▼" FontSize="10" Foreground="#FF94A3B8" />
                </StackPanel>
            </Button>
            <!-- 折叠体：输入/输出 -->
            <StackPanel IsVisible="{Binding IsExpanded}" Spacing="6" Padding="12,8">
                <StackPanel IsVisible="{Binding HasInputText}" Spacing="2">
                    <TextBlock Text="输入" FontSize="10" Foreground="#FF94A3B8" FontWeight="Medium" />
                    <Border Background="#FFF8FAFC" Padding="8" CornerRadius="4">
                        <TextBlock Text="{Binding InputText}"
                                   FontFamily="Cascadia Code,Consolas,monospace"
                                   FontSize="11"
                                   Foreground="#FF475569"
                                   TextWrapping="Wrap" />
                    </Border>
                </StackPanel>
                <StackPanel IsVisible="{Binding HasOutputText}" Spacing="2">
                    <TextBlock Text="输出" FontSize="10" Foreground="#FF94A3B8" FontWeight="Medium" />
                    <Border Background="#FFF8FAFC" Padding="8" CornerRadius="4">
                        <TextBlock Text="{Binding OutputText}"
                                   FontSize="12"
                                   Foreground="#FF334155"
                                   TextWrapping="Wrap" />
                    </Border>
                </StackPanel>
            </StackPanel>
        </StackPanel>
    </Border>
</DataTemplate>
```

#### CopilotChatSubAgentItem 模板

与 `CopilotChatToolItem` 相同处理，绑定 `ToolName`、`InputText`、`OutputText`。**不递归渲染嵌套 MessageItems**。

#### CopilotChatImageItem 模板

```xml
<DataTemplate x:Key="ChatImageItemTemplate" x:DataType="agent:CopilotChatImageItem">
    <!-- 将 BinaryData 转换为 Bitmap 后展示 -->
    <Image Source="{Binding Data, Converter={x:Static vm:BinaryDataToBitmapConverter.Instance}}"
           MaxWidth="320"
           Stretch="Uniform"
           Margin="0,4" />
</DataTemplate>
```

#### CopilotChatAudioItem 模板

```xml
<DataTemplate x:Key="ChatAudioItemTemplate" x:DataType="agent:CopilotChatAudioItem">
    <Border Background="#FFF1F5F9" CornerRadius="8" Padding="12,8">
        <TextBlock Text="{Binding DisplayText}"
                   FontSize="12"
                   Foreground="#FF64748B" />
    </Border>
</DataTemplate>
```

#### CopilotChatApprovalToolItem

不定义模板。在 TemplateSelector 的 `Build()` 中遇到此类型直接抛出 `NotSupportedException`。

#### 组装 TemplateSelector

```xml
<ui:CopilotChatMessageItemTemplateSelector x:Key="ChatMessageItemTemplateSelector"
    TextItemTemplate="{StaticResource ChatTextItemTemplate}"
    ReasoningItemTemplate="{StaticResource ChatReasoningItemTemplate}"
    ToolItemTemplate="{StaticResource ChatToolItemTemplate}"
    SubAgentItemTemplate="{StaticResource ChatToolItemTemplate}"
    ImageItemTemplate="{StaticResource ChatImageItemTemplate}"
    AudioItemTemplate="{StaticResource ChatAudioItemTemplate}" />
```

> 注：`SubAgentItemTemplate` 复用 `ChatToolItemTemplate`，不做特殊处理。

### Step 3: 修改 `CopilotChatMessage` 的 DataTemplate

**文件：** `Code/PptxGenerator/Core/Ui/Views/CharUserControl.axaml`

将原来的气泡内 `StackPanel` 改为：

```xml
<StackPanel Spacing="6">
    <!-- 作者名 -->
    <TextBlock Text="{Binding Author}"
               FontWeight="Bold"
               FontSize="12"
               Foreground="{Binding Role, Converter={x:Static vm:ChatBubbleAuthorColorConverter.Instance}}" />

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

### Step 4: 用户输入图片支持 — `BinaryDataToBitmapConverter`

**背景：** 经过代码链路分析，`SlideRenderTool` 返回给模型的 OutputText 是纯文本（回填后的 XML + 警告列表），不包含图片。图片分为两个独立渠道：

1. **用户输入中的图片**：已通过 `CopilotChatImageItem`（`BinaryData` + `MimeType`）存在于 `MessageItems` 中，由 Step 2 的 `ChatImageItemTemplate` 渲染。需要实现 `BinaryDataToBitmapConverter` 将 `BinaryData` 转换为 Avalonia `Bitmap`。

2. **SlideML 渲染预览图**：通过 `SlideRenderTool.LatestPreviewBitmap` → `SlideChatManager.PreviewBitmap` → `MainWindowViewModel.PreviewBitmap` 暴露。这不在消息气泡内展示，而是由 ViewModel 层绑定到独立的预览区域（当前已在 ViewModel 中暴露，后续可在 UI 中绑定）。此外，多模态截图反馈（Step 5）也会将预览图注入到消息流中。

**本步骤需要：**
- 新建 `BinaryDataToBitmapConverter`（`IValueConverter`），将 `BinaryData` 转为 `Bitmap`
- 确保 `CopilotChatImageItem` 的 `Data` 属性可被正确绑定和渲染

### Step 5: 多模态截图反馈 — 链路改造

**目标：** 实现 SlideML V2 规划文档 §9 的多模态截图反馈机制。

**背景：**
- `SlideRenderTool.RenderSlideAsync()` 渲染后生成 `PreviewBitmap`
- 当前 `PreviewBitmap` 仅在 ViewModel 层暴露给 UI，不会反馈给 AI 模型
- 对于支持图片输入的模型（如 GPT-4o），如果能将渲染截图作为下一轮上下文，模型可以"看到"视觉效果并自我修正

**实现方案：**

修改 `SlideChatManager`（或 `CopilotChatManager`），在每轮 `render_slide` 工具调用返回后：

1. 从 `SlideRenderTool.LatestPreviewBitmap` 获取渲染截图
2. 将截图编码为 PNG 字节数组（base64 或 `BinaryData`）
3. 创建 `CopilotChatImageItem`，插入到当前 Assistant 消息的 `MessageItems` 末尾
4. 后续轮次的模型调用自然包含该截图作为多模态上下文

**链路改动点：**

```
SlideRenderTool.RenderSlideAsync()
  → 完成后发出信号（事件/回调）
    → SlideChatManager 截获信号
      → 从 LatestPreviewBitmap 生成 CopilotChatImageItem
        → 追加到当前 CopilotChatMessage.MessageItems
```

**具体改造文件：**

| 文件 | 改动 |
|---|---|
| `SlideRenderTool.cs` | 暴露 `event Action<Bitmap>? PreviewRendered` 事件 |
| `SlideChatManager.cs` | 订阅事件，收到截图后注入 `CopilotChatImageItem` 到当前 Assistant 消息 |
| `CopilotChatManager.cs` | （可能需要）暴露当前正在构建的 Assistant 消息引用 |

**风险：**
- 截图 base64 数据量较大（1280×720 PNG 约 200-500KB），注意 token 消耗
- 部分模型/API 可能不支持图片输入或限制图片大小
- 可在 `SlideChatManager` 中通过配置开关控制是否启用截图反馈

### Step 6: 设计时支持

### Step 6: 设计时支持

为提升开发体验与设计器预览能力：

**6.1 CharUserControl.axaml 设计时 DataContext：**
- 确保 `<UserControl>` 已有 `d:DesignWidth="800" d:DesignHeight="450"`
- 添加 `xmlns:d="http://schemas.microsoft.com/expression/blend/2008"` 和 `xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"`（已有）
- 可选：通过 `d:DataContext` 指定设计时 ViewModel 实例

**6.2 子类型 DataTemplate 设计时预览：**
- 各 `DataTemplate` 已通过 `x:DataType` 指定具体类型，Avalonia 设计器可解析绑定路径
- 确保所有绑定的属性路径在设计时类型上存在

**6.3 样式资源抽取：**
- 将模板中使用的颜色（如 `#FFF1F5F9`、`#FF64748B`、`#FF334155` 等）抽取为 `SolidColorBrush` 资源键
- 定义在 `<UserControl.Resources>` 中，便于主题切换和设计时修改
- 将所有涉及的控件（如工具卡片头部 Button、输入输出边框等）定义显式样式

### Step 7: 编译与逻辑正确性验证

### Step 7: 编译与逻辑正确性验证

**7.1 编译验证：**
- 构建项目（`dotnet build`），确保无编译错误
- 验证 Avalonia 编译绑定正确

**7.2 逻辑正确性验证（CLI 端到端测试）：**

通过 `SlideCliRunner` 的 `dotnet run` 命令行机制进行端到端验证：

```bash
# 测试基本流程：发送 prompt，验证工具调用、渲染结果、输出文件
dotnet run --project Code/PptxGenerator -- "做一页介绍 SlideML 的幻灯片"

# 验证点：
# 1. 控制台输出包含 "[render_slide] 渲染完成"（工具调用成功）
# 2. 控制台输出 "生成完成"（流程正常结束）
# 3. artifacts/generated-slides/ 目录下生成 slide-*.xml、slide-*.rendered.xml、slide-*.png 文件
# 4. 控制台输出 Warnings 信息
# 5. 控制台输出 Final SlideML XML
```

**7.3 聊天列表 UI 验证（手动）：**
- 启动 Avalonia 应用
- 发送消息，观察聊天气泡中是否按 `MessageItems` 逐项渲染
- 验证 `CopilotChatTextItem` 正常显示文本
- 验证 `CopilotChatReasoningItem` 以灰色斜体显示思考内容
- 验证 `CopilotChatToolItem` 以可折叠卡片显示工具调用信息
- 验证 `CopilotChatImageItem` 正确渲染图片
- 验证 Token 用量信息显示

### Step 8: 后续优化（可选）

- `CopilotChatToolItem` 输入/输出折叠动画
- `CopilotChatAudioItem` 音频播放控件
- 渲染预览图在 UI 中的展示区域

---

## 关键文件

| 文件 | 步骤 | 操作 | 说明 |
|---|---|---|---|
| `Core/Ui/CopilotChatMessageItemTemplateSelector.cs` | Step 1 | 新建 | 实现 `IDataTemplate`，属性注入各子类型模板，按类型分发 |
| `Core/Ui/Views/CharUserControl.axaml` | Step 2, 3, 6 | 修改 | 定义各子类型 DataTemplate + 组装 TemplateSelector + 修改气泡内容 + 设计时支持 |
| `Core/Ui/BinaryDataToBitmapConverter.cs` | Step 4 | 新建 | `IValueConverter`，将 `BinaryData` 转为 Avalonia `Bitmap` |
| `Core/SlideMl/SlideRenderTool.cs` | Step 5 | 修改 | 暴露 `PreviewRendered` 事件 |
| `Core/SlideMl/SlideChatManager.cs` | Step 5 | 修改 | 订阅截图事件，注入 `CopilotChatImageItem` 到消息流 |
| `Core/Ui/ChatBubbleConverters.cs` | — | 不变 | 气泡对齐/颜色转换器不变 |

> 注：TemplateSelector 和模板均在 `CharUserControl.axaml` 的 `<UserControl.Resources>` 中注册，无需修改 `App.axaml`。

---

## 风险与待定项

- **`ICopilotChatMessageItem` 是空接口**：无法定义公共属性，所有绑定依赖具体类型，TemplateSelector 需要按 7 种具体类型分发
- **编译绑定兼容**：XAML 中 `x:DataType` 指定具体类型后，Avalonia 编译绑定可正常工作
- **`CopilotChatSubAgentItem`**：不做特殊处理，复用 `CopilotChatToolItem` 的模板，不递归渲染嵌套 `MessageItems`
- **`CopilotChatApprovalToolItem`**：遇到时抛 `NotSupportedException`，现阶段不支持审批交互
- **`CopilotChatToolItem` 折叠状态**：需要在 `CopilotChatToolItem` 模型上增加 `IsExpanded` 属性，或通过 ViewModel 包装。如模型不可改，可在模板中始终展开输入/输出
- **图片渲染**：`CopilotChatImageItem` 通过 `BinaryDataToBitmapConverter` 转换，需注意内存释放
- **`CopilotChatAudioItem`**：暂不实现播放控件，仅显示占位文本
- **多模态截图反馈**：
  - 截图 base64 数据量较大（1280×720 PNG 约 200-500KB），注意 token 消耗
  - 部分模型/API 可能不支持图片输入或限制图片大小
  - 可在 `SlideChatManager` 中通过配置开关控制是否启用截图反馈
  - `SlideRenderTool` 需要暴露事件机制，当前是纯函数式设计，需要扩展
- **设计时数据**：Avalonia 设计器对复杂 DataTemplate 的支持有限，可能需要额外 `DesignTimeData` 类
- **性能**：`ObservableCollection` 逐项变化时，`ItemsControl` 会逐个更新，目前消息量不大，不是瓶颈

---

## 待确认问题（已解决）

1. ~~图片实际格式~~ → 已确认：工具返回的 OutputText 是纯文本，图片通过 `CopilotChatImageItem`（`BinaryData` + `MimeType`）存在于 MessageItems 中，或通过 `PreviewBitmap` 在 ViewModel 层暴露
2. ~~审批交互~~ → 已确认：现阶段不需要，遇到 `CopilotChatApprovalToolItem` 抛出 `NotSupportedException`
3. ~~思考过程折叠~~ → 已确认：不折叠，始终展开显示

---

> 创建时间: 2025-XX-XX | 状态: 待审阅 | 最后更新: 2025-07-10
