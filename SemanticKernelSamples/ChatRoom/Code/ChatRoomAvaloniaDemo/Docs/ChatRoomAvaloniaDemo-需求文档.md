# ChatRoomAvaloniaDemo 需求文档

## 1. 项目概述

### 1.1 项目定位

ChatRoomAvaloniaDemo 是一个基于 Avalonia UI 的多角色 AI 聊天室桌面应用。多个 LLM 角色在同一个聊天室中自动对话，人类可以作为其中一个角色随时插话。每个角色拥有独立的系统提示词（人设）、记忆、模型和工具。

### 1.2 参考对象

AutoGen / CrewAI 等框架的 GroupChat 模式。

### 1.3 项目结构

```
SemanticKernelSamples/AgentLib/
├── AgentLib/                          ← 基础库：单人聊天核心（CopilotChatManager、模型管理、工具、日志）
├── AgentLib.ChatRoom/                 ← 群聊库：多角色编排（ChatRoomManager、ChatRoomRole、发言选择器、持久化）
├── AgentLib.Tests/                    ← 基础库测试
├── AgentLib.ChatRoom.Tests/           ← 群聊库测试
├── ChatRoom/Code/ChatRoomAvaloniaDemo/← Avalonia UI 桌面应用（本项目）
│   ├── Models/                        ← 配置数据模型
│   ├── Services/                      ← 服务层（配置加载、聊天室服务封装）
│   ├── ViewModels/                    ← MVVM ViewModel 层
│   ├── Views/                         ← Avalonia XAML 视图
│   └── Styles/                        ← 全局样式资源
└── Microsoft.Agents.AI.*              ← 第三方 AI 扩展包
```

### 1.4 目标框架

- AgentLib / AgentLib.ChatRoom: `.NET 6; .NET 9` (多目标)
- ChatRoomAvaloniaDemo: `.NET 10`

---

## 2. 核心功能需求

### 2.1 多角色自动对话

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-01 | 支持在聊天室中添加多个 LLM 角色，每个角色拥有独立的系统提示词、记忆内容和模型配置 | P0 |
| F-02 | 角色按可插拔的发言策略自动轮流发言，默认为 RoundRobin（固定顺序轮流） | P0 |
| F-03 | 每个角色发言时，只看到"自己上次发言之后"其他角色/人类产生的增量公开消息，看不到其他角色的内部思考和工具调用 | P0 |
| F-04 | 角色发言通过 LLM 流式输出，使用内部 CopilotChatManager 管理私有会话历史（含工具调用、思考细节） | P0 |
| F-05 | 支持最大对话轮次限制（默认 10 轮） | P1 |
| F-06 | 某个角色发言失败时，生成系统错误消息，不中断整个聊天室循环 | P0 |

### 2.2 人类插话

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-10 | 人类可以随时在聊天室中插话，消息直接追加到共享对话历史 | P0 |
| F-11 | 人类插话后自动启动新一轮自动循环，让 AI 角色回复 | P0 |
| F-12 | 人类消息中可以通过 @RoleName 提及特定角色，被提及的角色优先回复 | P1 |
| F-13 | 人类插话后，被 @ 的角色回复完毕即暂停，不继续无限循环 | P1 |

### 2.3 @Mention 机制

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-20 | 支持 `@RoleName` 和 `@[Role Name]` 两种 @mention 格式 | P1 |
| F-21 | 消息追加到聊天室时自动解析 @mention，填充 `MentionedRoleIds` | P1 |
| F-22 | @mention 触发链式调用：角色 A @ B → B 发言 → B @ C → C 发言 | P1 |
| F-23 | 角色可配置参与模式：`AlwaysParticipate`（默认参与轮流）或 `MentionOnly`（仅被 @ 时才发言） | P2 |

