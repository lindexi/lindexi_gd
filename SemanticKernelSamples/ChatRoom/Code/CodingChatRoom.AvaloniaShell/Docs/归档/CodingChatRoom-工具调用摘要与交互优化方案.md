# CodingChatRoom 工具调用摘要与交互优化方案

## 1. 文档定位

本文定义 `CodingChatRoom.AvaloniaShell` 聊天界面中工具调用消息的交互优化方案，以及工具参数摘要的生成、注册、传递、持久化和测试设计。

本文作为后续实现基线，重点解决以下问题：

- 折叠状态下只能看到 `ReadFileLines`、`ReplaceStringInFile`、`run_tests` 等工具名，无法知道工具正在操作哪个文件、目录或项目。
- 不应在 Avalonia UI 中按工具名解析参数。
- 不应从已经格式化的 `InputText` 中反向解析参数。
- 不使用 Attribute 标记摘要参数。
- 不为摘要功能增加运行时反射扫描。
- 工具摘要规则应与工具注册代码相邻，并可由工具提供方独立扩展。
- 顶层 Agent、子代理、历史会话应使用一致的工具摘要。

本文不直接修改工具执行语义，也不改变工具传递给大模型的名称、参数描述和调用协议。

---

## 2. 已确认的现状

### 2.1 当前工具消息链路

工具调用的显示链路如下：

```text
CodingAgent
    ↓
AgentResponseUpdate
    ↓
CopilotChatManager.AppendAssistantResponseUpdate
    ↓
CopilotChatMessage.AppendFunctionCall / AppendFunctionResult
    ↓
CopilotChatToolItem
    ↓
MessageItemViewModel.MessageItems
    ↓
ChatMessageItemTemplateSelector
    ↓
ChatView.axaml 中的 AssistantToolItemTemplate
```

`FunctionCallContent` 到达时，`CopilotChatMessage` 创建或更新 `CopilotChatToolItem`；`FunctionResultContent` 到达时，根据 `CallId` 找到同一个工具项并写入输出。

### 2.2 参数已经存在

当前 `CopilotChatToolItem` 已保存：

- `CallId`
- `ToolName`
- `InputText`
- `OutputText`

工具输入来自 `FunctionCallContent.Arguments`，经过 `CopilotChatMessageItemFormatter` 转换后写入 `InputText`。

因此问题不是参数丢失，而是折叠标题只绑定了 `DisplayName`。参数必须展开卡片后才能看到。

### 2.3 当前直接根因

`Views/ChatView.axaml` 中普通工具模板的标题只显示：

```text
DisplayName
```

所以多个连续工具调用在折叠状态下只能显示：

```text
ReadFileLines
ReadFileLines
ReplaceStringInFile
ReplaceStringInFile
run_tests
run_build
```

用户无法快速判断每个调用的操作对象。

### 2.4 不能依赖 InputText 生成摘要

不应从 `InputText` 反向解析摘要，原因如下：

1. 单参数和多参数的现有格式不同。
2. 值本身可能包含换行和冒号。
3. `RawRepresentation` 可能因模型提供商不同而变化。
4. 大型字符串、源码和 JSON 不适合进入标题。
5. 子代理当前使用的参数格式化入口与顶层消息不完全一致。
6. 历史格式演进后，文本格式不应成为内部协议。

摘要必须在仍然持有结构化 `FunctionCallContent.Arguments` 时生成。

---

## 3. 产品目标

### 3.1 折叠状态目标

工具卡片折叠后应至少表达：

```text
工具行为 + 主要操作对象 + 必要补充信息
```

示例：

```text
ReadFileLines  Views\ChatView.axaml  第 70–150 行
ReplaceStringInFile  ViewModels\MessageItemViewModel.cs
运行测试  CodingChatRoom.AvaloniaShell.Tests.csproj
构建项目  CodingChatRoom.AvaloniaShell.csproj  Debug · net10.0
搜索内容  “ToolItemTemplate”  CodingChatRoom.AvaloniaShell
查找符号  CopilotChatToolItem
读取日志  第 1–100 行
```

