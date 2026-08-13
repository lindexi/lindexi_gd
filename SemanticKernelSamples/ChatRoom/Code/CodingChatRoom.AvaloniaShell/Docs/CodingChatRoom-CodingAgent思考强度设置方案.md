# CodingChatRoom CodingAgent 思考强度设置方案

## 1. 文档目的

本文设计 CodingChatRoom 为 CodingAgent 增加思考强度选择能力的实现方案，重点说明思考强度如何从聊天界面稳定传递到最终模型请求。

本文仅输出方案，不修改 C#、AXAML、项目配置或测试代码。

## 2. 当前结构

### 2.1 模型切换

当前聊天界面的模型选择由以下文件负责：

- `Views/ChatView.axaml`
- `ViewModels/ChatViewModel.cs`
- `ViewModels/LanguageModelOptionViewModel.cs`

`ChatViewModel.SelectedModel` 修改 `CopilotChatManager.AgentApiEndpointManager.PrimaryModel`，因此当前模型切换属于进程内共享状态。

### 2.2 消息发送链路

当前发送链路为：

```text
ChatViewModel
  → CodingChatApplication.SendMessageAsync
  → ICodingChatRunner.RunAsync
  → CodingAgentChatRunner
  → CopilotChatManager.CreateManualSendMessageContextAsync
  → CodingAgent.RunAsync
  → IChatClient
```

现有 `ICodingChatRunner.RunAsync` 只接收消息内容、工作路径、自动压缩状态和取消令牌，没有通用的单次运行参数对象。

`Microsoft.Extensions.AI.ChatOptions` 已提供 `AdditionalProperties` 和 `RawRepresentationFactory`，可以承载供应商扩展请求参数，但仍需要 AgentLib 建立供应商无关的参数模型和协议映射。

### 2.3 已有推理能力标记

AgentLib 已存在 `LlmModelCapabilities.Reasoning`。该标记只能说明模型具有推理能力，不能说明模型允许用户调整推理强度，也不能表达支持哪些强度。

因此需要把以下两个概念分开：

- 模型能够产生推理内容；
- 模型支持配置推理强度。

## 3. 设计原则

1. 思考强度属于单次 CodingAgent 运行参数，不属于共享模型实例状态。
2. 用户点击发送时生成不可变快照；运行开始后，界面修改只能影响下一次运行。
3. 同一次 CodingAgent 运行的首次模型调用和工具调用后的后续模型调用必须使用相同强度。
4. Shell 和 CodingAgent 使用供应商无关的枚举，供应商适配器负责转换协议字段。
5. 不根据模型名称、提供商显示名称或字符串前缀判断支持能力。
6. 默认值表示省略供应商协议字段，不表示发送字符串 `default`。
7. 不支持的配置必须产生明确错误，不能静默降级。
8. 第一阶段保持改动最小，不强制修改会话持久化格式。

## 4. 思考强度模型

建议在 AgentLib 公共模型层定义供应商无关的枚举：

```text
Default
Minimal
Low
Medium
High
ExtraHigh
```

建议语义如下：

| 值 | 语义 |
| --- | --- |
| `Default` | 不发送强度字段，使用模型或服务端默认行为 |
| `Minimal` | 最低推理预算，仅供明确支持的模型使用 |
| `Low` | 较低思考强度 |
| `Medium` | 中等思考强度 |
| `High` | 较高思考强度 |
| `ExtraHigh` | 最高思考强度，仅供明确支持的模型使用 |

第一阶段界面至少展示：

- 默认；
- 低；
- 中；
- 高。

`Minimal` 和 `ExtraHigh` 可以保留在公共模型中，但只有模型能力配置明确声明支持时才展示。

## 5. 模型能力声明

建议扩展 `ModelDefinition`，增加可选的支持列表，例如：

```text
SupportedReasoningEfforts
```

规则如下：

- `null` 或空集合：模型没有声明可配置思考强度；
- 非空集合：界面展示“默认”以及模型声明支持的值；
- `Default` 不必写入支持列表，它始终表示不发送强度参数；
- `LlmModelCapabilities.Reasoning` 继续表达模型是否具有推理能力；
- `SupportedReasoningEfforts` 单独表达是否允许配置强度。

模型配置概念示例：

```json
{
  "modelId": "gpt-5",
  "modelName": "GPT-5",
  "capabilities": {
    "reasoning": true
  },
  "supportedReasoningEfforts": [
    "Low",
    "Medium",
    "High"
  ]
}
```

新增配置字段必须向后兼容。旧配置没有该字段时继续正常加载，并视为没有声明可配置思考强度。

## 6. 单次运行参数

### 6.1 Shell 运行参数

