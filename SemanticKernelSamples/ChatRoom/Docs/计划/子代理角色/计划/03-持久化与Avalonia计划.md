# ChatRoom 持久化与 Avalonia 实施计划

## 1. 目标

在模型、调度和执行稳定后，开放完整产品入口：

- `room.config.json` 保存 `InvocationMode`、`IsPresetInfo` 和结构化 `Mentions`。
- 角色模板保存 `InvocationMode`。
- 两套格式各自提升版本，明确拒绝旧版和未来版，不实现迁移。
- 恢复 Assistant 消息时把外层 preset 传播到底层 `CopilotChatMessage`。
- Avalonia 可编辑和展示角色调用模式。
- 点击 SubAgent 的 Mention 操作时插入到消息索引 0。
- 发送消息不再 `Trim()` 掉前导空白，保证位置语义真实。

本阶段不增加子代理专用消息模板、卡片或事件。

## 2. 当前代码事实

### 2.1 存在两套独立格式

| 格式 | 当前入口 | 当前版本状态 |
|---|---|---|
| 会话配置 | `ChatRoomPersistence` / `ChatRoomSessionData` | 无版本 |
| 角色模板 | `RoleTemplateService` / `RoleTemplate` | 无版本 |

它们的 DTO、失败策略和使用者不同，不应共享一个模糊的全局版本号。

### 2.2 JSON 默认值会掩盖缺失字段

`System.Text.Json` 会把缺失 enum/bool 读成默认值。仅在反序列化后检查 `InvocationMode == Participant` 或 `IsPresetInfo == false`，无法区分新格式的显式值和旧格式的缺失字段。

需要局部检查版本和本次新增必需字段是否存在，但不需要递归验证所有既有属性。

### 2.3 模板有三条手工复制路径

`RoleTemplateService.ToDefinition`、`FromDefinition`、`UpdateFromDefinition` 都逐字段复制角色定义。新增字段必须同时加入，内置 preset 和 Coding runtime template 也应显式设置 Participant。

### 2.4 Avalonia 当前会改写 Mention 位置

`ChatViewModel.SendAsync` 使用 `InputText.Trim()`，会删除前导空白；`InsertMention` 总是把新 Mention 追加到现有文本末尾。这样 UI 无法稳定表达“索引 0 的 SubAgent Mention”。

## 3. 版本策略

### 3.1 三个独立版本

建议值：

```text
ChatRoomSessionData.CurrentFormatVersion = 1
RoleTemplate.CurrentFormatVersion = 1
```

会话配置和模板此前没有版本，因此本次从 1 开始。

保存入口必须显式写当前版本。加载入口只接受严格等于当前版本的值。

### 3.2 最小严格读取

每个加载入口按以下顺序处理：

1. 读取 JSON。
2. 使用 `JsonDocument` 检查根对象和版本属性。
3. 版本不等于当前值时拒绝。
4. 对本次新增且默认值可能掩盖缺失的字段做存在性检查。
5. 使用现有反射或 source-generated `System.Text.Json` 反序列化。
6. 使用 DTO、枚举校验和领域构造不变量验证值。

需要检查的新增字段：

- 角色 `InvocationMode`。
- 消息 `IsPresetInfo`。
- 消息 `Mentions`。
- Mention 的 `TargetRoleId`、`SourceMessageId`、`StartIndex`、`Length`。

不建立通用 `JsonFormatValidator` 框架，不逐字段重复验证全部旧属性，也不强行统一会话配置与角色模板的异常模型。

### 3.3 失败策略保持入口现状

- 指定会话加载：格式错误抛 `InvalidDataException`。
- 模板批量加载：无效或旧版文件按现有策略跳过，不影响其他模板。

## 4. 会话配置

### 4.1 模型和源生成上下文

`ChatRoomSessionData` 增加 `FormatVersion`。角色定义和消息模型已经由前两阶段增加新字段，源生成上下文登记 `ChatRoomMention` 及集合类型。

保持 net6.0 反射序列化和新 TFM source-generated 序列化两条路径。

### 4.2 保存

`SaveConfigAsync` / `ChatRoomSession.ToPersistence` 统一保证：

- 写入当前 `FormatVersion`。
- 角色保存 `InvocationMode`。
- 消息保存 `IsPresetInfo` 和 `Mentions`。
- 不写旧 `MentionedRoleIds`。
- 流式结束后保存 `StaticContent`。

若当前存在两处手写 `ChatRoomSessionData` 组装，收敛为一个小 mapper/helper；不要重构整个文件仓储。

### 4.3 加载和恢复

加载通过版本与新增字段检查后，继续执行现有角色存储标识、ExecutionKind 和 Human/Coding 校验，并增加 InvocationMode 枚举校验。

`ChatRoomManager.LoadAsync`：

- 使用持久化的结构化 Mention，不重新解析历史文本。
- 恢复 Assistant 的底层 `CopilotChatMessage` 时同步设置 `IsPresetInfo`。
- Participant 继续恢复角色 AgentSession。
- SubAgent 即使加载了最近 AgentSession，下一次调用仍由执行阶段创建新会话。

不修改底层 AgentSession 文件格式，不新增 ChatRoom envelope。