### 3.2 展开状态目标

展开后继续显示完整信息：

- 完整输入参数。
- 完整工具输出。
- 完整路径。
- 后续可扩展复制输入、复制输出和打开路径等操作。

摘要不能替代现有 `InputText` 和 `OutputText`。

### 3.3 路径显示目标

- 标题优先显示文件名或最后一到两个路径段。
- Tooltip 显示完整路径。
- 完整输入区域继续显示原始路径。
- 路径过长时省略中间部分，优先保留末尾文件名。
- 空测试或构建目标显示为“整个工作区”。

### 3.4 普通工具状态边界

普通工具项不维护 `Running`、`Completed` 等执行状态，也不在标题、完整消息文本或历史快照中显示状态。

函数调用与函数结果仍通过 `CallId` 更新同一个工具项；是否存在输出只表示当前已记录的内容，不作为执行状态协议。

审批工具的 `ApprovalState` 属于用户决策流程，不在本次移除范围内。

---

## 4. 核心设计原则

1. **显式注册**：摘要规则在创建工具时与工具一起注册。
2. **不使用 Attribute**：不在工具方法或参数上增加摘要特性。
3. **不增加反射扫描**：摘要系统不扫描方法、参数或程序集。
4. **参数默认不展示**：只有摘要函数主动读取的参数才进入摘要。
5. **工具负责语义，UI 负责布局**：工具提供方决定哪些参数有意义，Avalonia 决定如何排列和截断。
6. **保留结构化结果**：摘要至少拆分为主要文本和次要文本，而不是只保存一个长字符串。
7. **生成后持久化**：历史会话保存生成后的展示快照，不依赖重新执行摘要函数。
8. **无全局可变注册表**：展示注册信息跟随工具集合或工具 Lease 生命周期。
9. **安全回退**：未知或未注册工具只显示原始工具名，不猜测参数。
10. **局部扩展**：复杂工具的摘要逻辑保留在工具所属项目，不让中央组件了解具体参数类型。

---

## 5. 为什么不需要 Attribute 和反射

在创建工具时，程序已经明确知道：

- 正在创建哪个 `AITool`。
- 该工具的名称。
- 哪些参数适合展示。
- 应该如何生成摘要。

因此可以把实际工具和摘要函数一起保存：

```text
工具注册 = AITool + 摘要生成函数
```

模型发起工具调用后，`FunctionCallContent` 已经包含：

- 工具名。
- 参数字典。

程序按工具名找到对应注册项，直接调用其中的摘要函数即可：

```text
注册工具时：
工具名 → AITool + 摘要函数

调用工具时：
工具名 → 找到注册项 → 参数字典生成摘要
```

例如：

- `ReadFileLines` 的摘要函数读取 `filePath/startLine/endLine`。
- `ReplaceStringInFile` 的摘要函数只读取 `filePath`。
- `run_tests` 的摘要函数读取 `targetPath/filter`。

摘要系统不需要查看方法签名，也不需要读取参数特性。

---

## 6. ToolRegistration 设计

### 6.1 定位

引入不可变注册对象 `ToolRegistration`，表达：

> 一个可以交给 Agent 使用的工具，以及该工具如何生成面向用户的调用摘要。

建议概念结构：

```csharp
internal sealed record ToolRegistration(
    AITool Tool,
    Func<IReadOnlyDictionary<string, object?>, ToolCallPresentation>? CreatePresentation = null);
```

具体类型可根据 `FunctionCallContent.Arguments` 的实际公开类型调整，避免无意义复制。

### 6.2 字段职责

#### Tool

实际提供给 `Microsoft.Extensions.AI` 和 Agent 的工具，不改变现有执行行为。

#### CreatePresentation

根据本次调用的结构化参数生成展示结果。

该委托：

- 与工具注册代码相邻。
- 只读取允许进入摘要的参数。
- 不执行文件系统、进程或网络操作。
- 不修改参数字典。
- 参数缺失或类型不符时安全回退，不应抛出影响聊天流程的异常。

### 6.3 没有摘要函数的工具

允许 `CreatePresentation` 为空。

