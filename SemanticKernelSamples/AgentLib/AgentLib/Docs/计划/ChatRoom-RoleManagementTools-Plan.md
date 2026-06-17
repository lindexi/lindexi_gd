# ChatRoom 角色管理工具集成计划

## 背景

当前 `ChatRoomManager` 管理的角色集合（`ObservableCollection<ChatRoomRole> Roles`）仅能通过代码或 UI 手动操作。在自动对话循环中，LLM 无法动态管理聊天室中的角色——无法查询当前有哪些角色、无法创建新角色、无法修改已有角色的属性。

本计划为聊天室新增三个角色管理工具（`AITool`），由 `ChatRoomManager` 在每次角色发言时自动注入到 `SendMessageRequest.Tools` 中，使 LLM 能够在对话过程中动态管理角色。

> **约束**：本计划所有变更仅限于 `AgentLib.ChatRoom` 项目，禁止改动 `AgentLib` 库项目。

## 设计目标

1. 提供「列举角色」工具，让 LLM 了解当前聊天室的角色组成
2. 提供「创建角色」工具，让 LLM 能在对话中动态创建新角色并加入聊天室
3. 提供「编辑角色」工具，让 LLM 能修改已有角色的属性（人设、模型等）
4. 工具自动注入，调用方无需手动管理
5. 输出格式为结构化纯文本，对 LLM 友好
6. 遵循 `AgentLib.ChatRoom` 现有工具创建模式（`AITool` + `AIFunction`），使用 `AIFunctionFactory.Create(委托, 工具名, 描述)` 方法创建工具，禁止采用反射方式创建

## 核心设计决策

### 决策 1：工具定义位置 —— AgentLib.ChatRoom 项目内

角色管理工具放在 `AgentLib.ChatRoom` 项目中，新建 `Tools/ChatRoomRoleManagementTools.cs`。

理由：
- 工具需要直接引用 `ChatRoomManager` 来操作 `Roles` 集合
- 工具仅在聊天室场景中有意义，放在 `AgentLib.ChatRoom` 符合关注点分离原则
- 与现有 `CopilotToolManager`（`AgentLib` 项目）无耦合

### 决策 2：注入时机 —— ChatRoomManager.StepAsync 自动注入

`ChatRoomManager` 在 `StepAsync` 中调用 `ChatRoomRole.SpeakAsync` 之前，自动将角色管理工具追加到 `SendMessageRequest.Tools`。

方式：通过 `SendMessageRequest` 的 `with` 表达式创建新请求，将三个工具追加到 `Tools` 列表：

```csharp
var roleManagementTools = CreateRoleManagementTools();
request = request with
{
    Tools = [.. request.Tools, .. roleManagementTools]
};
```

理由：
- 对调用方完全透明，无需手动管理
- 与 `AppendDefaultTools` 的行为一致：框架自动追加工具
- `SendMessageRequest` 是 `record struct`，通过 `with` 创建新实例不会修改原请求

### 决策 3：工具输出格式 —— 结构化纯文本

所有工具输出采用 Markdown 表格或结构化纯文本格式，对 LLM 自然、易解析。

- 「列举角色」：Markdown 表格，含 RoleId、RoleName、人设摘要、模型信息
- 「创建角色」：纯文本确认，含新角色的完整信息
- 「编辑角色」：纯文本确认，含修改前后的对比

理由：
- 纯文本是 LLM 最自然的输入格式，无需额外 JSON 解析
- Markdown 表格在 LLM 训练数据中常见，理解准确率高
- 避免 Token 浪费（JSON 格式含大量冗余结构字符）

### 决策 4：角色定位方式 —— 通过 RoleId 精确匹配

「编辑角色」通过 `roleId` 定位目标角色，不支持通过 `roleName` 匹配。

理由：
- `roleId` 是唯一标识，无歧义
- `roleName` 可能重复或被修改
- LLM 可从「列举角色」的输出中获取 `roleId`

如果 `roleId` 不存在，返回错误信息并附带当前角色列表，让 LLM 重新选择。

### 决策 5：创建后立即加入聊天室

「创建角色」成功后立即将新角色加入 `ChatRoomManager.Roles`。

理由：
- 符合直觉：创建角色就是为了让它参与对话
- 避免额外步骤（创建后还需手动加入）
- 新角色加入后，如果 `SpeakerSelector` 是 `RoundRobin`，会在下一轮自动被纳入轮流队列

---

## 工具详细设计

### 工具 1：list_characters（列举角色）

**功能描述**：列出当前聊天室中的所有角色。

**参数**：无。

**输出格式**：

```text
当前聊天室共有 3 个角色：

| RoleId    | RoleName | 人设摘要                         | 模型              | 参与模式       |
|-----------|----------|----------------------------------|-------------------|----------------|
| architect | 架构师   | 关注系统设计、扩展性和技术选型    | gemini-2.5-pro     | 总是参与       |
| dev       | 开发者   | 关注代码实现细节和性能            | (默认)            | 总是参与       |
| qa        | 测试     | 关注边界情况、异常场景和测试覆盖  | (默认)            | 仅被@时参与    |
```

- `RoleId`：角色唯一标识，用于「编辑角色」工具定位
- `RoleName`：角色显示名
- `人设摘要`：`SystemPrompt` 的前 50 个字符 + "..."（如果超出），为 null 或空时显示 "(未设置)"
- `模型`：`ModelProviderId` + `ModelId` 组合展示，均为 null 时显示 "(默认)"
- `参与模式`：`AlwaysParticipate` → "总是参与"，`MentionOnly` → "仅被@时参与"

### 工具 2：create_character（创建角色）