### 2.4 角色管理

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-30 | 通过 UI 添加新角色，配置角色名、系统提示词、模型提供商、模型 ID、记忆内容、是否人类角色 | P0 |
| F-31 | 通过 UI 编辑已有角色的所有属性 | P0 |
| F-32 | 通过 UI 删除角色 | P0 |
| F-33 | LLM 可在对话中通过工具动态管理角色（list_characters / create_character / edit_character） | P2 |
| F-34 | 新建角色默认使用全局配置的模型提供商和首选模型 | P0 |

### 2.5 会话管理

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-40 | 创建新会话，自动包含一个默认"助手"角色 | P0 |
| F-41 | 列出历史会话（标题、创建时间、角色数、消息数） | P0 |
| F-42 | 打开历史会话，恢复角色定义和消息历史 | P0 |
| F-43 | 删除历史会话 | P0 |
| F-44 | 会话持久化到本地文件系统 | P0 |
| F-45 | 新建会话时关闭当前会话 | P0 |

### 2.6 设置

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-50 | 编辑全局默认模型（从所有 Provider 的模型列表中选择） | P0 |
| F-51 | 编辑模型提供商列表（提供商名、API 终结点、API Key、主模型 ID） | P0 |
| F-52 | 添加/删除模型提供商 | P0 |
| F-53 | 编辑持久化路径、技能文件夹路径、默认最大轮次 | P1 |
| F-54 | 设置变更后保存到配置文件 | P0 |
| F-55 | 设置页返回后，模型切换立即生效，无需重启 | P0 |
| F-56 | 每个提供商下可配置多个模型（模型名、模型 ID、提供商名、是否 Flash） | P1 |

### 2.7 模型管理

| 编号 | 需求 | 优先级 |
|------|------|--------|
| F-60 | 支持多个 LLM 提供商（OpenAI 协议兼容），每个提供商有独立的 API 终结点和密钥 | P0 |
| F-61 | 所有角色共享同一组已注册的模型提供商 | P0 |
| F-62 | 全局首选模型（PrimaryModel）可在运行时切换 | P0 |
| F-63 | 模型能力标记（IsFlash），用于子代理工具选择快速模型 | P2 |
| F-64 | 新建角色必须能正确使用已注册的模型提供商获取 IChatClient | P0 |

---

## 3. 界面需求

### 3.1 主界面布局（三栏式）

```
┌─────────────┬────────────────────┬──────────────┐
│  历史会话    │    聊天消息区       │   角色列表    │
│  (240px)    │    (自适应)        │   (260px)    │
│             │                    │              │
│ [新建]      │  消息气泡列表       │ [+] 添加角色  │
│ 会话列表    │  (角色名+时间+内容)  │ 角色列表      │
│ [打开][删除]│                    │ [编辑][删除]  │
│ [设置]      │  ──────────────    │              │
│             │  输入框 [发送]      │              │
│             │  [停止] [发言中...] │              │
└─────────────┴────────────────────┴──────────────┘
```

- **左栏**：历史会话列表，支持新建/打开/删除会话，底部有"设置"按钮
- **中栏**：聊天消息区（ScrollViewer + ItemsControl），底部输入框和发送/停止按钮
- **右栏**：角色列表，支持添加/编辑/删除角色

### 3.2 设置界面

- 顶部：标题 + 返回按钮
- 全局默认模型：ComboBox 下拉选择（所有 Provider 的所有模型）
- 路径配置：持久化路径、技能文件夹路径、默认最大轮次
- 模型提供商列表：每个提供商可编辑名称、API 终结点、API Key、主模型 ID
- 添加/删除提供商按钮

### 3.3 角色编辑界面

- 顶部：标题 + 取消/保存按钮
- 基本信息：角色 ID（只读）、角色名称、是否人类角色
- 系统提示词：多行文本框
- 模型配置：提供商 ID、模型 ID
- 角色记忆：多行文本框（可选）

### 3.4 视觉风格

- 主题色：蓝色系（Primary #4A90D9），白色/浅灰背景
- 圆角卡片式布局（section 样式）
- 角色头像：彩色圆形 + 角色名首字
- 消息气泡：角色名 + 时间戳 + 内容
- 状态色：成功/警告/错误/信息

---