回退行为：

- `DisplayName` 使用工具原名。
- `PrimaryText` 为空。
- `SecondaryText` 为空。
- 不自动选择任意参数进行展示。

这种默认行为可避免意外展示 Token、Prompt、文件全文或其他敏感内容。

---

## 7. ToolCallPresentation 设计

不建议只返回一个 `SummaryText`。建议返回结构化展示快照：

```csharp
public sealed record ToolCallPresentation(
    string? PrimaryText,
    string? SecondaryText,
    string? FullTargetText = null);
```

### 7.1 PrimaryText

本次调用最重要的操作对象，例如：

- 文件路径。
- 项目路径。
- 目录。
- 查询文本。
- 符号名。
- 可执行文件。

### 7.2 SecondaryText

必要但优先级较低的信息，例如：

- 行范围。
- 测试过滤器。
- 构建配置。
- 目标框架。
- 搜索目录。
- 操作数量。

窗口空间不足时，UI 可以优先隐藏或截断该字段。

### 7.3 FullTargetText

用于 Tooltip 或展开详情，一般保存完整路径或完整目标名。

### 7.4 兼容摘要

`CopilotChatToolItem` 可提供组合属性：

```text
SummaryText = PrimaryText + “ · ” + SecondaryText
```

该属性适用于：

- 复制整条消息。
- 人类可读日志。
- 无法分栏布局的界面。

UI 主模板仍应优先绑定独立字段。

---

## 8. 注册与执行链路

### 8.1 工具创建阶段

现有工具提供器通常直接返回：

```text
IReadOnlyList<AITool>
```

建议内部演进为：

```text
IReadOnlyList<ToolRegistration>
```

对 Agent 仍然投影为：

```text
registrations.Select(registration => registration.Tool)
```

因此 `Microsoft.Extensions.AI` 不需要理解自定义展示类型。

### 8.2 注册表示例

#### ReadFileLines

```csharp
new ToolRegistration(
    AIFunctionFactory.Create(ReadFileLines, name: nameof(ReadFileLines), description: "读取文件的指定行范围。"),
    arguments => ToolCallPresentationFactory.ForFileLineRange(
        arguments,
        filePathArgumentName: "filePath",
        startLineArgumentName: "startLine",
        endLineArgumentName: "endLine"));
```

#### ReplaceStringInFile

```csharp
new ToolRegistration(
    AIFunctionFactory.Create(ReplaceStringInFile, name: nameof(ReplaceStringInFile), description: "替换文件中唯一匹配的文本；替换前必须先读取文件。"),
    arguments => ToolCallPresentationFactory.ForPath(arguments, "filePath"));
```

摘要函数没有读取 `oldString` 和 `newString`，因此它们不会进入标题。

#### run_tests

```csharp
new ToolRegistration(
    AIFunctionFactory.Create(RunTestsAsync, "run_tests"),
    arguments => ToolCallPresentationFactory.ForTestRun(
        arguments,
        targetPathArgumentName: "targetPath",
        filterArgumentName: "filter"));
```

### 8.3 工具调用阶段

```text
FunctionCallContent
    ↓
根据 Name 查找 ToolRegistration
    ↓
调用 CreatePresentation(Arguments)
    ↓
生成 ToolCallPresentation
    ↓
交给 CopilotChatMessage.AppendFunctionCall
    ↓
保存到 CopilotChatToolItem
```

`CopilotChatMessage` 不应访问全局服务或静态注册表。调用方应先生成展示结果，再传入消息模型。

### 8.4 建议的方法形态

概念上将：

```text
AppendFunctionCall(FunctionCallContent functionCallContent)
```

扩展为：

```text
AppendFunctionCall(
    FunctionCallContent functionCallContent,
    ToolCallPresentation? presentation)
```

保留原重载可降低迁移成本，并为没有注册信息的调用提供兼容入口。

---

## 9. 注册表设计

### 9.1 ToolRegistrationRegistry

工具集合创建后，构造只读查找表：

```text
工具名称 → ToolRegistration
```

注册表职责仅为：