建议在 CodingChatRoom 应用层定义不可变的运行参数快照，例如：

```text
CodingChatRunOptions
```

至少包含：

```text
ReasoningEffort
EnableAutomaticCompression
```

工作路径目前由 `CodingWorkspaceController` 提供，可以暂时继续单独传递；如果后续希望统一契约，也可以纳入运行参数对象。

### 6.2 AgentLib.Coding 运行参数

建议在 AgentLib.Coding 定义：

```text
CodingAgentRunOptions
```

至少包含：

```text
ReasoningEffort
EnableAutomaticCompression
```

如果 Shell 和 AgentLib 可以直接共享同一枚举，优先复用 AgentLib 中的 `ReasoningEffort`，不要在 Shell 中复制一套枚举并增加无意义的映射。

### 6.3 快照时机

参数必须在用户点击发送时捕获：

```text
用户点击发送
  → 读取当前模型
  → 读取当前思考强度
  → 生成不可变运行快照
  → 启动 CodingAgent
```

运行过程中不得再次从 `ChatViewModel` 读取强度。否则用户在工具调用期间修改选择，会导致同一次任务的不同模型调用使用不同强度。

## 7. 参数传递链路

完整链路建议如下：

```text
ChatViewModel.SelectedReasoningEffort
  → ChatViewModel.SendAsync 创建 CodingChatRunOptions
  → CodingChatApplication.SendMessageAsync(contents, options)
  → ICodingChatRunner.RunAsync(contents, workspacePath, options)
  → CodingAgentChatRunner 转换为 CodingAgentRunOptions
  → CodingAgent.RunAsync(..., options)
  → CodingAgent 每轮模型调用创建或克隆 ChatOptions
  → SendMessageRequest 持有 ChatOptions
  → IChatClient 使用 ChatOptions 发起请求
  → 供应商适配器映射为实际协议字段
```

### 7.1 ChatViewModel

职责：

- 展示模型支持的思考强度；
- 保存用户当前选择；
- 发送时创建不可变快照；
- 不负责解释 OpenAI 或其他供应商协议字段。

### 7.2 CodingChatApplication

职责：

- 验证运行参数不为空；
- 将参数传给 `ICodingChatRunner`；
- 在运行期间的人类插话场景中沿用当前活动运行参数；
- 不负责将强度映射为具体供应商字符串。

### 7.3 ICodingChatRunner

建议把现有签名从多个独立参数调整为包含运行参数对象的契约。

Runner 的职责是把 Shell 运行参数转换成 AgentLib.Coding 的运行参数，并将其传给 `CodingAgent.RunAsync`。

### 7.4 CodingAgent

`CodingAgent.RunAsync` 在整个运行生命周期内保存 `CodingAgentRunOptions`。一次运行可能包含多轮模型调用：

```text
第一次模型请求
  → 工具调用
  → 工具结果
  → 第二次模型请求
  → 工具调用
  → 工具结果
  → 第三次模型请求
  → 最终回复
```

以上每次模型请求都必须使用本次运行的同一思考强度。

建议建立基础 `ChatOptions`，每次请求前克隆，避免工具、响应格式或其他请求逻辑修改共享对象。

### 7.5 SendMessageRequest

工作区引用的 AgentLib 已存在 `SendMessageRequest`。它是承载请求级 `ChatOptions` 的合适位置。

建议增加或复用请求选项属性，使最终调用形成：

```text
CodingAgentRunOptions.ReasoningEffort
  → SendMessageRequest.ChatOptions
  → IChatClient 流式请求
```

不能只给第一次请求传递选项，工具调用后的后续请求也必须继续传递。

## 8. ChatOptions 中的表达

建议在通用层使用规范化键，不直接使用供应商字段名。例如：

```text
agentlib.reasoning-effort
```

通用层可以把枚举放入 `ChatOptions.AdditionalProperties`，由供应商适配器读取并转换。

规则：

- `Default`：不写入扩展属性；
- 其他值：写入供应商无关的枚举值或稳定字符串；
- 通用 CodingAgent 不写入 `reasoning_effort`；
- 通用 CodingAgent 不引用具体 OpenAI SDK 类型。

如果 AgentLib 已有统一的请求选项模型，应优先扩展现有模型，不额外创建重复抽象。

## 9. 供应商协议映射

### 9.1 OpenAI 协议

OpenAI 协议适配器可执行如下映射：

| AgentLib 强度 | OpenAI 协议值 |
| --- | --- |
| `Default` | 省略 `reasoning_effort` |
| `Minimal` | `minimal` |
| `Low` | `low` |
| `Medium` | `medium` |
| `High` | `high` |
| `ExtraHigh` | `xhigh` |

