# ChatRoom CodingAssistantRoleFactory 实施计划

## 背景

`AgentLib.ChatRoom` 当前已经具备普通角色创建、角色模板、工作区文件工具、Roslyn 代码理解工具和 .NET 构建测试工具。默认“助手”角色目前由 `MainViewModel` 直接创建 `ChatRoomRoleDefinition`，角色大厅则通过 `PresetTemplates`、`RoleTemplateService` 和 `ChatRoomService` 创建角色。

编程助手仍然可以使用普通的 `ChatRoomRole` 运行时类型，但它的系统提示词、角色定义、编程工具装配以及工作区资源生命周期明显比普通角色复杂。继续把这些逻辑放在 ViewModel、模板或自动发言循环中，会使宿主层承担过多职责，也难以正确管理 `RoslynAgentTools` 启动的外部 Language Server 进程。

因此，计划新增独立的 `CodingAssistantRoleFactory`，集中创建和配置编程助手。该工厂不引入新的角色继承体系，也不改变现有工作区文件工具的默认行为。

## 已确认的设计约束

1. 编程助手的运行时类型仍然是 `ChatRoomRole`，不新增 `CodingChatRoomRole` 子类。
2. `CodingAssistantRoleFactory` 集中负责：
   - 编程助手系统提示词
   - 参与模式和角色定义
   - 编程工具装配
   - 工作区相关资源的生命周期
3. 现有 `WorkspaceToolProvider` 文件读写工具继续作为设置工作区后的默认工具提供给角色。
4. 不以“编程工具按角色最小授权”为本次设计目标，也不为了创建编程助手而拆除现有默认文件工具机制。
5. Roslyn 代码理解工具和 .NET CLI 工具都依赖工作区路径。未设置有效工作区时，不创建这些工具实例。
6. `RoslynAgentTools` 会启动外部进程，不能在每次角色发言时临时创建，必须随工作区切换和会话关闭正确释放。
7. 角色模板和会话持久化只保存可序列化配置，不保存 `AITool`、Language Server 进程或其他运行时资源。
8. 本次计划优先复用现有能力，不重复实现文件读取、文件写入、Roslyn 查询、构建和测试工具。

## 当前实现分析

### 默认助手创建链路

`MainViewModel.InitializeAsync` 和 `MainViewModel.OnNewSessionCreated` 分别直接创建一个名为“助手”的 `ChatRoomRoleDefinition`，随后调用 `ChatRoomService.AddRoleAsync`。

两处定义内容基本相同：

- 使用新的 GUID 作为 `RoleId`
- `RoleName` 为“助手”
- 使用通用助手系统提示词
- 设置 `IsManagerRole = true`

这条链路适合简单角色，但不适合直接承载编程工具的异步初始化和释放。

### 模板创建链路

预置角色由 `PresetTemplates.GetPresets` 创建，并在首次初始化时由 `RoleTemplateService.EnsurePresetTemplatesAsync` 写入模板目录。用户从角色大厅选择模板后，调用链为：

```text
RoleTemplate
  → RoleTemplateService.ToDefinition
  → ChatRoomService.AddRoleAsync
  → new ChatRoomRole(definition)
  → ChatRoomManager.AddRoleAsync
```

模板适合保存角色名称、提示词、参与模式和模型偏好，但不适合直接保存或创建需要释放的工作区工具资源。

### 当前工具装配链路

`ChatRoomRole` 内部使用 `CopilotChatManager`。当角色设置了 `WorkspacePath` 后，`WorkspaceToolProvider.CreateDefaultTools` 提供目录查询、文件搜索、文件读取、文件创建和精确替换等默认工具。

`ChatRoomAutoLoopRunner` 在每次发言时追加聊天室角色管理工具和设置工作区路径工具。`ChatRoomRole.SpeakAsync` 允许调用方通过 `additionalTools` 追加当前发言使用的工具。

`RoslynAgentTools` 和 `DotNetCliTools` 已经能够通过 `AsAITools` 创建 `AITool` 集合，但目前没有接入角色创建和聊天室生命周期。

### `ChatRoomRoleDefinition.Tools` 的现状

`ChatRoomRoleDefinition.Tools` 当前会参与模板和会话的序列化、复制，但没有被解析为实际的运行时 `AITool`。本计划不依赖该属性完成编程工具装配，避免把工具实例生命周期错误地塞入持久化模型。