- 根据 `FunctionCallContent.Name` 找到注册项。
- 调用注册项的摘要函数。
- 对摘要结果执行统一的长度限制和空值规范化。

注册表不应包含按具体工具名编写的大型 `switch`。

### 9.2 按工具名查找不属于不良耦合

工具名本身就是工具调用协议的身份标识。

合理耦合：

```text
工具名 → 对应注册项
```

不合理耦合：

```text
中央格式化器 switch 所有工具名并理解每个工具的参数
```

新工具加入后，应只修改该工具所属的注册代码，不修改中央注册表和 Avalonia UI。

### 9.3 重复注册

同一个工具名不能注册多个摘要规则。构造注册表时若发现重复名称，应抛出精确的 `InvalidOperationException`，避免运行时选择不确定。

---

## 10. 生命周期设计

### 10.1 跟随 CodingWorkspaceToolLease

当前应用支持工作区切换，一次 Agent 运行会持有稳定的 `CodingWorkspaceToolLease`。

展示注册也应跟随本次工具 Lease：

```text
CodingWorkspaceToolLease
├── Tools
└── ToolRegistrationRegistry
```

这样可以保证：

1. 本次运行使用哪组工具，就使用哪组摘要规则。
2. 中途切换工作区不会影响正在运行的旧调用。
3. 附加工具源可以按工作区提供不同工具和不同摘要规则。
4. 不需要全局静态可变注册表。
5. 并行测试不会相互污染。

### 10.2 CodingWorkspaceToolSession

`CodingWorkspaceToolSession` 创建各种工具时，应同时聚合：

- Roslyn 工具注册。
- Workspace 文件工具注册。
- .NET CLI 工具注册。
- 图片工具注册。
- Windows Sandbox 工具注册。
- 宿主附加工具注册。

最终分别形成：

- 提供给 Agent 的 `AITool` 集合。
- 提供给消息显示链路的只读注册表。

### 10.3 附加工具源

如果需要让附加工具也支持摘要，建议将工具源的返回值逐步从纯 `AITool` 扩展为 `ToolRegistration`。

迁移期间可以同时支持：

- 旧工具源返回 `AITool`，使用无摘要回退。
- 新工具源返回 `ToolRegistration`，具有完整摘要。

不要为了兼容旧工具自动展示其第一个字符串参数。

---

## 11. 通用摘要辅助函数

虽然每个注册项都可以直接写委托，但不应大量重复参数读取和格式化逻辑。建议提供无状态的通用辅助类。

### 11.1 参数读取

提供安全读取能力：

- `TryGetString`
- `TryGetInt32`
- `TryGetBoolean`
- `TryGetArray`
- 同时兼容常规 CLR 值和 `JsonElement`
- 参数名按工具协议使用确定的比较规则

参数缺失时返回空，不抛出影响聊天流程的异常。

### 11.2 路径摘要

提供：

- 完整路径规范化展示。
- 最后一个或最后两个路径段。
- 文件名提取。
- `.csproj/.sln/.slnx` 目标展示。
- 空目标映射为“整个工作区”的语义结果。

不能要求目标路径实际存在，因为摘要生成发生在工具执行前，非法路径也应该能被展示。

### 11.3 行范围

规则：

| 参数 | 显示 |
|---|---|
| 起始与结束均存在且不同 | 第 70–150 行 |
| 起始与结束相同 | 第 70 行 |
| 只有起始行 | 从第 70 行开始 |
| 参数无效 | 省略范围 |

### 11.4 查询文本

- 去除不必要的首尾空白。
- 单行化。
- 长文本限制长度。
- UI 显示时可加引号。
- 不修改原始 `InputText`。

### 11.5 测试过滤器

优先展示用户可识别的测试目标：

- 完全限定名。
- 类型名。
- 方法名。

对于无法可靠解析的表达式，截断显示原表达式，不猜测含义。

### 11.6 统一长度限制

建议模型层设置防御性限制：

- `PrimaryText`：最多约 160 个字符。
- `SecondaryText`：最多约 100 个字符。
- 查询和过滤器：采用更短限制。