最终请求概念示例：

```json
{
  "reasoning_effort": "high"
}
```

`Default` 不得生成：

```json
{
  "reasoning_effort": "default"
}
```

### 9.2 AdditionalProperties

如果具体 `IChatClient` 支持把 `ChatOptions.AdditionalProperties` 映射到请求体，可以由供应商适配器写入：

```text
reasoning_effort = high
```

但不能仅凭接口存在就假定一定会序列化。必须通过供应商适配器测试或 HTTP 请求捕获测试确认字段确实进入最终请求。

### 9.3 RawRepresentationFactory

如果底层 SDK 提供原生请求选项类型，可以在供应商适配项目中通过 `ChatOptions.RawRepresentationFactory` 创建或修改底层对象。

该逻辑只能位于具体供应商适配层，不能放在：

- CodingChatRoom UI；
- `ChatViewModel`；
- 通用 `CodingAgent`；
- 通用模型能力定义。

### 9.4 其他供应商

不同供应商可能使用：

- 推理强度枚举；
- 推理预算 token；
- 是否启用推理的布尔值；
- 不同的字段名称。

因此供应商适配器应负责：

- 判断是否支持本次值；
- 映射为供应商协议；
- 对无法表达的值给出明确错误；
- 保证 `Default` 不发送额外字段。

不得通过模型名称硬编码判断，例如根据 `gpt-`、`o1` 或 `deepseek` 前缀选择协议。

## 10. 参数优先级

如果后续增加模型默认强度，建议优先级为：

```text
单次运行显式选择
  > 模型配置默认强度
  > 供应商默认行为
```

具体规则：

1. 用户显式选择 `High`：使用 `High`；
2. 用户选择 `Default`，模型配置存在默认强度：使用模型配置值；
3. 用户选择 `Default`，模型也没有默认值：省略协议字段；
4. 用户显式选择模型不支持的值：请求开始前报错；
5. 不进行静默降级。

第一阶段可以不增加模型默认强度，只实现“用户显式值”和“供应商默认行为”。

## 11. UI 设计

### 11.1 控件位置

在聊天页当前模型选择器旁边增加思考强度选择器：

```text
[模型：provider/model] [思考：高]
```

建议：

- 模型选择器保持现有宽度；
- 思考强度选择器宽度约 110 至 130；
- 当前模型没有声明支持列表时隐藏强度选择器；
- 运行期间禁用模型和强度选择器；
- 用户可见字符串放入 `Styles/Strings.axaml`。

### 11.2 切换模型

模型发生变化后：

1. 读取新模型的 `SupportedReasoningEfforts`；
2. 重建可用强度选项；
3. 始终包含“默认”；
4. 如果原选择仍受支持则保留；
5. 如果不受支持则回退到“默认”；
6. 更新状态文本。

### 11.3 状态显示

可以显示：

```text
当前模型：provider/model；思考强度：高
```

如果选择“默认”，可显示：

```text
当前模型：provider/model；思考强度：默认
```

## 12. 特殊运行场景

### 12.1 人类插话

运行期间再次发送消息会进入当前活动运行的插话流程。插话只增加用户内容，不创建新的运行参数。

示例：

```text
活动运行强度：High
用户在运行中把界面改成 Low
用户提交插话：仍由 High 的活动运行处理
下一次新运行：使用 Low
```

运行期间禁用选择器可以避免用户对生效范围产生误解，但底层仍必须依靠不可变快照保证正确性。

### 12.2 循环迭代

循环开始时捕获一次运行参数，整个循环任务复用同一快照。不要让每轮迭代重新读取界面选择，否则一次循环任务的成本和行为不可预测。

如果后续需要每轮动态调整，应作为独立功能设计。

### 12.3 自动压缩与手动压缩

思考强度默认只应用于 CodingAgent 主运行。

自动压缩和手动压缩属于辅助模型请求，不应默认继承主运行的高强度，否则可能无意增加延迟和调用成本。若后续需要，应增加独立的压缩请求设置。

### 12.4 停止与取消

停止当前运行时，只取消活动运行。已选择但尚未启动的新强度不需要额外处理。

如果运行被取消，下一次发送重新捕获界面当前值。

## 13. 持久化策略

### 13.1 第一阶段

建议第一阶段：

- 默认值为 `Default`；
- 选择只在当前应用进程内有效；
- 不修改会话存储格式；
- 不写回模型主配置；
- 应用重启后恢复为 `Default`。

该策略与当前聊天页模型切换的进程内行为接近，改动和迁移风险较低。

### 13.2 后续保存用户默认值

如果需要记住用户偏好，可在 `CodingChatShellSettings` 中增加默认思考强度，并通过：