## 目标设计

### CodingAssistantRoleFactory 的定位

建议在 `AgentLib.ChatRoom.Services` 下新增公共的 `CodingAssistantRoleFactory`。它是编程助手的创建与工作区能力协调者，而不是新的角色运行时类型。

建议职责如下：

1. 创建编程助手的 `ChatRoomRoleDefinition`
2. 创建并初始化普通 `ChatRoomRole`
3. 根据当前工作区创建 `RoslynAgentTools` 和 `DotNetCliTools`
4. 汇总编程助手需要追加的运行时 `AITool`
5. 在工作区切换时释放旧资源并创建新资源
6. 在聊天室关闭或工厂释放时终止 Language Server 进程
7. 保证工具实例与其创建时的工作区一致

不建议职责如下：

- 不负责模型提供商注册
- 不负责聊天室消息调度
- 不负责会话持久化
- 不重新实现通用文件工具
- 不把 `ChatRoomRole` 替换为专用子类
- 不在工厂内部启动自动发言循环

### 编程助手角色定义

工厂应提供统一方法创建角色定义，避免宿主重复拼装。定义至少包含：

- 新生成的 `RoleId`
- 默认显示名“编程助手”
- 编程工作流系统提示词
- `IsHuman = false`
- 默认 `ParticipationMode = MentionOnly`
- `IsManagerRole = false`
- 可选的 `ModelProviderId` 和 `ModelId`
- 可选的稳定记忆内容

角色名称、模型和参与模式可以通过选项对象覆盖，但系统提示词的主体应由工厂集中维护。

### 系统提示词结构

编程助手系统提示词建议保持稳定工作方式，不写入当前项目的临时知识。内容应包含以下部分。

#### 身份与职责

- 负责完成用户明确提出的编程任务
- 不主动扩大修改范围
- 不修改与任务无关的代码
- 不把讨论或建议误报为已经完成的修改

#### 探索规则

- 先确认并读取工作区
- 不猜测项目和文件路径
- 先了解解决方案、项目和相关文件结构
- 查询代码符号时优先使用 Roslyn 工具
- 修改文件前必须读取相关内容
- 读取并遵守仓库中的编码指令与约定

#### 修改规则

- 使用最小修改完成目标
- 优先使用已有库和项目模式
- 不编辑生成目录和生成文件
- 不覆盖工作区外文件
- 不通过禁用检查或删除测试隐藏问题

#### 验证规则

- 修改后运行相关构建或测试
- 构建失败时读取完整日志的相关部分
- 区分本次修改引入的问题和已有问题
- 无法验证时明确报告阻塞原因和未验证范围

#### 汇报规则

- 汇报修改文件和主要行为变化
- 汇报实际执行的构建与测试结果
- 明确指出仍存在的风险或后续工作

## 工作区工具会话

### 建议增加 CodingWorkspaceToolSession

为避免让 `CodingAssistantRoleFactory` 同时承担过多底层状态管理，建议新增内部或公共的异步可释放类型，例如 `CodingWorkspaceToolSession`。

该类型按单个工作区创建并持有：

- 规范化后的工作区绝对路径
- 一个 `RoslynAgentTools` 实例
- 一个 `DotNetCliTools` 实例
- 已创建的 Roslyn `AITool` 集合
- 已创建的 .NET CLI `AITool` 集合

`DotNetCliTools` 本身不需要释放，但应和 Roslyn 工具一起绑定到同一个工作区会话，避免切换工作区后继续使用旧路径。

### 创建流程

```text
确认工作区路径
  → 规范化为绝对路径
  → 验证目录存在
  → 创建 RoslynAgentTools
  → 创建 DotNetCliTools
  → 缓存两组 AITool
  → 返回 CodingWorkspaceToolSession
```

如果 Roslyn Language Server 创建失败，应释放已经创建的资源并向调用方返回明确异常。是否允许仅使用 .NET CLI 工具降级运行，应由工厂 API 明确表达，不应静默忽略失败。

### 工作区切换流程

```text
收到新工作区路径
  → 与当前规范化路径比较
  → 相同则复用现有会话
  → 不同则先创建新会话
  → 新会话创建成功后替换当前会话
  → 异步释放旧会话
```