UI 仍需使用视觉省略。模型层限制主要用于：

- 防止摘要快照和历史 XML 异常膨胀。
- 防止复杂工具意外返回大型文本。

截断实现应正确处理 Unicode，不在代理项中间截断。

---

## 12. 具体工具摘要规则

### 12.1 Workspace 文件工具

#### ListDirectory

读取：

- `directoryPath`
- 可选 `recursive`

建议：

```text
列出目录 | 目标目录
```

`recursive` 可作为次要信息“递归”，但不是必须。

#### FindEntriesByName

读取：

- `query`
- `directoryPath`

建议：

```text
查找文件 | “名称关键词” | 搜索目录
```

#### FindFilesMatchingPattern

读取：

- `query`
- `directoryPath`
- 可选 `useRegex`

建议：

```text
搜索内容 | “查询内容” | 搜索目录
```

`useRegex=true` 可增加“正则”次要标记。

#### ReadFileLines

读取：

- `filePath`
- `startLine`
- `endLine`

不读取：

- `includeLineNumbers`

建议：

```text
读取文件 | Views\ChatView.axaml | 第 70–150 行
```

#### WriteFileContent

读取：

- `filePath`

不读取：

- `content`

建议：

```text
写入文件 | Views\ChatView.axaml
```

#### ReplaceStringInFile

读取：

- `filePath`

不读取：

- `oldString`
- `newString`

建议：

```text
替换文件 | ViewModels\MessageItemViewModel.cs
```

#### MultiReplaceStringInFile

该工具参数是复杂集合，使用工具所属项目提供的局部摘要函数。

建议结果：

```text
批量替换 | 3 个文件 | 5 项修改
```

如果全部操作只涉及一个文件，可优先显示文件名：

```text
批量替换 | MessageItemViewModel.cs | 5 项修改
```

不展示 `oldString/newString`。

### 12.2 Roslyn 工具

#### get_projects_in_solution

读取：

- `solutionPath`

空值：

```text
读取解决方案 | 当前工作区解决方案
```

指定路径：

```text
读取解决方案 | ChatRoom.slnx
```

#### get_files_in_project

读取：

- `projectPath`

建议：

```text
读取项目文件 | CodingChatRoom.AvaloniaShell.csproj
```

#### code_search

读取：

- `searchQueries`

建议：

```text
搜索代码 | 2 个查询
```

如果只有一个短查询，可直接显示查询内容。

#### find_symbol

读取：

- `symbolName`

建议：

```text
查找符号 | CopilotChatToolItem
```

#### find_all_references

读取：

- `filePath`
- `line`

建议：

```text
查找引用 | CopilotChatMessage.cs | 第 618 行
```

### 12.3 .NET CLI 工具

#### run_tests

读取：

- `targetPath`
- `filter`

空 `targetPath`：

```text
运行测试 | 整个工作区
```

指定项目：

```text
运行测试 | CodingChatRoom.AvaloniaShell.Tests.csproj
```

带过滤器：

```text
运行测试 | CodingChatRoom.AvaloniaShell.Tests.csproj | ChatViewModelTests
```

#### run_build

读取：

- `targetPath`
- `configuration`
- `targetFramework`
- 可选 `runtimeIdentifier`

建议：

```text
构建项目 | CodingChatRoom.AvaloniaShell.csproj | Debug · net10.0
```

空目标显示“整个工作区”。

#### run_msbuild

规则与 `run_build` 相同，行为名称显示为“MSBuild 构建”。

#### read_last_log_lines

读取：

- `startLine`
- `endLine`

建议：

```text
读取日志 | 第 1–100 行
```

#### search_last_log

读取：

- `pattern`

建议：

```text
搜索日志 | “error|warning”
```

### 12.4 图片和沙盒工具

#### load_image

读取：

- `filePath`

建议：

```text
加载图片 | Assets\diagram.png
```

#### execute_in_windows_sandbox

读取：

- `executableRelativePath`
- `workingDirectoryRelativePath`

不读取：

- 完整参数数组内容，除非后续确认安全且必要。

建议：