## 5. 角色模板

`RoleTemplate` 增加独立 `FormatVersion`。所有保存到磁盘的模板写当前版本。

更新：

- `RoleTemplateService.LoadAll`：先检查版本和 Definition.InvocationMode 是否存在；无效文件跳过。
- `ToDefinition`：复制 InvocationMode。
- `FromDefinition`：复制 InvocationMode并写当前模板版本。
- `UpdateFromDefinition`：复制 InvocationMode并保持当前模板版本。
- `PresetTemplates`：现有模板显式 Participant。
- `CodingAssistantRoleFactory`：runtime Coding 模板显式 Participant。

不因为 ExecutionKind 或 MentionOnly 推断 SubAgent。

## 6. Avalonia 最小改造

### 7.1 角色编辑

`RoleEditViewModel` 增加调用模式选择：

```text
普通参与者
子代理
```

加载、创建和更新时映射 `ChatRoomRoleInvocationMode`。不自动联动 ParticipationMode、ExecutionKind、IsManagerRole 或 IsHuman。

`RoleEditView.axaml` 在参与模式附近增加一个 ComboBox 和简短说明：

- 普通参与者按当前自动队列和 Mention 规则参与。
- 子代理不自动参与，只能通过开头 Mention 或工具调用。

### 7.2 角色列表

`RoleItemViewModel` 增加调用模式显示。现有 `ParticipationModeDisplay` 可改为组合文本或增加独立属性，至少能区分：

- 人类。
- 普通 AI 角色。
- Standard 子代理。
- Coding 子代理。

角色列表不需要新图标体系或新模板类型。

SubAgent 隐藏或禁用“压缩对话”“清空记忆”是 UI 简化，不在服务层增加拒绝。若隐藏会导致明显 UI 改造，也可首期保留命令；正确性由 fresh invocation 保证。

### 7.3 Mention 插入

从角色列表点击 Mention 时需要携带角色调用模式，而不是只传 RoleName。

规则：

- Participant：保持现有追加到输入末尾的行为。
- SubAgent：把 `@角色名 ` 插入到输入索引 0；原有输入作为任务正文接在后面。
- 若输入已经以该 SubAgent Mention 开头，不重复插入。
- 不在插入时自动发送。

建议事件参数改为轻量角色信息或直接传 `RoleItemViewModel`，不要新增全局 UI event bus。

### 7.4 发送文本

`SendAsync` 保留原始前导空白，只拒绝全空白输入。可以去除结尾换行/空白以保持现有体验，但不得改变首字符索引。

最小安全规则：

```text
text = InputText.TrimEnd()
```

若尾随空白也是业务任务的一部分，则直接发送原文；关键门禁是禁止 `Trim()` 和 `TrimStart()`。

### 7.5 消息显示

继续使用现有 Assistant 消息模板。`IsPresetInfo` 只影响上下文和调度，不要求增加“子代理消息”模板。可选增加弱标签，但不属于完成门禁。

## 7. 按序实施任务

### 03-01 升级会话配置格式

增加 FormatVersion、新字段序列化和加载入口的局部存在性检查。

### 03-02 修复消息恢复

恢复底层 Assistant 消息时传播 preset，并保持持久化 Mentions 原值。

### 03-03 升级角色模板格式

增加版本，迁移三条复制路径和所有内置模板创建点。

### 03-04 增加角色调用模式编辑

修改 RoleEdit ViewModel、服务更新参数和 XAML 控件。

### 03-05 增加角色类型展示

让列表可区分 Participant/SubAgent 和 Standard/Coding。

### 03-06 修正 Mention 插入位置

Participant 追加，SubAgent 插入索引 0，并保留原任务文本。

### 03-07 修正发送文本处理

移除全量 Trim，保证前导空白不会被 UI 改写。

### 03-08 更新 README

说明角色调用模式、两种调用方式、Standard 正式返回协议、调用间无记忆和 preset 可见性。

## 8. 关键测试

### 会话配置

- 当前版本完整往返。
- InvocationMode、preset、Mention span 往返。
- 缺失/旧版/未来版拒绝。
- 缺失新增字段拒绝，而显式 Participant/false/空 Mentions 合法。
- 恢复后外层与底层 preset 一致。
- net6.0 和 source-generated 路径语义一致。

### 模板

- 三条转换保留 InvocationMode。
- 旧版和缺失字段模板被跳过，其他模板仍返回。
- preset 与 runtime Coding 模板显式 Participant。

### Avalonia

- 编辑已有 SubAgent 正确回显和保存。
- 新建默认 Participant。
- Participant Mention 仍追加。
- SubAgent Mention 插到索引 0，并保留原正文。
- 前导空白发送后仍存在，使 Mention 的 StartIndex 不被改写。
- 消息继续使用现有模板展示。

## 9. 完成门禁

- 两套格式只接受当前版本，新字段完整往返。
- 没有旧字段双写和旧格式迁移分支。
- 不存在通用 JSON shape-validation 框架。
- Avalonia 可配置、展示和正确触发 SubAgent。
- 未增加子代理专用消息模板。
- AgentSession 底层文件格式、Coding 执行链和 AgentLib 现有子代理实现无改动。