优先先创建新会话再替换，避免新工作区初始化失败后丢失仍可用的旧会话。切换过程需要串行化，防止并发创建多个 Language Server 进程。

### 释放流程

以下场景必须调用异步释放：

- 切换工作区
- 关闭当前聊天室会话
- 应用退出
- `CodingAssistantRoleFactory` 不再使用
- 初始化过程失败

释放操作应具备幂等性，重复调用不能重复终止或抛出无意义异常。

## 编程工具装配

### 保留默认工作区文件工具

`WorkspaceToolProvider.CreateDefaultTools` 的现有行为保持不变。设置 `ChatRoomManager.WorkspacePath` 后，角色继续通过内部 `CopilotChatManager` 获得默认文件系统工具。

编程助手不需要在 `CodingAssistantRoleFactory` 中重复创建以下工具：

- 目录枚举
- 文件名查询
- 文本或正则搜索
- 按行读取文件
- 创建或写入文件
- 唯一字符串替换
- 多字符串替换

工厂只负责追加目前尚未进入默认工具集合的编程工具：

- `RoslynAgentTools.AsAITools()`
- `DotNetCliTools.AsAITools()`

这些工具同样基于当前工作区工作，与默认文件工具共同组成编程助手的完整工作区能力。

### 角色专属运行时工具入口

建议为 `ChatRoomRole` 增加只存在于运行时的附加工具集合，例如只读属性和内部设置方法。该集合不属于 `ChatRoomRoleDefinition`，也不参与 JSON 持久化。

每次发言的最终工具集合由以下部分组成：

```text
CopilotChatManager 默认工具
  + ChatRoomRole 运行时附加工具
  + ChatRoomAutoLoopRunner 本轮附加的聊天室工具
```

合并时应按工具名称去重，避免相同工具被多次加入。若出现同名不同实例，应采用明确且可测试的优先级，而不是依赖列表顺序产生隐式覆盖。

### 工作区变化后的工具更新

编程助手创建时可能尚未设置工作区，因此工厂必须支持以下状态：

1. 已创建角色，但没有 Roslyn 和 .NET CLI 工具
2. 工作区设置成功后，为角色绑定编程工具
3. 工作区切换后，用新会话工具替换旧工具
4. 工作区清空后，移除依赖工作区的编程工具并释放会话

默认文件工具仍由 `ChatRoomManager.SetWorkspacePath` 传播路径；工厂负责同步更新编程助手的 Roslyn 和 .NET CLI 工具。

## 与 ChatRoomService 的集成

### 创建编程助手

建议 `ChatRoomService` 接收或创建一个 `CodingAssistantRoleFactory`，并提供明确的应用服务方法，例如：

- 创建编程助手定义
- 将编程助手添加到当前会话
- 更新当前编程工作区

`ChatRoomService` 继续负责：

- 设置 `MainThreadDispatcher`
- 将角色加入 `ChatRoomManager`
- 注册和选择模型提供商
- 执行角色模型可用性检查

工厂负责返回已经完成编程角色配置的普通 `ChatRoomRole`，或者提供定义与运行时工具装配所需的组合结果。

### 会话加载

历史会话恢复时，持久化数据只能恢复 `ChatRoomRoleDefinition`。如果需要识别某个角色是否是编程助手，应增加稳定、可序列化的角色种类或能力标识，而不是根据中文角色名称、模板 ID 或系统提示词内容判断。

建议在 `ChatRoomRoleDefinition` 增加稳定的角色类型标识，例如：

```text
General
CodingAssistant
```

该标识只描述角色创建和运行时装配策略，不保存工具实例。加载角色后，`ChatRoomService` 根据标识调用 `CodingAssistantRoleFactory` 重新绑定当前工作区工具。

若不希望立即扩展角色定义，也可以第一阶段只支持新创建的编程助手，不支持从持久化会话自动恢复工具。但该限制必须在 API 和文档中明确，不能依赖角色名称猜测。

### 会话关闭

当前 `ChatRoomService.CloseCurrentSession` 是同步方法，而 Roslyn 工具需要异步释放。建议将关闭流程扩展为异步方法，并让以下调用等待资源释放：

- 新建会话前关闭旧会话
- 加载历史会话前关闭旧会话
- 应用退出