**功能描述**：创建一个新角色并加入当前聊天室。

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| `roleName` | string | ✅ | 角色显示名，如 "代码审查员" |
| `systemPrompt` | string | ✅ | 角色人设/系统提示词，定义角色的行为和专长 |
| `modelId` | string? | ❌ | 模型 ID，不传则使用聊天室默认模型 |
| `modelProviderId` | string? | ❌ | 模型提供商 ID，不传则使用聊天室默认提供商 |
| `memoryContent` | string? | ❌ | 角色的长期记忆内容，不传则为空 |

**内部行为**：

1. 校验 `roleName` 和 `systemPrompt` 非空
2. 自动生成 `RoleId`：`Guid.NewGuid().ToString("N")[..8]`
3. 设置 `IsHuman = false`、`ParticipationMode = AlwaysParticipate`
4. 创建 `ChatRoomRoleDefinition` 和 `ChatRoomRole`
5. 调用 `ChatRoomManager.Roles.Add(role)`

**输出格式**：

```text
✅ 角色创建成功并已加入聊天室：

- RoleId      : a1b2c3d4
- RoleName    : 代码审查员
- 人设        : 你是一位资深代码审查员，擅长发现代码中的安全漏洞...
- 模型        : (默认)
```

**错误处理**：
- `roleName` 为空或纯空白 → 返回错误："角色名称不能为空"
- `systemPrompt` 为空或纯空白 → 返回错误："角色人设不能为空"
- 创建失败（如内部异常） → 返回错误："创建角色失败：{异常消息}"

### 工具 3：edit_character（编辑角色）

**功能描述**：修改已有角色的属性。只更新传入的非空参数，未传入的字段保持不变（部分更新）。

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| `roleId` | string | ✅ | 目标角色的唯一标识 |
| `roleName` | string? | ❌ | 新的显示名 |
| `systemPrompt` | string? | ❌ | 新的人设/系统提示词 |
| `modelId` | string? | ❌ | 新的模型 ID |
| `modelProviderId` | string? | ❌ | 新的模型提供商 ID |
| `memoryContent` | string? | ❌ | 新的记忆内容 |

**内部行为**：

1. 通过 `roleId` 在 `ChatRoomManager.Roles` 中查找角色
2. 如果找不到，返回错误并附带可用角色列表
3. 对 `ChatRoomRoleDefinition` 中每个非 null 的参数进行更新
4. `roleId` 和 `IsHuman` 不可修改

**输出格式（成功）**：

```text
✅ 角色 "代码审查员" (a1b2c3d4) 编辑成功：

| 字段        | 修改前                                  | 修改后                                  |
|------------|----------------------------------------|----------------------------------------|
| systemPrompt | 你是一位资深代码审查员...               | 你是一位安全专家，擅长发现代码漏洞...    |
| modelId    | (默认)                                 | gemini-2.5-pro                          |
```

**输出格式（角色未找到）**：

```text
❌ 未找到 RoleId 为 "xyz123" 的角色。当前聊天室中的角色：

| RoleId    | RoleName |
|-----------|----------|
| architect | 架构师    |
| dev       | 开发者    |

请使用 list_characters 工具查看完整角色列表，并使用正确的 RoleId 重试。
```

---

## AITool 实现方案

### 创建方式

使用 `AIFunctionFactory.Create` 从委托创建工具：

```csharp
var tool = AIFunctionFactory.Create(
    (CancellationToken _) => ListCharacters(),
    name: "list_characters",
    description: "列出当前聊天室中的所有角色，包括角色ID、名称和人设摘要");
```

三个工具的 function signature：
- `list_characters`：`Func<CancellationToken, string>` — 无参数，返回文本
- `create_character`：`Func<string, string, string?, string?, string?, CancellationToken, string>` — roleName, systemPrompt, modelId, modelProviderId, memoryContent, ct → 返回文本
- `edit_character`：`Func<string, string?, string?, string?, string?, string?, CancellationToken, string>` — roleId, roleName, systemPrompt, modelId, modelProviderId, memoryContent, ct → 返回文本

> 注：`AIFunctionFactory.Create` 通过委托签名自动推断参数名和类型，无需手动指定 `AIFunctionParameter` 或使用反射。

### 类设计

```csharp
namespace AgentLib.ChatRoom.Tools;

/// <summary>
/// 提供聊天室角色管理相关的 <see cref="AITool"/> 集合。
/// </summary>
public static class ChatRoomRoleManagementTools
{
    /// <summary>
    /// 创建角色管理工具集合（list_characters、create_character、edit_character）。
    /// </summary>
    /// <param name="chatRoomManager">聊天室管理器，用于操作角色集合。</param>
    /// <returns>角色管理工具列表。</returns>
    public static IReadOnlyList<AITool> CreateTools(ChatRoomManager chatRoomManager)
    {
        // ...
    }
}
```

---

## 需要变更的文件

### 新增

#### 1. `AgentLib.ChatRoom/Tools/ChatRoomRoleManagementTools.cs`

静态工具工厂类，创建三个 `AITool` 实例。

### 修改

#### 2. `AgentLib.ChatRoom/ChatRoomManager.cs`

在 `StepAsync` 方法中，调用 `ChatRoomRole.SpeakAsync` 之前，将角色管理工具注入到请求中。

改动点：
```csharp
// 在构建 SendMessageRequest 后追加工具
var roleTools = ChatRoomRoleManagementTools.CreateTools(this);
// 将 roleTools 合并到 request.Tools
```

---

## 实施步骤

1. **创建 `ChatRoomRoleManagementTools.cs`** — 实现三个工具的创建逻辑和对应的私有方法
2. **修改 `ChatRoomManager.cs`** — 在 `StepAsync` 中自动注入角色管理工具
3. **编译验证** — `dotnet build` 确保无编译错误