```text
沙盒执行 | TestRunner.exe | bin\Debug\net10.0
```

---

## 13. CopilotChatToolItem 修改

建议增加：

```text
PrimaryText
SecondaryText
FullTargetText
```

并提供：

```text
HasPrimaryText
HasSecondaryText
SummaryText
ToolTipText
```

### 13.1 无状态模型

不增加 `CopilotChatToolState`。普通工具项只保存调用标识、工具名称、输入输出和展示摘要。

`AppendFunctionCall` 与 `AppendFunctionResult` 根据 `CallId` 更新同一个工具项，但不额外维护执行状态字段。

### 13.2 PropertyChanged

以下变化应通知 UI：

- `PrimaryText`
- `SecondaryText`
- `SummaryText`
- `ToolTipText`

### 13.3 克隆

`ICopilotChatMessageItem.Clone()` 必须保留：

- 展示行为。
- 主要文本。
- 次要文本。
- 完整目标。
- 输入和输出。

---

## 14. 顶层与子代理一致性

`CopilotChatMessage` 和 `CopilotChatSubAgentItem` 都会处理 `FunctionCallContent`。

二者必须使用相同的 `ToolCallPresentation` 生成结果，不能出现：

- 顶层工具显示友好摘要。
- 子代理工具仍只显示原始 JSON。

建议让当前执行上下文持有本次 Lease 的只读注册表，并在向顶层或子代理追加函数调用前生成展示结果。

同时应统一顶层和子代理的 `InputText` 人类可读格式，避免同一种工具在不同层级展开后显示格式不一致。

---

## 15. 持久化与兼容性

### 15.1 新字段

普通工具 XML 建议增加可选字段：

```text
PrimaryText
SecondaryText
FullTargetText
```

可以使用属性或元素，需保持现有格式风格一致。

### 15.2 保存展示快照

应保存已经生成的摘要，而不是只保存工具名和输入后在加载时重新计算。

原因：

- 历史加载时可能不存在原工具注册表。
- 工具摘要规则以后可能发生变化。
- 旧会话应保持当时看到的含义。
- 外部或临时工具可能已经不再注册。

### 15.3 旧 XML 恢复

旧历史没有新字段时：

- 遗留的 `Action` 属性直接忽略。
- `PrimaryText` 和 `SecondaryText` 为空。
- `DisplayName` 回退为原始 `ToolName`。
- 旧 XML 中已有的 `State` 属性直接忽略。

不要求从旧 `InputText` 中解析摘要，以避免引入脆弱兼容逻辑。

### 15.4 未知值

- 遗留或未知的 `Action`、`State` 属性忽略。
- 缺失字段不能导致整个会话加载失败。

---

## 16. Avalonia 界面设计

### 16.1 折叠标题结构

普通工具 `Expander.Header` 攧为分栏布局：

```text
[工具名称] [主要文本] [次要文本]
```

优先级：

1. 工具名称。
2. 主要文本。
3. 次要文本。

### 16.2 建议布局

- 工具名称：保持可见。
- 主要文本：占用剩余空间，启用单行省略。
- 次要文本：次要颜色，空间不足时优先缩减。
- Tooltip：显示 `FullTargetText` 或完整组合摘要。

### 16.3 展开内容

保留现有：

- 输入参数。
- 工具输出。

新增展示字符串应放入 `Styles/Strings.axaml`，避免继续增加硬编码用户界面文本。

### 16.4 默认展开策略

普通工具默认折叠。展开状态只属于 UI 交互，不与工具执行过程绑定。

---

## 17. 本地化设计

工具名称直接使用工具调用协议中的 `ToolName`，不再维护额外的展示语义、友好名称映射或类别资源。

用户可见字符串放入：

```text
Styles/Strings.axaml
```

包括：

- “整个工作区”。
- 行范围格式。
- “输入参数”。
- “工具输出”。

工具注册层不应依赖 Avalonia 资源，也不应返回完整中文句子。对于行范围等次要文本，第一阶段可由通用摘要辅助函数生成中文；长期可以把结构化范围交给 UI 本地化。

---