如果需要保留同步入口，应只作为明确的兼容包装，并确保不会遗漏异步释放异常。

## 与工作区路径设置的集成

当前工作区路径通过 `WorkspacePathTools` 调用 `ChatRoomManager.SetWorkspacePath` 设置。为了让工厂及时创建或切换编程工具会话，需要增加工作区变化通知或将设置入口提升到应用服务。

推荐方案是让 `ChatRoomManager` 在工作区发生实际变化时发出事件，事件至少包含：

- 旧工作区路径
- 新工作区路径

`ChatRoomService` 订阅事件并异步通知 `CodingAssistantRoleFactory`。由于现有事件模型是同步的，异步初始化失败需要通过任务跟踪、状态属性或错误事件反馈，不能使用无法等待的 `async void` 隐藏异常。

另一种方案是让 `WorkspacePathTools` 调用一个支持异步工作区切换的协调服务，但这会增加工具对应用服务的依赖。实施前应优先选择事件与显式异步 API结合的方式，保持 `ChatRoomManager` 的职责边界。

## 与角色模板的集成

### 编程助手运行时内置模板

编程助手不写入 `PresetTemplates`，而是在每次进程启动时由 `CodingAssistantRoleFactory` 创建并注册到 `RoleTemplateService` 的运行时模板覆盖层。模板包含：

- 显示名称
- 描述和分类
- 稳定角色类型标识
- 系统提示词或由工厂识别的配置
- 默认参与模式
- 默认模型偏好

运行时模板不保存：

- 工作区绝对路径
- `AITool` 实例
- Roslyn Language Server 命令进程
- 工具日志和文件快照

### 固定 TemplateId 与磁盘模板替换

编程助手使用固定 `TemplateId`。加载角色大厅时按以下规则处理：

1. 正常加载磁盘模板
2. 从内存结果中排除固定 ID 的磁盘编程助手
3. 添加本次进程创建的运行时编程助手
4. 不删除或覆盖对应磁盘文件
5. 用户本次进程删除运行时项后不立即补回，下次进程启动重新注入
6. 用户本次进程可以编辑运行时项，但保存只更新内存，不写回磁盘

普通预置模板初始化同时改为按 `TemplateId` 幂等补齐：

1. 读取已有模板 ID
2. 枚举内置预置模板
3. 只写入缺失项
4. 不覆盖用户已经修改或保存的同 ID 文件

是否允许内置模板升级覆盖旧版本，应另行设计版本字段。本次只补齐缺失模板。

## 建议新增类型

### CodingAssistantRoleFactory

建议公开，供宿主应用创建编程助手。

主要成员方向：

- 创建角色定义
- 创建并初始化角色
- 设置或切换工作区
- 为编程助手绑定当前工具
- 移除角色绑定
- 异步释放

### CodingAssistantRoleOptions

用于承载可覆盖配置，避免工厂方法参数持续增加。

建议包含：

- 角色显示名
- 模型提供商 ID
- 模型 ID
- 参与模式
- 是否为管理者
- Roslyn Language Server 命令
- Roslyn 初始化失败策略

所有公共 API 均需要 XML 文档注释，并保持可空安全。

### CodingWorkspaceToolSession

负责单个工作区的工具实例和异步释放。可以先作为 `internal sealed` 类型，避免过早暴露底层生命周期 API。

### ChatRoomRoleKind

若需要支持持久化恢复，建议增加稳定枚举或字符串标识。枚举名称应表达角色装配策略，不表达临时 UI 状态。

## 实施步骤

### 第一阶段：角色规格与工厂骨架

1. 增加 `CodingAssistantRoleOptions`
2. 增加稳定的编程助手系统提示词生成逻辑
3. 增加 `CodingAssistantRoleFactory`
4. 由工厂创建普通 `ChatRoomRoleDefinition` 和 `ChatRoomRole`
5. 保持默认参与模式为 `MentionOnly`
6. 为工厂和选项增加中文 XML 文档注释

### 第二阶段：工作区工具会话

1. 增加 `CodingWorkspaceToolSession`
2. 按工作区创建 `RoslynAgentTools`
3. 按同一工作区创建 `DotNetCliTools`
4. 缓存两者的 `AITool` 集合
5. 实现并发安全的工作区切换
6. 实现幂等的 `IAsyncDisposable`
7. 确保初始化失败时释放已创建资源