## 4. 架构设计

### 4.1 分层架构

```
ChatRoomAvaloniaDemo (UI 层)
├── Models/          ← 配置数据模型（AppConfig、ModelProviderConfig、ModelItemConfig）
├── Services/        ← 服务层
│   ├── AppConfigService    ← 配置加载/初始化/路径管理
│   └── ChatRoomService     ← 封装 ChatRoomManager + AgentApiEndpointManager 生命周期
├── ViewModels/      ← MVVM ViewModel
│   ├── MainViewModel          ← 主导航，持有所有子 ViewModel
│   ├── SessionListViewModel   ← 历史会话列表
│   ├── ChatMessagesViewModel  ← 聊天消息、自动循环启停、人类插话
│   ├── RoleListViewModel      ← 角色列表增删改
│   ├── RoleEditViewModel      ← 角色编辑表单
│   └── SettingsViewModel      ← 设置页表单
└── Views/           ← Avalonia XAML 视图
```

### 4.2 关键数据流

#### 4.2.1 启动流程

```
App.OnFrameworkInitializationCompleted
  → AppConfigService.LoadOrInitializeAsync()  ← 加载或创建默认配置
  → new ChatRoomService()
  → chatRoomService.ApplyConfig(appConfig)    ← 注册 Provider + 设置 PrimaryModel
  → new MainViewModel(chatRoomService)
  → mainViewModel.InitializeAsync()
    → SessionListViewModel.LoadSessionsAsync()  ← 加载历史会话列表
    → chatRoomService.CreateNewSession()        ← 创建初始会话 + 默认"助手"角色
    → RoleListViewModel.BindToManager()
    → ChatMessagesViewModel.BindToManager()
```

#### 4.2.2 角色发言流程

```
ChatMessagesViewModel.SendHumanMessage()
  → 停止当前循环
  → ChatRoomService.HumanInterjectAsync(text)  ← 人类消息追加到 ChatRoomSession
  → StartLoopInternalAsync()
    → ChatRoomService.StartAutoLoopAsync()
      → ChatRoomManager.StartAutoLoopAsync()
        循环:
          → SpeakerSelector.SelectNextSpeakerAsync(roles, history, ct)
          → ChatRoomManager.StepAsync(nextSpeaker)
            → BuildIncrementalUserText(role)     ← 取角色上次发言后的增量消息
            → role.SpeakAsync(incrementalText, additionalTools, ct)
              → ChatRoomRole.BuildSystemPrompt()  ← 首次发言时构建人设+记忆
              → ChatManager.SendMessage(request)  ← CopilotChatManager 流式输出
              → 提取 Assistant 回复文本
            → 解析 @mention，填充 MentionedRoleIds
            → AppendMessageAsync(message)         ← 追加到 ChatRoomSession
          → SpeakerSelector 返回 null → 循环结束
```

#### 4.2.3 配置变更流程

```
用户在设置页修改配置
  → SettingsViewModel.ApplyToConfig()      ← 写回 AppConfig
  → ChatRoomService.UpdatePrimaryModel()   ← 同步 PrimaryModel 到 EndpointManager
  → AppConfig.SaveAsync()                  ← 持久化到文件
  → 返回主界面
```

### 4.3 核心依赖关系

```
ChatRoomAvaloniaDemo
  └→ AgentLib.ChatRoom
       ├→ AgentLib
       │    ├→ CopilotChatManager          ← 单人聊天管理（流式输出、工具、历史压缩）
       │    ├→ AgentApiEndpointManager     ← 模型提供商注册和选择
       │    ├→ CopilotToolManager          ← 默认工具（文件系统、子代理）
       │    ├→ SessionTitleGenerator       ← LLM 会话标题生成
       │    └→ FileCopilotChatLogger       ← XML 格式聊天日志
       └→ (无其他外部依赖)
```

### 4.4 持久化文件结构

