# CopilotChatManager 技能（Skills）集成计划

## 背景

当前 `CopilotChatManager` 在 `CreateChatClientAgentAsync` 内部方法中创建 `ChatClientAgentOptions` 时，未设置 `AIContextProviders` 属性，导致 `ChatClientAgent` 框架的技能（Skills）机制未启用。技能是一种独立于 Tool 的上下文注入通道，允许 Agent 在运行前加载预定义的外部技能文件（如文本文件中的提示词、规则、知识等），并在对话过程中作为上下文自动提供给模型。

参考演示代码，`Microsoft.Agents.AI` 框架通过 `ChatClientAgentOptions.AIContextProviders` 集合接受 `AIContextProvider` 实现，其中 `AgentSkillsProvider` 是框架提供的内置技能加载器，通过技能文件夹路径 + 脚本运行委托的方式工作。

## 框架能力回顾

`ChatClientAgentOptions` 中与技能相关的属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `AIContextProviders` | `IList<AIContextProvider>` | AI 上下文提供者集合，Agent 运行时按顺序为每条消息注入上下文 |

框架提供的内置实现：

- **`AgentSkillsProvider(skillFolder, scriptRunner)`**：从指定文件夹加载技能文件，通过 `ScriptRunner` 委托执行技能脚本。

技能与 Tool 的区别：

| 维度 | Tool（函数工具） | Skill（技能） |
|------|-----------------|--------------|
| 触发方式 | 模型主动调用 | 每轮对话自动注入上下文 |
| 用途 | 执行具体操作（文件读写、子代理等） | 提供静态知识、规则、工作流指导 |
| 注册位置 | `ChatOptions.Tools` | `ChatClientAgentOptions.AIContextProviders` |
| 代表类型 | `SubAgentToolProvider`、`WorkspaceToolProvider` | `AgentSkillsProvider` |

## 设计目标

1. 为 `CopilotChatManager` 增加可选的技能加载能力
2. 保持向后兼容：默认不启用技能，行为与现状一致
3. 技能与现有 Tool 体系（`CopilotToolManager`）各自独立运作，互不干扰
4. 技能配置可在 Manager 级别设置全局默认值，也支持在 `SendMessageRequest` 级别覆盖
5. 不封装 `AgentSkillsProvider` 的具体构造细节，由调用方自行创建并注入

## 核心设计决策

### 决策 1：入口层级 —— 直接暴露 `AIContextProviders` 集合

在 `CopilotChatManager` 上提供 `AIContextProviders` 集合属性作为全局默认值，同时在 `SendMessageRequest` 上提供可选覆盖。

理由：

- **通用性**：不绑定 `AgentSkillsProvider` 一种实现。调用方可以注入任意 `AIContextProvider`（如 `AgentSkillsProvider`、未来的 MCP 上下文提供者、自定义实现等）
- **职责分离**：`CopilotChatManager` 不关心 provider 如何构造（`SkillFolder`、`ScriptRunner` 等），只负责传递集合给 `ChatClientAgentOptions`
- **与 `ChatReducer` 模式一致**：调用方自己创建实例，Manager 只负责传递
- 同时保留 Request 级别覆盖，允许单次请求临时调整上下文提供者

### 决策 2：不暴露 ScriptRunner

`CopilotChatManager` 完全不涉及 `ScriptRunner` 委托。调用方在外部创建 `AgentSkillsProvider` 时自行处理。

理由：

- `ScriptRunner` 是 `AgentSkillsProvider` 的构造细节，不是 `CopilotChatManager` 的职责
- 保持 Manager 职责单一：管理聊天会话，不管理技能运行时

### 决策 3：AIContextProviders 变更后的生效时机

与 `WorkspacePath` 一致：属性 setter 即时生效，但已在运行中的 Agent 不受影响。每次创建新的 `ChatClientAgent` 时读取最新值。

理由：

- 与现有 `WorkspacePath` 行为一致，调用方无需额外学习成本
- 避免在 Agent 运行期间动态注入上下文导致的不确定性

### 决策 4：AIContextProviders 为空时的处理

当 `AIContextProviders` 为 `null` 或空集合时，不向 `ChatClientAgentOptions.AIContextProviders` 赋值。不抛出异常。

理由：

- 技能是可选增强功能，不应因配置缺失而阻止正常对话
- 与 `ChatReducer = null` 跳过压缩的模式一致

### 决策 5：日志记录

不在 `CopilotChatManager` 中强制集成技能加载的日志。

理由：

- 与 `ChatReducer` 的日志策略一致（参考 `ChatHistoryCompression-Plan.md` 决策 5）
- 调用方可在自己的 `AIContextProvider` 实现内部自行记录

## 需要变更的文件

### 1. `AgentLib\CopilotChatManager.cs`

在 `CopilotChatManager` 类上新增属性：

```csharp
/// <summary>
/// AI 上下文提供者集合。设置后将在每次创建 <see cref="ChatClientAgent"/> 时注入到 <see cref="ChatClientAgentOptions.AIContextProviders"/>。
/// 调用方可添加任意 <see cref="AIContextProvider"/> 实现，如 <c>AgentSkillsProvider</c> 等。
/// 为 <see langword="null"/> 或空集合时，不会注入任何上下文提供者。
/// </summary>
public IList<AIContextProvider>? AIContextProviders { get; set; }
```

修改 `CreateChatClientAgentAsync` 内部方法（约第 345 行附近）：

- 在创建 `ChatClientAgentOptions` 之后、调用 `AsAIAgent` 之前
- 读取 `AIContextProviders`（优先使用 `request` 级别，回退到 Manager 级别）
- 当集合非空时，赋值给 `ChatClientAgentOptions.AIContextProviders`

### 2. `AgentLib\Model\SendMessages_\SendMessageRequest.cs`

新增可选覆盖字段：

```csharp
/// <summary>
/// 本次请求的 AI 上下文提供者集合。非 <see langword="null"/> 时覆盖 <see cref="CopilotChatManager.AIContextProviders"/>。
/// 设为空集合可临时禁用上下文提供者。
/// </summary>
public IList<AIContextProvider>? AIContextProviders { get; init; }
```

### 3. 不变更的文件

- `CopilotToolManager.cs` — 技能是独立通道，与 Tool 体系无关
- `SubAgentToolProvider.cs` — 子代理通过 Tool 机制工作，不受技能影响
- `CopilotChatSession.cs` — 技能上下文不影响会话数据结构
- `SendMessageResult.cs` — 技能注入对调用方透明
- `SendMessageAsync` 重载方法 — 保持现有签名不变（使用 Manager 全局默认值）

## 实施步骤

1. 在 `SendMessageRequest` 上新增 `AIContextProviders` 可选覆盖字段
2. 在 `CopilotChatManager` 上新增 `AIContextProviders` 属性作为全局默认值
3. 修改 `CopilotChatManager` 的 `CreateChatClientAgentAsync` 内部方法，解析最终 `AIContextProviders` 并注入到 `ChatClientAgentOptions.AIContextProviders`
4. 编译验证通过
5. 编写单元测试验证：AIContextProviders 为 null 时行为不变、AIContextProviders 非空时正确注入