### 第三阶段：角色运行时工具装配

1. 为 `ChatRoomRole` 增加不参与持久化的运行时附加工具
2. 在发言时合并默认工具、角色附加工具和本轮附加工具
3. 按工具名称去重并明确冲突优先级
4. 工厂在工作区建立后绑定 Roslyn 和 .NET CLI 工具
5. 工作区切换时替换编程工具
6. 工作区清空时移除编程工具
7. 保持 `WorkspaceToolProvider` 的默认文件读写工具行为不变

### 第四阶段：应用服务和生命周期接入

1. 将 `CodingAssistantRoleFactory` 接入 `ChatRoomService`
2. 增加创建编程助手的应用服务入口
3. 在设置工作区后通知工厂创建或切换工具会话
4. 在加载会话后为编程助手重新绑定工具
5. 将关闭会话流程调整为可等待异步资源释放
6. 在应用退出时释放工厂
7. 避免通过 `async void` 承担资源切换和异常处理

### 第五阶段：模板和宿主接入

1. 增加固定 `TemplateId` 的编程助手运行时内置模板
2. 增加稳定的角色类型或能力标识
3. 运行时模板遮蔽磁盘中的同 ID 模板，但不删除磁盘文件
4. 运行时模板编辑和删除只影响当前进程
5. 将普通预置模板初始化改为按模板 ID 补齐
6. 从角色大厅添加编程助手时使用工厂完成运行时装配
7. 保持“默认创建普通助手”与“显式添加编程助手”为独立宿主策略

### 第六阶段：测试与验证

1. 测试工厂创建的角色名称、提示词和参与模式
2. 测试模型配置能够从选项传入角色定义
3. 测试未设置工作区时不创建工作区编程工具
4. 测试设置工作区后包含 Roslyn 和 .NET CLI 工具
5. 测试默认文件读写工具行为不受影响
6. 测试重复设置相同工作区时复用工具会话
7. 测试切换工作区时释放旧 Roslyn 会话
8. 测试并发切换不会泄漏多个 Language Server 进程
9. 测试会话加载后能够根据稳定标识重新装配编程工具
10. 测试关闭会话和应用退出时完成异步释放
11. 运行 `AgentLib.ChatRoom.Tests`
12. 运行 `ChatRoom.Shell.Tests`
13. 构建 `ChatRoom.slnx`

## 重点测试场景

### 创建角色但尚未设置工作区

预期结果：

- 编程助手可以加入聊天室
- 角色定义和系统提示词完整
- 不启动 Roslyn Language Server
- 不创建 `DotNetCliTools`
- 默认文件工具因没有工作区而返回明确的未配置提示

### 设置有效工作区

预期结果：

- `ChatRoomManager` 将路径传播给角色的默认文件工具
- 工厂创建 Roslyn 和 .NET CLI 工具会话
- 编程助手下一次发言能够使用代码理解、构建和测试工具
- 所有工具使用同一个规范化工作区路径

### 切换工作区

预期结果：

- 新工具只访问新工作区
- 旧 Roslyn Language Server 被释放
- 旧 `DotNetCliTools` 不再被角色调用
- 新工作区初始化失败时不会留下半初始化资源

### 恢复历史会话

预期结果：

- 角色定义正常恢复
- 不尝试反序列化工具实例
- 根据稳定角色标识重新绑定当前工作区工具
- 未设置工作区时保持无编程工具会话状态

### 关闭会话

预期结果：

- 自动循环停止
- 编程工具会话被异步释放
- Language Server 进程退出
- 后续新会话不会复用旧工作区实例

## 风险与待确认项

### Roslyn 初始化失败策略

需要确定工厂默认采用以下哪种行为：

1. 创建编程助手失败并向用户报告
2. 创建角色成功，但仅提供默认文件工具和 .NET CLI 工具，同时明确 Roslyn 不可用

建议默认采用第二种方式，以便 Language Server 不可用时仍可执行基于文件搜索的有限编程任务，但必须通过状态和消息明确暴露降级结果，不能静默忽略。

### 角色识别方式

如果需要支持历史会话恢复，应增加稳定角色类型标识。根据角色名称“编程助手”判断不可靠，根据完整系统提示词判断更不可维护。

