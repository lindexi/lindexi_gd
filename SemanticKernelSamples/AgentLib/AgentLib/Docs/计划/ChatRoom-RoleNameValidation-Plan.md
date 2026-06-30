# ChatRoom 角色名可解析性校验设计

## 背景

当前聊天室中，`MentionedRoleIds` 的解析由 `MentionParser.ParseMentions` 负责。该解析器使用正则表达式从消息文本中提取 `@角色名` 或 `@[角色名]` 格式的提及，然后通过角色名（`RoleName`）反查角色 ID（`RoleId`）。

但在创建角色时，角色名没有经过任何校验——无论是通过 LLM 调用 `create_character` 工具创建、通过 UI 界面手动创建、还是通过代码直接构造 `ChatRoomRoleDefinition`，角色名都是直接写入 `Definition.RoleName`，不做任何检查。

这导致以下问题：**如果角色名包含特殊字符或格式不便于正则解析，后续被 `@` 时将无法正确匹配。** 例如角色名 "白帽·信息员"：

- 使用 `@白帽·信息员` 格式时，正则 `@(\S+?)(?:\s|$)` 中的 `\S+?` 是**非贪婪**匹配。虽然 `\S` 能匹配 `·`（U+00B7），但非贪婪特性意味着正则引擎会尽可能少地匹配字符。
- 如果 LLM 或用户在 `@` 后紧跟的文本中，角色名后面没有恰好出现空格或行尾（例如标点符号、换行符差异等），`@(\S+?)(?:\s|$)` 的锚定 `(?:\s|$)` 就无法满足，导致匹配失败。
- 即使正则能匹配，角色名中的特殊字符 `·` 可能在不同输入法、不同 LLM 输出中产生不同的 Unicode 码点（如 `·` U+00B7、`•` U+2022、`・` U+30FB），导致字面上看起来一样但实际匹配失败。
- 使用 `@[白帽·信息员]` 方括号格式可以避免正则问题，但无法保证 LLM 或用户每次都使用方括号格式。

**根本原因**：角色创建时缺乏"可解析性校验"——没有验证角色名是否能被 `MentionParser` 的正则正确提取和匹配。

## 设计目标

1. 在 LLM 工具 `create_character` 和 `edit_character` 创建/修改角色名时，校验角色名是否能被 `MentionParser` 正确解析
2. 校验逻辑直接复用 `ParseMentions`，确保"创建时能解析"等价于"使用时能被 @"
3. 校验不通过时返回明确的错误提示，指导 LLM 修正角色名
4. UI 界面不做任何校验，用户命名不可被 @ 的角色是用户自己的事
5. 不新增公开 API，`MentionParser` 保持 `internal`

## 根因分析

### MentionParser 正则解析逻辑

`MentionParser` 的正则定义（`MentionParser.cs` 第 17-19 行）：

```csharp
private static readonly Regex MentionRegex = new(
    @"@\[([^\]]+)\]|@(\S+?)(?:\s|$)",
    RegexOptions.Compiled);
```

- **捕获组 1**：`@[xxx]` 方括号格式，`xxx` 可以是除 `]` 外的任意字符
- **捕获组 2**：`@xxx` 常规格式，`xxx` 是非空白字符的非贪婪匹配，必须以空格或结尾终止

解析流程：
1. 用正则从消息文本中提取所有 `@` 后面的名称
2. 用名称在 `Roles` 列表中做 `RoleName → RoleId` 的不区分大小写查找
3. 匹配成功的收集为 `MentionedRoleIds`

### 失败场景

| 场景 | 角色名 | `@` 格式 | 结果 | 原因 |
|------|--------|----------|------|------|
| 正常英文 | `Helper` | `@Helper help` | ✅ 解析成功 | `\S+?` 匹配 "Helper"，后面是空格 |
| 正常中文 | `代码专家` | `@代码专家 帮我看看` | ✅ 解析成功 | `\S+?` 匹配 "代码专家"，后面是空格 |
| 含中点 | `白帽·信息员` | `@白帽·信息员 帮我看看` | ⚠️ 可能失败 | `\S+?` 非贪婪，`·` 的 Unicode 码点可能不一致 |
| 含中点 | `白帽·信息员` | `@白帽·信息员，帮我看看` | ❌ 失败 | `，`不是 `\s`，`(?:\s\|$)` 不满足 |
| 含空格 | `Code Expert` | `@Code Expert help` | ❌ 失败 | `\S+?` 只匹配 "Code"，"Expert" 被截断 |
| 含方括号 | `测试[1]` | `@测试[1] help` | ❌ 失败 | 正则优先匹配 `@\[...\]`，但 `测试[1]` 不在方括号内 |

### 当前角色创建入口

| 入口 | 文件 | 方法 | 校验情况 |
|------|------|------|----------|
| LLM 工具：`create_character` | `Tools/ChatRoomRoleManagementTools.cs` | `CreateCharacter` | 仅检查非空（`IsNullOrWhiteSpace`） |
| LLM 工具：`edit_character` | `Tools/ChatRoomRoleManagementTools.cs` | `EditCharacter` | 仅检查非空 |
| UI：AvaloniaDemo | `ViewModels/RoleListViewModel.cs` | `AddRole` | 无校验 |
| UI：AvaloniaDemo 编辑 | `ViewModels/RoleEditViewModel.cs` | `Save` | 无校验 |
| UI：AvaloniaShell 编辑 | `ViewModels/RoleEditViewModel.cs` | `SaveAsync` | 无校验 |
| 代码：ChatRoomService | `Services/ChatRoomService.cs` | `AddRoleAsync` | 无校验 |
| 代码：ChatRoomManager | `ChatRoomManager.cs` | `AddRoleAsync` | 无校验 |

