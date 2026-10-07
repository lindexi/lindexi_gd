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

## AgentApiEndpointManager 与客户端入口：本轮确认决策

### 设计原则

- 面向长期维护，采用最佳实践，不以补丁或重复实现解决架构问题。
- 禁止过度设计、过度防御；不为尚未出现的需求增加抽象和检查。
- AgentLib 提供 Chat Completions 与 Responses 两套调用入口，但共用相同的用户入口。

### 保留统一管理器与现有配置

继续使用 `AgentApiEndpointManager`，不另起一套 Responses 管理器。模型注册、查询、首选模型选择及共享网络配置继续统一管理；管理器不承担 Responses 会话状态、流式事件和工具循环。

配置部分已经确定，不属于本次改造范围：

- 不修改设置模型、设置界面、设置文件格式及读写逻辑。
- 不新增 API 类型、Responses 开关或 capability 配置字段。
- 两套客户端复用现有地址、凭据和模型信息，不要求为同一个模型额外注册两套配置。
- 调用路线由上层执行入口选择，不由模型配置决定。

### 通过独立接口提供 Responses 客户端

保持 `ILanguageModel` 及其 `GetChatClientAsync()` 不变，新增一个只负责获取 Responses 客户端的接口 `IResponsesClientProvider`。`OpenAILanguageModel` 同时实现这两个接口，同一个模型对象提供两套客户端入口。

此决定替代上一轮“直接在 ILanguageModel 上增加 Responses 客户端获取方法”的结论。独立接口用于保持现有公开接口契约和已有实现方的兼容性，不另建模型体系、管理器或工厂体系。

### 接口判断与错误处理边界

- Responses 执行入口判断当前 `ILanguageModel` 对象是否同时实现 `IResponsesClientProvider`；实现时通过该接口获取客户端，未实现时明确报错。
- 接口判断集中在 Responses 执行入口，不散落到管理器、UI 或工具逻辑中。
- 判断的是本地对象是否提供客户端获取能力，不是判断远端服务是否支持 Responses。
- 实现接口的对象直接提供客户端，不增加服务端支持性预检查、能力标记、网络探测，也不用可空客户端表示不支持。
- 按所选路线正常发起请求；真实端点不支持时，按服务返回的错误处理，不自动回退到 Chat Completions，也不将网络或鉴权错误转换成“不支持”。

### JSON 配置链路与实现责任

现有创建路线保持不变：JSON 配置 → `AgentApiEndpointManager.LoadConfiguration()` → `JsonConfigurationOpenAIProtocolLanguageModelProvider` → `OpenAIProtocolLanguageModelProviderBase.GetSupportedModels()` → `OpenAILanguageModel`。

由 AgentLib 的 `OpenAILanguageModel` 直接同时实现 `ILanguageModel` 和 `IResponsesClientProvider`，确保 Shell 经这条配置链路获得的所有模型天然具备两套客户端获取能力。配置替换后同样成立，不按服务商名称或远端支持情况筛选。

禁止将一个模块的实现缺陷交给另一个尚未实现的模块兜底。Shell 不包装模型、不补建客户端，也不承担补齐 AgentLib 接口实现的责任。执行入口的接口判断只是获取接口的手段，不是允许上述配置链路缺失实现的理由。此保证由 AgentLib 配置链路测试验证。

### 本轮实现范围

- 新增 `IResponsesClientProvider.GetResponsesClientAsync()`，返回 `Task<OpenAI.Responses.ResponsesClient>`。
- `OpenAILanguageModel` 实现新接口；原 `ILanguageModel` 不变。
- 复用既有 OpenAI 客户端创建逻辑，共享 Endpoint、凭据和 HttpClient（包含代理配置），不建立另一套连接配置。
- SDK 的 `ResponsesClient` 不绑定模型，请求时需显式传入模型 ID；本轮仅实现客户端获取，不扩展执行层。
- 不修改管理器、JSON 配置结构、Provider 创建路线及 Shell。
- 增加 JSON 加载、配置替换、端点、共享 HTTP 传输与凭据测试，并验证原 Chat 客户端仍可获取。
- SDK Responses API 当前标记为实验性，仅在直接使用它的文件中明确接受 `OPENAI001` 诊断，不修改项目级告警设置。

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