## 18. 安全与失败策略

### 18.1 显式读取

摘要函数只读取允许展示的参数。未读取参数默认不展示。

典型禁止内容：

- `oldString`
- `newString`
- 文件完整内容
- Prompt
- Token 或密钥
- Base64 数据
- 完整命令输出
- 大型 JSON

### 18.2 摘要异常不能中断工具调用

摘要属于辅助展示能力。

若摘要函数发生非预期异常：

- 不得阻止工具执行。
- 回退为工具原名。
- 记录适当诊断信息，但避免日志刷屏。

注册和测试阶段应尽量发现错误，运行时仍需防御。

### 18.3 无描述工具

只显示工具名，不使用启发式参数猜测。

### 18.4 结果状态

第一阶段不从任意字符串结果推断成功或失败。

准确失败状态应等待工具层提供统一结构化执行结果，或在调用管线中明确传递异常/取消状态。

---

## 19. 测试方案

项目继续使用现有 MSTest，不增加新测试框架。

### 19.1 ToolRegistration 测试

覆盖：

1. 注册项保存正确的 `AITool` 和摘要函数。
2. 重复工具名构造注册表时失败。
3. 无摘要函数的工具可以正常注册和调用。
4. 摘要函数异常时安全回退，不影响工具调用。
5. 注册表是只读的，不存在全局可变状态。

### 19.2 参数读取测试

覆盖：

- CLR 字符串、整数和布尔值。
- `JsonElement` 字符串和数字。
- 缺失参数。
- `null` 参数。
- 类型不匹配。
- 超长文本。
- Unicode 截断。

### 19.3 工具摘要测试

#### ReadFileLines

断言：

- 主要文本包含目标文件。
- 次要文本包含行范围。
- 不显示 `includeLineNumbers`。

#### ReplaceStringInFile

断言：

- 只显示目标文件。
- 不显示 `oldString`。
- 不显示 `newString`。

#### run_tests

分别覆盖：

- 指定测试项目。
- 空目标显示整个工作区。
- 带过滤器。
- 超长过滤器截断。

#### MultiReplaceStringInFile

断言：

- 显示操作数量。
- 显示文件数量或单个文件名。
- 不显示替换文本。

#### 未注册工具

断言：

- 只显示工具名。
- 不自动展示任何参数。

### 19.4 工具项更新测试

覆盖：

1. 相同 `CallId` 更新同一个工具项。
2. 空结果不会创建重复工具项。
3. 克隆保留摘要、输入和输出。
4. 普通工具项不包含执行状态属性。

### 19.5 持久化测试

覆盖：

1. 新字段保存后可恢复。
2. 旧 XML 无新字段时可加载。
3. 旧 XML 中的遗留 `State` 属性被忽略。
4. 遗留 Action 属性被安全忽略。
5. 历史恢复不依赖当前工具注册表。

### 19.6 Shell 测试

覆盖：

- 工具模板仍由 `ChatMessageItemTemplateSelector` 正确选择。
- 标题绑定主要文本和次要文本。
- 长文本启用视觉截断。
- Tooltip 显示完整目标。
- 输入参数和工具输出仍然存在。
- 子代理中的普通工具使用同一模板。

### 19.7 回归测试

至少运行：

- `AgentLib.Tests`
- `AgentLib.Coding.Tests`
- `CodingChatRoom.AvaloniaShell.Tests`
- 目标解决方案构建

---

## 20. 分阶段实施计划

### 第一阶段：注册与摘要基础

1. 在 `AgentLib` 增加展示结果和注册表基础类型。
2. 在工具创建点引入 `ToolRegistration`。
3. 为文件工具、Roslyn 工具、CLI 工具、图片工具和沙盒工具配置摘要函数。
4. 让 `CodingWorkspaceToolSession/Lease` 同时携带工具和只读注册表。
5. 在函数调用到达时生成 `ToolCallPresentation`。
6. 顶层消息和子代理统一接收展示结果。

### 第二阶段：模型与持久化

