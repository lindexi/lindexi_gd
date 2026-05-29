# Copilot 聊天历史 XML 落盘约定

## 问题答案

Copilot 现在除了继续写入 `Logs/.../CopilotChatLogs` 下的可读文本日志，还会在与 `Logs`、`Configurations` 平级的 `CopilotChatHistory` 文件夹里，为每个会话维护一份 XML 历史文件。

## 当前约定

1. 目录由 `AppPathManager.CopilotChatHistoryDirectory` 创建，位于应用数据根目录下。
2. 每个会话只对应一个 XML 文件，文件名格式为 `yyyyMMdd_HHmmss_{SessionId:N}.xml`。
3. 每次有正式聊天消息落盘时，都会重写当前会话对应的 XML 文件，使其包含该会话目前为止的全部已记录消息。
4. XML 会保留消息角色、作者、创建时间、是否预设信息、正文、思考内容、消息片段集合，以及可用的 token 用量统计。
5. 如果当前会话已经拿到了机器侧 `AgentSession` 的持久化状态，也会额外写入 `AgentSessionState` 节点，供后续恢复机器记忆使用。
6. 预设欢迎语这类没有经过正式日志记录链路的消息，仍不会自动进入历史文件。

## XML 结构概览

根节点为 `CopilotChatSessionHistory`，带有：

- `SessionId`
- `CreatedTime`

其下包含 `Messages` 节点，内部每条消息使用一个 `Message` 元素，当前字段包括：

- `Role`
- `Author`
- `CreatedTime`
- `IsPresetInfo`
- `Content`
- `Reason`
- `MessageItems`
- `UsageDetails`（可选）

其中 `UsageDetails` 目前按属性写入：

- `TotalTokenCount`
- `InputTokenCount`
- `OutputTokenCount`
- `ReasoningTokenCount`
- `CachedInputTokenCount`

## 适用场景

- 需要把整段会话反序列化出来做历史查询时。
- 需要在恢复 `ChatClientAgent` 会话时继续沿用机器侧记忆时。
- 需要对接后续聊天历史浏览能力时。
- 需要核对某个 `SessionId` 当前已记录到哪一步时。

## 修改建议

- 如果后续要扩展 XML 字段，优先追加新字段，不要随意改动已有元素和属性名称。
- 如果后续要从“仅保存 `ConversationId`”升级为“保存完整 `AgentSession` 序列化结果”，优先保持 `AgentSessionState` 节点名称稳定。
- 如果要做跨版本兼容，先在读取端容忍缺失字段，再考虑写入端升级。
- 如果单个会话体积持续变大，再评估是否需要增加分段归档，而不是提前引入复杂格式。
