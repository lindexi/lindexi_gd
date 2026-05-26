# AgentLib 单元测试覆盖与 MSTest 注意事项

## 适用场景

- 重新搭建 `AgentLib.Tests`。
- 需要为 `AgentLib` 的纯逻辑与本地文件行为补齐回归测试。
- 运行 `dotnet test -c release` 时需要快速定位常见测试实现问题。

## 本次覆盖策略

1. 优先覆盖不依赖外部 AI 服务的稳定逻辑：
   - `LanguageModelCapabilityComparer`
   - `DictionaryModelNameToIdMap`
   - `NotifyBase`
   - `CopilotChatMessage`
   - `CopilotChatSubAgentItem`
   - `CopilotChatSession`
2. 文件系统行为单独测试：
   - `FileCopilotChatLogger` 使用临时目录隔离日志与 XML 历史文件。
3. 每个测试方法都添加中文 `Description`，便于在测试资源管理器中直接理解测试意图。

## 常见注意点

### 1. `Description` 特性命名冲突

如果测试文件同时引入了 `System.ComponentModel`，则 `Description` 会和 MSTest 的 `DescriptionAttribute` 冲突。

处理方式：

- 保留 `System.ComponentModel` 时，对测试描述特性写全名：
  `Microsoft.VisualStudio.TestTools.UnitTesting.Description(...)`
- 或移除不必要的 `System.ComponentModel` using。

### 2. `FunctionCallContent` 参数类型

当前使用的 `Microsoft.Extensions.AI` 版本中，`FunctionCallContent` 的 `arguments` 参数需要传入 `IDictionary<string, object?>?`，不要直接传匿名对象。

推荐写法：

- `new Dictionary<string, object?> { ["Path"] = "a.txt" }`

### 3. `NotifyBase.SetField` 的 `CallerMemberName`

如果通过测试辅助方法包装 `SetField`，默认捕获到的方法名会是包装方法，而不是目标属性名。

测试替身里应显式传入属性名，例如：

- `SetField(ref _value, value, nameof(Value))`

### 4. 断言风格

MSTest 4 自带分析器会提示优先使用更具体的断言：

- 数量校验优先 `Assert.HasCount`
- 大小比较优先 `Assert.IsGreaterThan`
- 空字符串优先 `Assert.IsEmpty`

补测试时尽量直接使用这些 API，避免额外告警。

## 验证命令

在 `AgentLib.Tests` 上执行：

- `dotnet test -c release`

## 本次结果

- 新增 28 个测试。
- 发布模式测试全部通过。

## 取消链路回归测试补充

### 1. `CopilotChatManager` 取消测试建议先搭可复用 Fake 基建

当测试 `SendMessageAsync` 的普通聊天、工具调用和子智能体调用取消链路时，不要在每个测试里重复拼装模型和聊天客户端。

推荐做法：

- 用 `FakeChatClient` 统一收集请求并按队列返回流式响应。
- 用 `FakeLanguageModel` / `FakeLanguageModelProvider` 把 Fake 客户端注入 `AgentApiEndpointManager`。
- 用 `CopilotChatManagerTestContext` 统一创建主模型、Flash 模型和常用 `ChatResponseUpdate`。

这样后续继续补 AI 交互测试时，只需要关注场景本身，不必重复搭测试依赖。

### 2. 工具取消测试要阻塞“工具执行本身”

如果 Fake 聊天客户端只是先返回一次 `FunctionCallContent`，再等待第二轮流式调用，测试可能会卡在框架内部的工具执行流程，无法稳定证明取消是否生效。

更稳妥的方式：

- 提供一个 `BlockingTool`，在 `AIFunction.InvokeAsync` 对应的方法内部等待取消。
- 让测试在工具真正开始执行后再 `Cancel()`。

这样可以直接验证工具调用链是否把 `CancellationToken` 透传到了工具执行层。

### 3. 子智能体取消要确认没有使用 `CancellationToken.None`

子智能体流式调用如果写死 `CancellationToken.None`，外层 `SendMessageAsync` 即使已经取消，子智能体仍会继续跑完。

回归测试应覆盖：

- 主代理通过 `InvokeSubAgent` 发起委托。
- 被选中的子智能体模型返回一个可阻塞的流式响应。
- 外层取消后，`SendMessageAsync` 最终追加“已取消”预设消息。

### 4. `ArtifactsPath` 指向项目内目录时要排除生成源码

`AgentLib.Tests` 通过 `Directory.Build.props` 把 `ArtifactsPath` 指到项目目录下的 `artifacts/`。此时 SDK 可能把 `artifacts/obj/**` 里的 `*.AssemblyInfo.cs`、`*.GlobalUsings.g.cs` 当成源码再次编译，触发重复程序集特性错误。

处理方式：

- 在测试项目文件中显式排除：
  - `artifacts\**\*.cs`
  - `artifacts\**\*`

否则命令行 `dotnet test` 可能比 IDE 内部构建更早暴露该问题。