### 工厂与聊天室的对应关系

一个工厂实例应服务一个当前工作区和一组由它创建或绑定的编程助手。若未来允许多个聊天室同时使用不同工作区，应为每个聊天室创建独立工厂，或将工作区会话缓存提升为按规范化路径索引的服务。本阶段建议一个 `ChatRoomService` 对应一个工厂实例。

### 多个编程助手共享工具

同一聊天室、同一工作区中的多个编程助手可以共享同一个 `RoslynAgentTools` 和 `DotNetCliTools` 实例。共享能够避免重复启动 Language Server，但工具返回的构建日志属于共享状态，多个角色并发运行构建或测试时可能互相覆盖最后日志。

本阶段应限制同一工作区编程命令串行执行，或者明确 `DotNetCliTools` 的最后日志是工作区会话级状态。后续若支持并发编程角色，再引入按调用标识保存日志。

### 异步关闭 API 兼容性

将同步 `CloseCurrentSession` 改为异步会影响 ViewModel 和测试调用点。实施时可以先增加 `CloseCurrentSessionAsync`，迁移内部调用后再评估是否保留同步方法，避免通过同步阻塞等待异步释放。

## 验收标准

1. 编程助手由 `CodingAssistantRoleFactory` 创建，宿主不再手工拼装其完整提示词和工具。
2. 编程助手仍是普通 `ChatRoomRole`，没有新增角色继承体系。
3. 未设置工作区时不启动 Roslyn Language Server。
4. 设置工作区后，编程助手同时具备默认文件工具、Roslyn 工具和 .NET CLI 工具。
5. 现有默认文件读写工具行为没有因编程助手接入而被移除或按角色限制。
6. Roslyn 和 .NET CLI 工具始终使用与当前聊天室一致的工作区。
7. 切换工作区后旧工具不再可调用，旧 Language Server 被释放。
8. 关闭会话或应用退出后不存在遗留的 Roslyn Language Server 进程。
9. 模板和会话 JSON 中不包含运行时工具实例或本地工作区绝对路径。
10. 历史会话中的编程助手可以通过稳定角色标识重新装配工具。
11. 默认助手与编程助手的创建策略彼此独立，不强制用编程助手替代普通助手。
12. 相关单元测试通过，并且 `ChatRoom.slnx` 构建成功。

## 实施结果

本计划已按确认后的运行时角色大厅规则完成实现：

- 新增 `CodingAssistantRoleFactory`、`CodingAssistantRoleOptions` 和内部 `CodingWorkspaceToolSession`
- 新增 `ChatRoomRoleKind.CodingAssistant`，用于会话持久化和恢复后的工具重新装配
- 编程助手使用固定 `runtime_coding_assistant` 模板 ID，在进程启动时注册到角色大厅内存覆盖层
- 同 ID 的磁盘模板只在内存加载结果中被替换，不删除磁盘文件
- 运行时模板支持本次进程编辑和删除，编辑不持久化，删除后本次进程不补回
- `ChatRoomRole` 按“本轮工具 > 运行时工具 > 默认工具”合并并按名称去重
- 工作区切换采用串行的先创建后替换流程，Roslyn 初始化失败时默认保留 .NET CLI 工具并暴露失败状态
- 会话关闭会等待自动循环停止，再解除角色绑定并异步释放工作区工具
- 应用退出时释放 `ChatRoomService` 和工厂资源
- 相关测试项目通过；并行运行中一个会启动 `dotnet build` 的既有测试曾因资源竞争失败，单独重跑通过；`ChatRoom.slnx` 构建成功

## 结论

`CodingAssistantRoleFactory` 应作为编程助手复杂创建逻辑的集中入口，负责角色规格、编程工具装配和工作区资源生命周期。编程助手本身仍使用普通 `ChatRoomRole`，角色模板和持久化模型只保存稳定配置。

现有工作区文件读写工具继续保持默认工具行为。Roslyn 和 .NET CLI 工具在此基础上由工厂按当前工作区创建和追加，共同构成编程助手完成“探索代码、修改文件、构建测试”的工作闭环。实现重点不是限制普通角色的工作区工具，而是保证编程助手的额外工具始终与当前工作区一致，并能够被可靠创建、切换和释放。