```
{LocalAppData}/AgentRoundtable/
├── AppConfiguration.json              ← 全局配置
├── sessions/                          ← 会话持久化目录
│   └── {SessionId}/
│       ├── room.config.json           ← 会话配置 + 角色定义 + 消息历史
│       └── public_logs/
│           └── {SessionId}.txt         ← 公开聊天记录（纯文本）
└── skills/                            ← 技能文件夹
```

---

## 5. 已知问题与技术债务

> 以下为当前代码库中已确认存在的问题，是下一轮重写的重点改进方向。

### 5.1 核心缺陷：CopilotChatManager 构造函数 init 时序问题

**问题**：`CopilotChatManager.AgentApiEndpointManager` 是 `{ get; init; } = new()` 属性。构造函数中 `_toolManager = new CopilotToolManager(this.AgentApiEndpointManager)` 和 `_titleGenerator = new SessionTitleGenerator(AgentApiEndpointManager)` 在构造函数体执行时引用的是默认空实例，而 `init` 赋值（对象初始化器）发生在构造函数之后。

**影响**：
- 即使 `ChatRoomRole` 传入了已注册 Provider 的共享 `AgentApiEndpointManager`，`CopilotChatManager` 内部的 `_toolManager.SubAgentTools` 和 `_titleGenerator` 始终持有空的 `AgentApiEndpointManager` 实例
- `SendMessage` 中直接使用 `AgentApiEndpointManager.PrimaryModel`（init 后的共享实例）是正确的，但子代理工具调用和标题生成会因空实例而失败
- 新建角色"没有被注入模型"的根因在此

**影响文件**：`AgentLib/CopilotChatManager.cs`

### 5.2 ChatRoomService.ApplyConfig 的 _isConfigApplied 守卫

**问题**：`ApplyConfig` 方法首次调用后设置 `_isConfigApplied = true`，后续调用直接 return，跳过 Provider 注册和 PrimaryModel 设置。设置页变更 Provider 列表后，新的 Provider 不会被注册到 `_endpointManager`。

**影响**：用户在设置页新增/修改 Provider 后，新 Provider 不会生效。

**影响文件**：`ChatRoomAvaloniaDemo/Services/ChatRoomService.cs`

### 5.3 ChatRoomRoleManagementTools 创建角色时未传入 EndpointManager

**问题**：`ChatRoomRoleManagementTools.CreateCharacter` 中 `new ChatRoomRole(definition)` 未传入共享 `EndpointManager`，导致 LLM 动态创建的角色没有注册模型。

**影响文件**：`AgentLib.ChatRoom/Tools/ChatRoomRoleManagementTools.cs`

### 5.4 ChatRoomManager.LoadAsync 创建角色时未传入 EndpointManager

**问题**：`ChatRoomManager.LoadAsync` 中 `new ChatRoomRole(roleDef)` 未传入共享 `EndpointManager`，从持久化恢复的角色没有注册模型。

**影响文件**：`AgentLib.ChatRoom/ChatRoomManager.cs`

### 5.5 RoleListViewModel.AddRole 诊断日志残留

**问题**：`AddRole` 中遗留了诊断日志代码（`System.Diagnostics.Debug.WriteLine`），应移除。

**影响文件**：`ChatRoomAvaloniaDemo/ViewModels/RoleListViewModel.cs`

### 5.6 SettingsViewModel 缺少 Provider 变更同步

**问题**：设置页修改 Provider 列表后，`ApplyToConfig` 仅写回 `AppConfig`，没有将新 Provider 注册到 `ChatRoomService._endpointManager`。`UpdatePrimaryModel` 只更新首选模型，不处理 Provider 变更。

**影响文件**：`ChatRoomAvaloniaDemo/ViewModels/SettingsViewModel.cs`、`ChatRoomAvaloniaDemo/Services/ChatRoomService.cs`

### 5.7 ChatMessagesViewModel.BindToManager 事件重复订阅

**问题**：每次调用 `BindToManager` 都会 `+=` 订阅 `OnMessageAdded` 和 `OnSpeakingChanged` 事件，但没有取消订阅旧 Manager 的事件。多次绑定会导致事件回调多次执行。

