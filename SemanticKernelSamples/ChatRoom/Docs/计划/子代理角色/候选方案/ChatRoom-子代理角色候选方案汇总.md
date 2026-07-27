# ChatRoom 子代理角色候选方案汇总

## 1. 文档目的

本文档用于比较“子代理身份如何进入现有角色模型”的候选方案，并给出推荐结论。

产品行为基线见[需求功能](../ChatRoom-子代理角色需求功能.md)。实现细节按需阅读各候选方案，不需要一次加载全部文档。

## 2. 共同约束

无论采用哪种角色建模方案，都必须满足：

- 子代理不进入普通自动发言队列或管理者兜底。
- 继续使用现有 Mention 语法；解析结果补充来源消息、匹配位置和是否位于消息开头。
- 用户只有在子代理 Mention 位于消息开头时才能触发目标角色；AI 普通文本中的子代理 Mention 不触发。
- Standard AI 角色通过 ChatRoom 自有 `InvokeChatRoomSubAgent` AITool 调用子代理，避免与 AgentLib 默认 `InvokeSubAgent` 重名。
- 用户 Mention 与 AI 工具最终都复用现有 `StepAsync` / 普通角色 runtime、AgentSession 和 checkpoint。
- Standard 子代理必须通过 `ReturnOutputToCaller` 提交结果；第一次未提交提醒一次，第二次失败。Coding 子代理沿用现有 Coding/AgentLib 结果并视为等价提交。
- 子代理原始输出作为普通角色消息显示和持久化，并设置 `IsPresetInfo = true`。
- 其他角色后续上下文和 Mention 调度跳过 preset 子代理消息；AI 调用时 Standard 返回工具值或 Coding 现有完成结果交给父 AI。
- AgentLib 现有 `SubAgentToolProvider` 与 Coding 执行链路保持不变；ChatRoom 使用独立命名的 `ChatRoomSubAgentToolProvider`。
- ChatRoom Standard 默认工具列表必须排除 AgentLib 的 `InvokeSubAgent`，只暴露 `InvokeChatRoomSubAgent`。
- 不增加环路、深度、自调用或角色字段组合等专用硬约束。
- legacy `ChatRoomManager` 与新 `ChatRoomCoordinator` 对同一角色定义保持一致解释。
- 产品尚未发布，持久化结构直接升级，不实现旧数据兼容迁移；legacy 会话、模板和 snapshot 使用显式格式版本拒绝旧数据。

## 3. 候选方案

### 3.1 方案一：独立 InvocationMode

在角色定义上新增独立调用模式：

```text
Participant
SubAgent
```

调用模式只回答“调度器对该角色采用普通触发规则还是子代理触发规则”。角色仍通过现有执行入口运行；参与模式、执行引擎和管理者身份继续由现有字段表达。

详见[方案一：独立 InvocationMode](方案一-独立InvocationMode.md)。

### 3.2 方案二：扩展 ParticipationMode

把子代理加入现有参与模式：

```text
AlwaysParticipate
MentionOnly
SubAgentOnly
```

该方案用最少的新字段表达调度隔离，但会把“参与时机”和“调用协议”合并到同一枚举。

详见[方案二：扩展 ParticipationMode](方案二-扩展ParticipationMode.md)。

### 3.3 方案三：独立子代理注册表

普通角色定义保持不变，房间另行维护子代理定义与运行时注册表。普通角色集合和子代理集合从模型层开始分离。

详见[方案三：独立子代理注册表](方案三-独立子代理注册表.md)。

## 4. 比较

| 比较维度 | 独立 InvocationMode | 扩展 ParticipationMode | 独立子代理注册表 |
|----------|---------------------|------------------------|------------------|
| 保持触发语义独立 | 好 | 弱 | 好 |
| 复用现有角色配置 | 好 | 好 | 需要复制或抽取 |
| legacy 改造成本 | 中 | 低到中 | 高 |
| Coordinator 长期边界 | 好 | 中 | 好 |
| 当前数据结构调整 | 新增正交字段 | 扩展现有枚举 | 需要双集合和映射 |
| 保持触发语义清晰 | 好 | 弱 | 好 |
| UI 和模板传播成本 | 中 | 中 | 高 |
| 未来扩展更多调用模式 | 好 | 容易继续膨胀 | 好 |
| 两套架构一致落地 | 好 | 可行但易遗漏 | 难度较高 |
| 对现有代码侵入 | 中 | 低 | 高 |

## 5. 推荐结论

推荐采用**方案一：独立 InvocationMode**。

主要理由：

1. 子代理身份描述的是触发方式，不是普通聊天室中的参与时机。
2. Standard/Coding 是执行引擎，MentionOnly/AlwaysParticipate 是普通参与策略，Manager 是调度身份；四个维度应保持正交。
3. legacy 与 Domain 角色已经承载稳定身份、人设、模型、技能和执行引擎，新增一个正交字段比复制整套角色模型更稳妥。
4. Mention 解析只需补充结构化位置，调度器即可结合 `InvocationMode` 判断触发，不需要新协议。
5. `StepAsync`、角色会话、checkpoint、普通消息和 UI 都可直接复用，避免创建第二套运行模型。
6. 新字段只参与必要的调度判断，不需要通过跨字段不变量或多层入口保护进行过度防御。
7. 未来若增加其他触发方式，可以在调用模式维度扩展，而不污染参与模式或执行引擎。

## 6. 按需阅读

- [方案一：独立 InvocationMode](方案一-独立InvocationMode.md)：推荐方案的完整实施设计。
- [方案二：扩展 ParticipationMode](方案二-扩展ParticipationMode.md)：最少新增字段，但会混合参与时机与调用协议。
- [方案三：独立子代理注册表](方案三-独立子代理注册表.md)：边界最彻底，但首版改造范围过大。
- [探索过程导航](../ChatRoom-子代理角色探索过程.md)：调查事实、推导过程和候选方案探索入口。

## 7. 决策状态

- 当前推荐：方案一，独立 InvocationMode。
- 当前状态：方案设计，待人类审核与实施。
- 若需求功能文档发生变化，应先更新需求，再重新比较候选方案。