1. 扩展 `CopilotChatToolItem`。
2. 更新克隆和完整消息文本格式。
3. 扩展 XML 保存和恢复。
4. 增加旧历史兼容测试。

### 第三阶段：Avalonia 展示

1. 改造普通工具标题布局。
2. 增加主要目标、次要信息和 Tooltip。
3. 补充资源字符串和样式。
4. 验证长路径、窄窗口和子代理嵌套显示。

### 第四阶段：后续增强

可选功能：

- 执行耗时。
- 复制输入和输出。
- 点击文件路径打开或定位。
- 测试结果数量摘要。
- 构建错误和警告数量摘要。
- 连续工具调用分组。

这些功能不作为核心问题修复的前置条件。

---

## 21. 预计修改范围

### AgentLib

- `Model/CopilotChatToolItem.cs`
- `Model/CopilotChatMessage.cs`
- `Model/CopilotChatSubAgentItem.cs`
- `Model/ICopilotChatCurrentContent.cs`
- `Logging/CopilotChatHistoryXmlCodec.cs`
- 新增工具展示相关模型、注册表和通用辅助类
- `CopilotChatManager.cs` 或更靠近执行上下文的函数调用转换位置

### AgentLib.Coding

- `CodingWorkspaceToolLease.cs`
- `CodingWorkspaceToolSession.cs`
- `CodingWorkspaceToolProvider.cs`
- `DotNetCliTools.cs`
- `RoslynAgentTools.cs`
- `CodingWorkspaceContentTools.cs`
- `Sandboxes/WindowsSandboxTools.cs`
- Workspace 工具注册位置
- 附加工具源契约或兼容适配层

### CodingChatRoom.AvaloniaShell

- `Views/ChatView.axaml`
- `Styles/MessageBubble.axaml`
- `Styles/Strings.axaml`

### 测试项目

- `AgentLib.Tests`
- `AgentLib.Coding.Tests`
- `CodingChatRoom.AvaloniaShell.Tests`

实施时应根据真实调用依赖做最小改动，不应为了摘要功能重构无关的 Agent 执行逻辑。

---

## 22. 验收标准

### ReadFileLines

原显示：

```text
ReadFileLines
```

目标显示：

```text
ReadFileLines  Views\ChatView.axaml  第 70–150 行
```

### ReplaceStringInFile

原显示：

```text
ReplaceStringInFile
```

目标显示：

```text
ReplaceStringInFile  ViewModels\MessageItemViewModel.cs
```

标题中不得出现 `oldString/newString` 内容。

### run_tests

指定项目：

```text
运行测试  CodingChatRoom.AvaloniaShell.Tests.csproj
```

未指定目标：

```text
运行测试  整个工作区
```

### 普通工具状态

普通工具标题不显示“正在执行”或“已完成”，消息模型和历史 XML 也不保存对应状态。

### 历史会话

- 新会话保存后重新打开仍显示摘要。
- 旧会话可以正常打开。
- 旧历史中的遗留 `State` 属性不影响加载和展示。

### 架构

- 不增加摘要 Attribute。
- 不增加摘要反射扫描。
- Avalonia UI 不按工具名解析参数。
- 中央摘要组件不包含所有具体工具名的 `switch`。
- 新增工具摘要时，只需在工具注册位置提供摘要函数。
- 未注册工具安全回退为只显示工具名。

---

## 23. 最终决策

本功能采用：

> **显式 `ToolRegistration` + 工具局部摘要函数 + 只读注册表 + 结构化 `ToolCallPresentation` + 工具 Lease 生命周期 + 展示快照持久化。**

明确不采用：

- 参数 Attribute。
- 运行时反射扫描。
- UI 按工具名编写摘要规则。
- 从 `InputText` 反向解析参数。
- 中央类维护所有具体工具的 `switch`。
- 未注册工具自动挑选参数展示。
- 为普通工具维护 `CopilotChatToolState` 或等价执行状态字段。

该方案使工具执行、参数摘要、消息模型和 Avalonia 展示保持清晰边界，同时能够覆盖内置文件工具、Roslyn 工具、.NET CLI 工具、沙盒工具、子代理工具和未来附加工具。