本次仅在前两个入口（LLM 工具）增加校验。UI 和代码入口不做校验——用户自行命名不可被 @ 的角色是用户自己的事，不应让用户去理解底层正则实现的复杂性。

## 核心设计

### 校验方法 —— 直接复用 ParseMentions

在 `MentionParser` 中新增一个 `internal static` 方法，用 `ParseMentions` 做反向验证：

1. 用待校验的角色名构造一个临时角色（`RoleId = "validation-test"`，`RoleName = 待校验名称`）
2. 构造模拟消息 `"@{角色名} "`（`@角色名` 后跟空格，最宽松的格式）
3. 调用 `ParseMentions` 解析这条消息
4. 如果解析结果包含 `RoleId = "validation-test"`，说明角色名能被正确提取和匹配，校验通过
5. 否则校验不通过

```csharp
/// <summary>
/// 校验角色名是否能被 @mention 正确解析。
/// 构造模拟消息并调用 <see cref="ParseMentions"/> 验证，确保角色名在实际使用中能被 @ 正确匹配。
/// </summary>
/// <param name="roleName">待校验的角色名。</param>
/// <returns>能被解析返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
internal static bool CanParseRoleName(string roleName)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

    // 构造一个临时角色用于 ParseMentions 的 RoleName → RoleId 映射
    var tempRole = new ChatRoomRole(new ChatRoomRoleDefinition
    {
        RoleId = "validation-test",
        RoleName = roleName,
    });

    // 构造模拟消息：@角色名 后跟空格（最宽松的 @ 格式）
    string testMessage = $"@{roleName} ";

    // 用 ParseMentions 尝试解析
    IReadOnlyList<string> result = ParseMentions(testMessage, [tempRole]);

    // 如果解析结果包含临时角色的 RoleId，说明角色名能被正确提取和匹配
    return result.Count > 0 && result[0] == "validation-test";
}
```

**理由**：
- 直接复用 `ParseMentions`，不引入第二套正则逻辑，保证校验和解析完全一致
- 构造最宽松场景（`@角色名 ` 后跟空格），如果连这个都无法解析，实际使用中更不可能成功
- 方法返回 `bool`，简单直接，不引入额外的结果类型

### 校验范围 —— 仅 LLM 工具

| 入口 | 是否校验 | 原因 |
|------|----------|------|
| `create_character` 工具 | ✅ 校验 | LLM 创建角色后需要通过 @ 来分配任务，角色名不可解析会导致整个协作流程断裂 |
| `edit_character` 工具 | ✅ 校验（仅当修改角色名时） | 同上 |
| UI 界面 | ❌ 不校验 | 用户自行命名，不应让用户理解底层正则实现 |
| `ChatRoomService.AddRoleAsync` | ❌ 不校验 | 代码入口，保持简单 |
| `ChatRoomManager.AddRoleAsync` | ❌ 不校验 | 内部核心方法 |

### 校验失败行为

`create_character` 和 `edit_character` 工具校验失败时：

- 返回格式化的错误提示字符串（不抛异常，因为工具返回值就是字符串）
- 不创建/不修改角色
- 错误提示中包含简单的修正建议，不暴露正则细节

提示格式示例：

```
❌ 角色名 "白帽·信息员" 无法被 @ 正确解析，其他角色或用户在消息中使用 @ 该角色名时将无法触发其发言。

请使用不含特殊符号的角色名，例如仅包含中文、英文、数字、连字符（-）或下划线（_）的名称。
```

## 需要修改的文件

### AgentLib.ChatRoom 项目

| 文件 | 改动 |
|------|------|
| `MentionParser.cs` | 新增 `internal static bool CanParseRoleName(string roleName)` 方法 |
| `Tools/ChatRoomRoleManagementTools.cs` | `CreateCharacter` 和 `EditCharacter` 方法增加角色名校验 |

### AgentLib.ChatRoom.Tests 项目

| 文件 | 改动 |
|------|------|
| `MentionParserTests.cs` | 新增 `CanParseRoleName` 方法的单元测试 |
| `Tools/ChatRoomRoleManagementToolsTests.cs` | 新增 `create_character` / `edit_character` 校验失败的测试 |

## 实施步骤

1. **在 `MentionParser` 中新增 `CanParseRoleName` 方法**
   - 构造临时角色（`RoleId = "validation-test"`，`RoleName = 待校验名称`）
   - 构造模拟消息 `"@{角色名} "`
   - 调用 `ParseMentions` 解析
   - 返回解析结果是否包含临时角色的 `RoleId`

2. **在 `CreateCharacter` 中增加校验**
   - 在非空检查之后、创建 `ChatRoomRoleDefinition` 之前调用 `MentionParser.CanParseRoleName`
   - 校验失败时返回错误提示字符串，不创建角色

3. **在 `EditCharacter` 中增加校验**
   - 在 `roleName` 参数非空且非空白时调用 `MentionParser.CanParseRoleName`
   - 校验失败时返回错误提示字符串，不修改角色名

4. **编写单元测试**
   - 测试正常角色名（中文、英文）校验通过
   - 测试含特殊字符的角色名校验失败
   - 测试 `create_character` 工具校验失败的返回提示
   - 测试 `edit_character` 工具校验失败的返回提示

## 开放问题

1. **已有角色名是否需要批量校验**：当前已创建的、包含特殊字符的角色名是否需要在加载时批量校验并提示？建议暂不处理，仅在新创建/编辑时校验。
2. **角色名唯一性校验**：`MentionParser` 使用 `TryAdd` 保证同名角色以先注册的为准。是否需要在创建时校验角色名唯一性？建议作为独立的后续改进，不在本次范围内。