- `CodingChatSettingsService`；
- `SettingsViewModel`；
- `SettingsView.axaml`；

完成保存和恢复。

不建议把思考强度写入聊天消息历史。除非后续明确要求每个会话保存独立强度，否则也不扩展会话存储结构。

## 14. 错误处理

以下情况应在请求发送前失败：

- 模型没有声明支持思考强度，但收到非默认值；
- 模型支持强度配置，但不支持当前值；
- 供应商适配器无法把当前值映射到协议；
- 底层 SDK 不支持所需请求选项。

错误信息至少说明：

- 当前模型；
- 用户选择的强度；
- 模型或供应商支持的值；
- 可以选择“默认”或其他有效值。

不要捕获后静默移除参数重新请求，也不要自动降级为较低强度。

## 15. 测试方案

### 15.1 AgentLib 模型配置测试

- 支持列表能够正确序列化和反序列化；
- 老配置没有支持列表时正常加载；
- 未知枚举值产生明确配置错误；
- `Reasoning` 能力与可配置强度列表互不替代。

### 15.2 AgentLib 供应商适配测试

- `Default` 不生成协议字段；
- `Low`、`Medium`、`High` 映射正确；
- `Minimal` 和 `ExtraHigh` 只在供应商支持时映射；
- 不支持的值产生明确异常；
- 最终 HTTP 请求或底层原生请求对象包含预期字段。

### 15.3 AgentLib.Coding 测试

- 运行参数能够进入首次模型请求；
- 工具调用后的后续模型请求继续携带相同强度；
- 每轮请求使用克隆后的 `ChatOptions`；
- 自动压缩请求默认不继承强度；
- 取消和异常不改变下一次运行的参数。

### 15.4 CodingChatApplication 测试

- 默认发送传递 `Default`；
- 指定 `High` 能传到 Runner；
- 多模态内容发送保留运行参数；
- 插话不启动新强度配置；
- 循环迭代保持初始快照；
- 强度参数不影响现有自动压缩开关。

### 15.5 ChatViewModel 测试

- 支持模型生成正确的选项列表；
- 不支持模型隐藏或禁用选择器；
- 切换模型刷新选项；
- 新模型不支持当前值时回退默认；
- 发送时捕获当前选择；
- 活动运行期间控件不可修改或修改不影响活动快照。

### 15.6 UI 结构测试

- 模型选择器旁存在思考强度选择器；
- `ItemsSource`、`SelectedItem` 和可见性绑定正确；
- 控件运行状态绑定正确；
- 用户可见文字使用资源。

## 16. 建议实施步骤

1. 在 AgentLib 定义供应商无关的思考强度枚举；
2. 扩展模型定义，增加支持强度列表并保证旧配置兼容；
3. 在 AgentLib.Coding 增加不可变运行参数；
4. 让 CodingAgent 的每轮模型请求携带运行级 `ChatOptions`；
5. 扩展或复用 `SendMessageRequest` 承载 `ChatOptions`；
6. 在 OpenAI 协议适配器中实现 `reasoning_effort` 映射；
7. 为其他供应商保留独立映射入口；
8. 扩展 `ICodingChatRunner` 和 `CodingChatApplication` 传递运行参数；
9. 在 `ChatViewModel` 中增加可用强度、当前选择和发送快照；
10. 在聊天页增加选择器和运行状态控制；
11. 补齐配置、适配器、CodingAgent、应用层、ViewModel 和 UI 测试；
12. 构建整个解决方案并运行相关测试项目。

## 17. 第一阶段建议范围

第一阶段建议只实现：

- 模型显式声明支持列表；
- `Default`、`Low`、`Medium`、`High`；
- 单次运行快照；
- OpenAI 协议 `reasoning_effort` 映射；
- 工具调用多轮请求保持同一强度；
- 聊天页选择器；
- 必要的单元测试和请求映射测试。

第一阶段不实现：

- 每个会话独立保存强度；
- 自动根据任务复杂度调整强度；
- 运行过程中动态改变强度；
- 压缩请求独立强度；
- 根据模型名称自动推断能力；
- 不支持值的静默降级。

## 18. 核心结论

思考强度的最终传递路径应固定为：

```text
界面选择
  → 单次运行不可变快照
  → CodingChatApplication
  → ICodingChatRunner
  → CodingAgentRunOptions
  → 每轮请求的 ChatOptions
  → SendMessageRequest
  → IChatClient
  → 供应商协议字段
```

模型负责声明支持能力，CodingAgent 负责保持运行参数一致，供应商适配器负责协议映射，CodingChatRoom UI 不接触具体供应商 SDK。