**影响文件**：`ChatRoomAvaloniaDemo/ViewModels/ChatMessagesViewModel.cs`

### 5.8 RoleListViewModel.Roles 属性每次访问创建新集合

**问题**：`Roles => _chatRoomService.ChatRoomManager?.Roles ?? new ObservableCollection<ChatRoomRole>()`，当 `ChatRoomManager` 为 null 时每次返回新实例，UI 绑定不稳定。

**影响文件**：`ChatRoomAvaloniaDemo/ViewModels/RoleListViewModel.cs`

---

## 6. 重写指导建议

### 6.1 AgentLib 基础库改进方向

1. **修复 init 时序问题**：`CopilotChatManager` 应在构造函数中接收 `AgentApiEndpointManager`，而非通过 `init` 属性后置赋值。新增带 `AgentApiEndpointManager` 参数的构造函数重载，确保 `_toolManager` 和 `_titleGenerator` 在构造时就持有正确实例。

2. **AgentApiEndpointManager 重新注册能力**：新增方法支持在 Provider 列表变更后重新注册（清空旧模型列表 + 重新注册所有 Provider + 重设 PrimaryModel）。

### 6.2 AgentLib.ChatRoom 库改进方向

1. **ChatRoomManager 持有共享 EndpointManager**：`ChatRoomManager` 应接收并持有 `AgentApiEndpointManager`，在创建角色（含 LoadAsync 恢复、RoleManagementTools 动态创建）时统一传入。

2. **ChatRoomRoleManagementTools 传入共享实例**：`CreateCharacter` 通过 `ChatRoomManager` 获取共享 `EndpointManager` 传给新角色。

### 6.3 ChatRoomAvaloniaDemo 应用改进方向

1. **ChatRoomService 移除 _isConfigApplied 守卫**：改为每次 `ApplyConfig`（或专门的 `ReapplyConfig` 方法）都重新注册 Provider。或者改为 `ApplyConfig` 接受增量变更。

2. **设置页变更全量同步**：设置页返回时，不仅同步 PrimaryModel，还要同步 Provider 列表（新增的 Provider 注册到 EndpointManager）。

3. **事件订阅管理**：`BindToManager` 先取消旧 Manager 的事件订阅，再订阅新 Manager。

4. **Roles 属性稳定返回**：使用固定的 `ObservableCollection` 实例，而非每次 `?? new()`。

5. **移除诊断日志**：清理 `RoleListViewModel.AddRole` 中的 `Debug.WriteLine`。

---

## 7. 配置数据模型

### 7.1 AppConfig（全局应用配置）

| 属性 | 类型 | 说明 |
|------|------|------|
| `PersistenceBasePath` | `string` | 持久化根目录路径 |
| `SkillFoldersBasePath` | `string` | 技能文件夹基础路径 |
| `DefaultMaxRounds` | `int` | 默认最大对话轮次（默认 10） |
| `DefaultModelProviderName` | `string` | 全局默认模型所属提供商名称 |
| `PrimaryModelId` | `string` | 全局主模型名称 |
| `ConfigFilePath` | `string` | 配置文件路径（不序列化） |
| `Providers` | `ObservableCollection<ModelProviderConfig>` | 模型提供商配置列表 |

### 7.2 ModelProviderConfig（模型提供商配置）

| 属性 | 类型 | 说明 |
|------|------|------|
| `ProviderName` | `string` | 提供商显示名（如 "deepseek"） |
| `ApiEndpoint` | `string` | API 终结点地址 |
| `ApiKey` | `string` | API 密钥 |
| `PrimaryModelId` | `string` | 主模型 ID |
| `Models` | `ObservableCollection<ModelItemConfig>` | 该提供商下的模型列表 |

### 7.3 ModelItemConfig（单个模型配置）

| 属性 | 类型 | 说明 |
|------|------|------|
| `ModelName` | `string` | 模型显示名称 |
| `ModelId` | `string` | 实际传给 API 的模型 ID |
| `Provider` | `string` | 模型供应商名称 |
| `IsFlash` | `bool` | 是否为 Flash（轻量快速）模型 |
| `DisplayText` | `string` | 界面显示文本（计算属性） |

