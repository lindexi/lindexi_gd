# AgentLib 与 AgentLib.Coding 变更边界

## 兼容门禁

### AgentLib

`AgentLib` 必须完全保持公开 API 兼容：

- 不删除公开类型或成员；
- 不修改现有公开签名、默认值和参数语义；
- 不降低公开成员可见性；
- 新能力只能通过新增类型、成员或重载提供；
- 旧 JSON 与旧会话 XML 必须继续加载。

需要建立公开 API 基线测试。项目能够编译不能替代兼容性证明。

### AgentLib.Coding

`AgentLib.Coding` 可以修改，也允许进行必要的破坏性变更，以形成长期可维护的共享工作区内核和两个独立执行引擎。

但必须保持产品行为边界：

- `CodingAgent` 类型及其现有执行路径保留；
- CodingAgent 的提示词、工具能力、授权语义和运行结果不能因 Responses 改造而缺失；
- Shell 中 Toggle 关闭时仍进入原 CodingAgent 分支；
- 对 `AgentLib.Coding` 的破坏性改动必须同步修改仓库内调用方和回归测试，不保留仅为内部兼容而存在的重复架构。

## Responses 客户端来源

不得修改 `AgentLib` 的设置模型，也不得给 `ILanguageModel` 增加 Responses 成员。

Responses 分支从现有 `ILanguageModel` 已提供的信息创建客户端：

- 模型名来自 `ModelDefinition`；
- Endpoint 和凭据来自现有 OpenAI 协议模型实例所持有的 `ApiEndpoint`；
- 客户端创建能力通过 `AgentLib` 新增的只读能力接口或扩展服务暴露；
- 不增加任何设置字段，不要求用户声明 capability，也不依据新增配置开关决定是否可用。

对无法提供 OpenAI Responses 客户端信息的 `ILanguageModel` 实现，在入口返回明确的不支持错误。对于能够创建客户端但远端不实现 `/responses` 的情况，保留服务端返回的状态码和错误信息，不自动回退。

## 共享工作区运行时

在 `AgentLib.Coding` 中形成：

```text
CodingWorkspaceRuntime : IAsyncDisposable
- AcquireRunAsync(workspacePath, enableDotNetRun, additionalSources, ct)
- StopLanguageServerAsync()

CodingWorkspaceRunLease : IAsyncDisposable
- WorkspacePath
- ToolRegistrations
- ToolRegistrationRegistry
```

工具构造从 `CodingAgent` 私有缓存中提取到 runtime。`CodingAgent` 与 `ResponsesCodingAgent` 都使用 run lease，确保：

- Roslyn、文件、build/test/publish、图片和沙箱工具只有一套实现；
- `dotnet run` 仍只影响本次工具集合；
- 工具名称、Schema、权限和展示摘要一致；
- 工作区资源具有单一所有者和明确释放顺序。

由于 `AgentLib.Coding` 允许破坏性修改，可直接调整 `CodingAgent` 构造和内部所有权，不要求额外保留旧构造路径；仓库内调用方统一迁移到新的组合方式。

## Responses Agent

新增：

```text
ResponsesCodingAgent
ResponsesCodingAgentOptions
ResponsesCodingAgentRunResult
ResponsesConversationState
ResponsesFunctionToolAdapter
ResponsesStreamProjector
```

`ResponsesCodingAgent` 直接依赖 OpenAI SDK Responses 客户端，负责请求 Items、流式事件、本地函数工具循环、Web Search 投影、协议状态、取消和用量。

## 提示词单一来源

提取 `CodingPromptProvider`：

```text
BuildInstructionsAsync(copilotInstructionsPath, ct)
```

现有 CodingAgent 和 Responses Agent 使用同一来源，不复制长提示词。

## AIFunction 桥接

`ResponsesFunctionToolAdapter`：

- 从 `AIFunction` 生成 Responses function tool；
- 按 call id 解析参数并调用原工具对象；
- 生成 function call output Item；
- 保持既有工具展示摘要和异常语义；
- 取消直接终止运行，不转换为工具失败文本。

这样 Responses 复用真实编程工具，而不是重新实现文件和 Roslyn 操作。