### 7.4 ChatRoomRoleDefinition（角色定义）

| 属性 | 类型 | 说明 |
|------|------|------|
| `RoleId` | `string` | 角色唯一标识 |
| `RoleName` | `string` | 角色显示名 |
| `SystemPrompt` | `string` | 角色人设/系统提示词 |
| `IsHuman` | `bool` | 是否人类角色 |
| `ModelProviderId` | `string?` | 模型提供商 ID |
| `ModelId` | `string?` | 具体模型 ID |
| `SkillFolders` | `List<string>` | 技能文件夹路径列表 |
| `Tools` | `List<ToolDefinition>` | 角色专属工具定义 |
| `MemoryContent` | `string?` | 角色记忆内容 |
| `ParticipationMode` | `ChatRoomParticipationMode` | 参与模式（AlwaysParticipate / MentionOnly） |

### 7.5 ChatRoomMessage（聊天室消息）

| 属性 | 类型 | 说明 |
|------|------|------|
| `MessageId` | `string` | 消息唯一标识 |
| `SenderRoleId` | `string` | 发言角色 ID |
| `SenderRoleName` | `string` | 发言角色显示名 |
| `Content` | `string` | 消息内容（纯文本） |
| `Timestamp` | `DateTimeOffset` | 创建时间戳 |
| `IsHumanMessage` | `bool` | 是否人类发送 |
| `IsSystemMessage` | `bool` | 是否系统消息 |
| `MentionedRoleIds` | `IReadOnlyList<string>` | 被 @ 提及的角色 ID 列表 |

---

## 8. 发言选择策略

### 8.1 RoundRobinSpeakerSelector（默认）

- 按角色注册顺序轮流发言
- 仅 `AlwaysParticipate` 的非人类角色参与自动循环
- 支持 `MaxRounds` 最大轮次限制
- @mention 队列优先：队列为空时才检查 `history[^1]` 的 @ 触发源
- 人类插话后：被 @ 的角色回复完毕即暂停（`_humanInterjectionPending` 标记）
- @ 链式调用：LLM 消息中的 @ 自动入队，形成链式对话

### 8.2 自定义策略

实现 `ISpeakerSelector` 接口即可。接口签名：
```csharp
Task<ChatRoomRole?> SelectNextSpeakerAsync(
    IReadOnlyList<ChatRoomRole> roles,
    IReadOnlyList<ChatRoomMessage> history,
    CancellationToken cancellationToken = default);
```

---

## 9. AgentLib 核心能力（重写时需复用或参考）

### 9.1 AgentApiEndpointManager

- 注册语言模型提供商（`RegisterLanguageModelProvider`）
- 获取所有已注册模型（`GetSupportedModels`）
- 按名称/ID 查找模型（`GetModel`）
- 按能力筛选最佳模型（`GetBestModel`）
- 首选模型属性（`PrimaryModel`，支持自动选择和用户设置）

### 9.2 CopilotChatManager

- 流式消息发送（`SendMessage` / `SendMessageAsync`）
- 工具注册和审批包装（`CopilotToolManager` + `HumanApprovalTool`）
- 历史压缩（`CopilotChatManagerChatReducer` / `CopilotChatManagerToolCallChatReducer`）
- 技能加载（`AddSkillFolder`）
- 会话标题生成（`SessionTitleGenerator`）
- AI 上下文提供者（`AIContextProviders`）

### 9.3 持久化

- `ChatRoomPersistence`：多文件结构（room.config.json + public_logs + 角色私有日志）
- `FileCopilotChatLogger`：XML 格式聊天日志

---

## 10. 默认配置来源

默认配置从 `LindexiAgentConfiguration.LoadDefault()` 加载，包含预置的 API 终结点和密钥。配置文件存储在 `%LocalAppData%/AgentRoundtable/AppConfiguration.json`。
