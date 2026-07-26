# ChatRoom 子代理角色方案一探索过程：独立 InvocationMode

本文件保留推荐方案的完整调查日志。日常审核优先阅读[需求功能](../ChatRoom-子代理角色需求功能.md)和[候选方案汇总](../候选方案/ChatRoom-子代理角色候选方案汇总.md)，仅在核对调查依据时按章节读取本文档。

## 1. 文档目的

本文档用于持续记录“聊天室子代理角色”方案探索过程，保存代码事实、调用链、设计推论、风险与待确认项，避免因项目规模和上下文限制丢失关键结论。

最终交付将另行形成可执行的完整方案文档；本文档保留调查依据和决策演进过程。

## 2. 原始需求拆解

目标是在现有聊天室角色体系中增加“子代理角色”，并满足以下约束：

1. 子代理角色不是普通参与讨论的 AI 角色。
2. 非人类角色不能通过普通 `@角色名` 方式调用子代理角色。
3. 子代理角色主要通过专用子代理工具被其他角色调用。
4. 用户可以直接调用子代理角色，但消息必须严格以 `@子代理角色名 ` 开头。
5. 固定前缀中的角色名后必须有一个空格；仅出现在消息中段的 `@子代理角色名 ` 不生效。
6. 例如 `Xxxx, xxx。 @子代理角色名 xx` 不应触发子代理调用。
7. 子代理执行产生的内容不进入整个聊天室后续上下文，消息应设置 `IsPresetInfo = true`。
8. 子代理不能把普通聊天回复作为最终结果；必须通过工具把结果返回给调用它的角色，行为与当前子代理工具保持一致。

## 3. 当前理解

### 3.1 需要区分的概念

- **角色定义**：持久化和配置层声明某个角色是否属于子代理角色。
- **调用权限**：用户、普通 AI 角色、管理者角色分别能以何种入口调用子代理。
- **触发语法**：用户输入只接受严格消息开头的 `@子代理角色名 `。
- **执行协议**：子代理运行后必须经工具返回结构化结果，不直接向聊天室广播普通回复。
- **上下文隔离**：子代理调用请求、执行过程和返回内容是否显示、如何持久化、如何从后续模型上下文过滤。
- **调度隔离**：子代理角色不应进入自动发言、普通提及发言、管理者点名等常规调度候选集合。

### 3.2 初步设计假设

以下内容只是调查起点，必须以代码事实校正：

- 角色定义可能需要新增显式角色类型或布尔标记，而不能仅依赖名称或提示词判断。
- 严格前缀解析应独立于现有通用 MentionParser，或在其上增加调用模式，避免改变普通 `@` 语义。
- 用户直接调用与角色通过工具调用应汇聚到同一“子代理执行入口”，只在调用方身份和结果投递目标上不同。
- `IsPresetInfo = true` 应由统一消息创建位置设置，不能依赖 UI 或持久化层事后修补。
- 子代理执行器需要识别“必须调用返回工具”的完成条件，并在模型未调用工具时视为协议失败，而不是接受文本输出。

## 4. 探索范围

### 4.1 核心领域与调度

- `AgentLib.ChatRoom/Domain/ChatRoomRoleDefinition.cs`
- `AgentLib.ChatRoom/Domain/ChatRoomMessage.cs`
- `AgentLib.ChatRoom/Domain/ChatRoomEnums.cs`
- `AgentLib.ChatRoom/ChatRoomManager.cs`
- `AgentLib.ChatRoom/ChatRoomManager.ChatRoomAutoLoopRunner.cs`
- `AgentLib.ChatRoom/Coordination/ChatRoomCoordinator.cs`
- `AgentLib.ChatRoom/MentionParser.cs`

### 4.2 角色执行与上下文

- `AgentLib.ChatRoom/ChatRoomRole.cs`
- `AgentLib.ChatRoom/ChatRoomRoleExecutionContext.cs`
- `AgentLib.ChatRoom/IChatRoomRoleExecutor.cs`
- `AgentLib.ChatRoom/StandardChatRoomRoleExecutor.cs`
- `AgentLib.ChatRoom/CodingChatRoomRoleExecutor.cs`
- `AgentLib.ChatRoom/Runtime/*`

### 4.3 工具与子代理现状

- `AgentLib.ChatRoom/Tools/*`
- 通用 `AgentLib` 项目中的 Agent、工具与子代理实现
- `AgentLib.Coding` 中的工具提供、工具会话和运行结果
- 代码库内包含 `subagent`、`sub-agent`、`子代理`、`SubAgent`、`delegate` 等概念的位置

### 4.4 配置、持久化与 UI

- 角色模板和角色管理服务
- 快照、存储模型及映射
- Avalonia 聊天室的角色配置、消息展示和输入提交逻辑
- 现有单元测试与集成测试

## 5. 重点调查问题

1. 现有角色类型、角色身份与发言权限如何表达？
2. 普通 `@角色名` 在何处解析，是否区分发送者身份和消息位置？
3. 用户消息提交后，直接点名、自动发言和管理者调度分别如何选择目标角色？
4. `IsPresetInfo` 在领域消息、兼容模型、序列化、UI 和上下文构造中的真实含义是什么？
5. 当前“子代理工具”位于何处，如何创建子代理、限制最终输出并把结果返回调用方？
6. 工具调用结果是否会被写入聊天室消息历史，还是只留在单次模型运行上下文？
7. 角色定义如何经过模板、服务、快照和恢复链路传播？
8. UI 是否允许配置角色类别，是否需要对子代理角色使用不同的视觉与交互入口？
9. 已有测试的命名、超时和场景构造方式是什么？
10. 老版本持久化数据缺少新字段时应采用何种默认值？

## 6. 已确认的工作区事实

- 当前解决方案包含 `AgentLib.ChatRoom`、`AgentLib.ChatRoom.Tests`、`CodingChatRoom.AvaloniaShell`、`AgentLib.Coding` 和 `Docs` 等相关项目。
- 聊天室核心已经分为领域模型、协调器、运行时、持久化、服务和工具等层次。
- 现有文档目录中已有 Mention、角色管理、自动发言、状态恢复、编程角色等多份设计文档，应优先复用其中已经稳定的术语和边界。
- 本次任务只输出过程文档和最终方案文档，不实施产品代码修改。

## 7. 调查日志

### 初始化

- 已建立需求拆解、探索范围与问题清单。
- 已读取 Mention 重构、MentionOnly 上下文隔离、角色管理工具、自动发言调度、状态所有权重构、CodingAgent 分流与早期多角色方案。

### 既有文档风格与约束

现有较新的方案文档通常采用以下结构：

1. 文档定位、背景和明确边界。
2. 当前实现事实，并给出文件、类型或调用链证据。
3. 目标行为和不变量。
4. 方案比较与核心决策。
5. 分层设计：领域、协调、运行时、持久化、UI。
6. 失败、取消、恢复、并发和兼容性边界。
7. 测试矩阵、分步实施计划和验收标准。

最终方案应沿用该结构，不只罗列待修改文件。

已提炼的可复用约束如下：

- `RoleId` 是稳定身份；角色名只用于显示和输入匹配，不能作为持久化关联或执行分流主判据。
- 参与时机、输入可见范围、执行引擎和管理者身份是相互独立的概念，不能把子代理语义塞入 `ParticipationMode`、`ExecutionKind` 或 `IsManagerRole` 的既有含义。
- 新能力应使用显式、可持久化、可验证的角色定义字段，禁止通过角色名、提示词或工具集合推断。
- 普通 mention 已经是结构化 `MentionedRoleIds`；历史消息一旦结构化，不应因角色改名或新增角色而重新解释。
- 调度项如果依赖某条触发消息，必须携带稳定的消息对象、消息 ID 或 `MessageSequence`，不能在执行阶段扫描历史猜测触发源。
- 新 Domain/Coordinator 架构以不可变 `ChatRoomState`、单写者命令协调器、execution lease、隔离 runtime 和 committed checkpoint 为目标边界。
- 调度和输入选择属于 Coordinator；Runtime 只消费已批准的输入，不应再次扫描完整聊天室历史决定可见范围。
- 工具、推理、子代理和审批详情在新架构中倾向于作为瞬态 execution 事件，不应自动成为公开聊天室恢复事实。
- Standard 与 Coding 执行器能力不同。当前设计中 ChatRoom 协调工具只注入 Standard 路径，CodingAgent 不接收 ChatRoom 专用工具；子代理工具若要求所有非人类角色可调用，必须显式解决该差异，不能假设统一工具注入已经存在。
- 角色私有 AgentSession 与聊天室公开上下文分离。隐藏公开消息不等于清空角色自己的私有记忆。
- 角色策略字段更新原则上不应替换 Runtime、清空 checkpoint 或重置消费水位；执行引擎和模型配置变化才属于运行时事实。
- 旧 JSON 缺少新增字段时必须有明确默认值；新 snapshot schema 必须提供旧版本兼容读取，不能只提升版本后拒绝所有旧数据。
- UI 应把公开消息与内部执行详情分开投影；`IsPresetInfo` 是否显示必须独立确认，不能仅凭名称推断。

从 Visual Studio Copilot 子智能体行为分析文档可复用的语义：

- 子智能体调用是同步等待式委派，不是后台任务。
- 每次调用无状态，调用任务必须自包含。
- 子智能体只返回一次最终报告。
- 返回报告不自动面向最终用户广播，而由调用者整合。
- 权限应按代理用途受限，工具 schema 同时承担接口和行为边界。

下一步读取当前代码，确认上述历史设计中哪些已经落地，以及 `IsPresetInfo` 和 mention 调度的真实行为。

### 当前角色、消息与调度代码事实

#### 两套架构同时存在

- legacy 生产模型位于 `Model/ChatRoomRoleDefinition.cs`、`Model/ChatRoomMessage.cs`、`ChatRoomManager.cs` 和 `ChatRoomManager.ChatRoomAutoLoopRunner.cs`。
- 新架构位于 `Domain/*`、`Coordination/ChatRoomCoordinator.cs`、`Runtime/*` 和 `Persistence/*`。
- 编译器引用显示 `ChatRoomCoordinator` 当前主要由架构测试直接使用，legacy `ChatRoomManager` 仍是现有应用服务和 UI 的主要运行入口。
- 最终方案必须给出两条链路的目标改造，并明确先后次序，不能只设计其中一套。

#### 角色定义当前没有“子代理”维度

- legacy 和 Domain 角色定义都只有：稳定身份、`ExecutionKind`、`IsHuman`、`ParticipationMode`、`IsManagerRole`、模型、人设、记忆和技能。
- `ExecutionKind` 只决定 Standard/Coding 运行引擎。
- `ParticipationMode` 只决定 AlwaysParticipate/MentionOnly 参与时机。
- `IsManagerRole` 只决定兜底和仲裁身份。
- 因此子代理必须新增独立、显式、可持久化的角色调用类别；复用 MentionOnly 会错误允许普通 AI 通过 `@` 调用，复用 Coding 会混淆执行引擎，复用 Manager 会混淆调度职责。
- Domain 构造函数和 `ChatRoomState.ValidateRoles` 已集中校验枚举、身份和角色名，适合增加子代理不变量；legacy 需要在角色添加、更新、工具创建和 UI 保存处补齐同等校验。

#### legacy 普通 mention 解析行为

- `MentionParser` 使用正则 `@\[名称\]` 或 `@非空白文本`，扫描整条消息，不要求出现在开头。
- 名称匹配不区分大小写，同名以角色注册顺序中的第一个为准，同一 RoleId 去重并保留首次出现顺序。
- 常规 `@角色名` 必须以空白或消息结尾终止；角色名含空格时只能使用方括号格式。
- 解析器接收完整角色集合，不区分人类发送者与 AI 发送者，也不区分目标角色类别。
- `HumanInterjectAsync` 会对人类消息立即解析并写入 `MentionedRoleIds`。
- AI 消息在 `HandleAutoLoopMessageAsync` 中同样解析并写入 `MentionedRoleIds`。
- 因此在当前实现中，任意消息中段出现合法 `@角色名 ` 都可能触发目标角色，和子代理的严格首部语法相冲突。

#### legacy 自动调度与直接执行

- 人类触发消息含任意 mention 时，只把 mention 目标放入优先栈；没有 mention 时才把全部 `AlwaysParticipate` 非人类角色加入默认队列。
- 后续 AI 消息中的 mention 会被放入优先栈，优先于剩余默认角色执行。
- `PushMentionedRoles` 只排除人类角色；任何新子代理角色若不增加过滤，都会被普通 AI 的 `@` 加入优先栈。
- `GetManagerRole` 只检查“非人类 + 管理者”；若错误配置为子代理管理者，也可能作为兜底直接执行。
- `ChatRoomManager.StepAsync(ChatRoomRole)` 是公开入口，只排除人类角色；新增子代理后，外部仍可绕过调度直接把它当普通发言者执行。
- legacy 执行会先把角色设置为 `CurrentSpeaker`，构造自上次发言后的全部增量公开消息，注入角色管理和工作区工具，再调用 `ChatRoomRole.SpeakAsync`。
- 成功的 AI 内容会作为公开 `ChatRoomMessage` 加入 `Session.Messages`，随后参与其他角色的增量上下文；当前没有按消息类别过滤的入口。
- `BuildChatRoomContext` 会向所有 Standard 角色列出全部角色，并统一说明可以通过 `@角色名` 协作；若不修改，模型会被明确鼓励通过普通 mention 调用子代理。

#### 新 Coordinator 的 mention 与自动调度

- `AppendHumanMessageCommand` 提交不可变人类消息时调用 Coordinator 私有 `ParseMentions`，把结果写入 `MentionedRoleIds`。
- Coordinator 私有解析器并未复用 legacy 正则，而是对每个角色执行不区分大小写的 `IndexOf("@" + RoleName)`；它不要求空格、词边界、消息开头或方括号格式，语义比 legacy 更宽松。
- `StartAutoLoopCommand` 从最后消息的结构化 mention 入队，然后再加入所有 AlwaysParticipate 角色。
- 自动循环运行期间追加人类消息时，会同时调用 `EnqueueMentionedRoles` 和 `EnqueueDefaultRoles`；这与 legacy“有 mention 则不进入默认队列”并不完全一致。
- `_autoLoopQueue` 当前只保存 RoleId，不保存触发消息、调用来源或返回目标。
- `DequeueNextAutoLoopRole` 只排除未知、人类、连续发言和超轮次角色；新增子代理若不增加过滤，仍会被普通 mention 或默认队列执行。
- 管理者兜底同样只检查“非人类 + 管理者”。
- `StartRoleExecutionCommand` 只携带 RoleId，不能表达“普通公开发言”还是“子代理调用”、调用者是谁、调用输入是什么、结果返回给谁。
- `StartExecutionCore` 默认把角色消费水位之后的全部公开消息作为 `InputMessages`，并在完成时把公开内容追加为普通 Assistant 消息；不满足无状态、自包含任务和非公开回传要求。
- 新架构已有房间级单活跃 execution、稳定 `ExecutionId`、runtime lease、candidate checkpoint、瞬态 execution event 和精确审批路由，可作为子代理调用生命周期的主要基础。

#### 对严格用户前缀语法的直接结论

- 子代理调用不能通过修改现有 `ParseMentions` 的一个正则分支完成，因为普通 mention 仍需要支持消息中段和 AI 链式调用。
- 需要独立的“用户子代理调用解析器”，只在发送者是人类时运行，并严格匹配消息索引 0。
- 建议首版语法只接受 `@角色名 `，其中分隔符是一个 ASCII 空格；不接受前导空白、消息中段、`@[角色名]`、Tab、换行或只有 `@角色名` 无任务内容。
- 解析成功后应产生独立的结构化调用元数据，而不是把子代理 RoleId 写入普通 `MentionedRoleIds`。
- 人类消息提交入口必须在普通自动循环分流之前识别该调用，防止“排除子代理 mention 后反而启动所有默认角色”。
- 普通 mention 解析必须显式排除子代理目标；自动队列、管理者兜底和公开 `StepAsync`/`StartRoleExecutionCommand` 还要再次验证调用类别，形成入口和执行层双重保护。

下一步调查 `AgentLib` 中当前子代理工具和 `IsPresetInfo` 的真实语义，决定专用调用命令、返回工具与消息隔离应如何落地。

### 当前子代理工具、工具回传与上下文事实

#### `SubAgentToolProvider` 当前行为

- 默认工具管理器会给普通 `CopilotChatManager` 注入工作区工具和 `InvokeSubAgent`。
- `InvokeSubAgent` 按能力选择模型，创建一个新的 `ChatClientAgent`，不传 `AgentSession`，因此每次调用原则上是独立、无状态的。
- 子代理获得工作区工具、递归 `InvokeSubAgent` 和 `ReturnOutputToParent`。
- 子代理的普通文本流只用于 UI 进度；最终可交付结果必须调用 `ReturnOutputToParent` 写入 output collector。
- 第一次运行没有调用返回工具时，宿主追加一条用户提醒并再运行一次；第二次仍未返回时当前实现返回空字符串，没有显式协议失败。
- 调用是同步等待式：父模型的工具调用直到子代理结束才获得函数结果。
- 子代理内部文本、推理、工具与嵌套子代理被写入父助手消息中的 `CopilotChatSubAgentItem`，而不是创建独立的顶层聊天消息。
- 子代理返回字符串作为 `InvokeSubAgent` 的函数结果进入父角色自己的 AgentSession，父模型可在同一轮继续整合；子代理内部过程不会自动成为父模型的普通历史文本。

#### 现有返回收集器的实现风险

- `InvokeSubAgentAsync` 检查当前 `SubAgentToolExecutor` 实例 A 的 `_outputCollector`。
- `CreateSubAgentTools` 调用 `_provider.CreateTools(...)`，会创建新的 `SubAgentToolExecutor` 实例 B。
- 子代理实际调用的 `ReturnOutputToParent` 属于实例 B，因此写入 B 的 collector；实例 A 的 collector 仍没有输出。
- 当前测试验证了“未返回时发生第二次调用”，但没有断言 `InvokeSubAgent` 的最终函数结果等于 `ReturnOutputToParent` 参数，无法保护真正的回传语义。
- 角色型子代理不能直接复制该实现。实施前应把一次 invocation 的 collector、取消令牌和 UI item 收敛为共享 `SubAgentInvocationScope`，所有嵌套创建出的返回工具必须引用同一作用域；第二次仍未返回应产生明确失败。

#### `IsPresetInfo` 的真实边界

- `IsPresetInfo` 只存在于 `CopilotChatMessage`，legacy `ChatRoomMessage` 和 Domain `ChatRoomMessage` 都没有该字段。
- `CopilotChatMessage` 注释称预设消息“不参与 GPT 信息”，但当前字段本身没有参与 `CopilotChatManager.SendMessage` 的历史筛选。
- `CopilotChatManager` 发送时只把本轮用户消息传给 `ChatClientAgent`，历史由独立 `AgentSession` 保存；普通消息一旦已经进入 AgentSession，事后把 UI 消息标成 preset 不会从 AgentSession 删除。
- `AddConversationAsync(..., isPresetInfo: true)` 之所以不进入模型历史，是因为它只追加 UI/日志消息，不执行 AgentSession 发送，并非框架读取 `IsPresetInfo` 自动过滤。
- 当前明确使用 `IsPresetInfo` 的行为包括：不用于自动标题、空会话判断、会话消息计数筛选、取消/错误/欢迎提示标记和 XML 往返。
- UI 会显示 preset 消息；`ChatViewModel` 对所有 `ChatMessages` 建立投影，没有按该字段隐藏。因此 `IsPresetInfo = true` 表示“可显示但不应作为普通会话内容”，不是“界面不可见”。
- XML 会完整保存 preset 消息及其工具、子代理片段；恢复时保留该标记。

#### 对聊天室子代理隔离的结论

- 只在子代理底层 `CopilotChatMessage` 上设置 `IsPresetInfo = true` 不足以实现需求：legacy 增量上下文仍读取 `ChatRoomMessage.Content`，新 Coordinator 仍读取 Domain `Messages`，角色私有 AgentSession 也可能已包含该内容。
- 需要在聊天室领域消息或调用记录上增加显式上下文可见性，并让 legacy `BuildIncrementalUserMessages`、Coordinator 输入选择、消费水位、自动调度和恢复映射统一遵守。
- 更稳妥的目标不是把子代理结果提交为普通公开 Assistant 消息再过滤，而是把调用过程建模为瞬态 execution detail；只有用户直接调用时，才额外创建一个可显示、可持久化但 `IsPresetInfo = true` / `IncludeInChatContext = false` 的结果消息投影。
- AI 角色调用时，结果只作为调用工具的函数结果进入调用者自己的私有 AgentSession，不写入聊天室公开消息。
- 用户直接调用时，可以把用户视为调用方：子代理仍必须调用同一个返回工具；宿主取得工具提交值后生成结果消息展示给用户，该结果不进入任何普通角色后续输入。
- 为保持与现有子代理工具一致，子代理角色每次调用应使用新会话/无 committed checkpoint；角色定义提供稳定的人设、模型和工具配置，但不积累跨调用私有记忆。

#### Standard/Coding 工具差异

- legacy Standard 执行器走 `CopilotChatManager.SendMessage`，会追加默认 `InvokeSubAgent` 工具和本轮额外工具。
- legacy Coding 执行器使用 `CodingAgent`，运行时工具被工作区 Roslyn、文件和 CLI 工具完全替换，不包含 `InvokeSubAgent` 或 ChatRoom 协调工具。
- 新 `IsolatedChatRoomRoleRuntime` 最终仍映射回 legacy 角色；当前没有利用 `IChatRoomRoleExecutionEventSink` 报告工具或子代理详情。
- 如果产品定义“所有普通非人类角色均可调用子代理”，实施必须为 Standard 与 Coding 提供统一的聊天室级调用工具注入接缝，不能只在 `ChatRoomManager.StepAsync` 追加工具。
- 推荐把“调用聊天室子代理角色”的工具由 ChatRoom runtime/Coordinator 绑定，和通用 `InvokeSubAgent` 区分名称与职责；Coding 路径需要由 `CodingAgent` 提供受控 host tool 扩展点，或者首版明确仅 Standard 角色可调用。最终方案优先选择前者以满足“不被非人类角色通过 @ 调用，但可用子代理形式调用”的完整语义。

下一步调查角色管理、模板、持久化、UI 输入和测试链路，确定新增字段与调用记录需要传播到哪些文件。

### 角色管理、持久化、UI 与测试影响面

#### 角色管理和模板

- `ChatRoomRoleManagementTools` 当前只提供列举、创建和编辑普通角色；动态创建固定为 Standard + MentionOnly，并要求角色名能被普通 mention 正则解析。
- 子代理角色的名称同样需要满足严格前缀可解析，但校验理由应改为“用户可使用固定首部语法调用”，不能继续声称所有角色或用户都可在任意消息位置 `@` 它。
- `list_characters` 目前只展示参与模式，必须增加角色调用类别，避免模型把子代理误认为普通 MentionOnly 角色。
- 若允许模型动态创建子代理，`create_character` 需要显式的角色调用类别参数；建议首版只允许用户/UI 创建子代理，普通模型工具只能创建普通角色，减少模型自行扩权风险。
- `edit_character` 当前不能修改 `ParticipationMode`、`ExecutionKind`、`IsHuman` 或管理者身份。子代理类别如果允许编辑，应通过专用、受约束参数更新，不能被任意普通模型静默切换；建议工具只读展示，UI 才能修改角色类别。
- `RoleTemplateService.ToDefinition`、`FromDefinition`、`UpdateFromDefinition` 对角色字段逐项复制，新增角色类别必须三处同步。
- 模板校验必须增加子代理不变量；旧模板缺失字段默认普通角色。
- 预置模板和 `CodingAssistantRoleFactory` 当前都是普通角色；不应升级为子代理，除非另行增加明确的子代理预置模板。

#### legacy 持久化

- `room.config.json` 直接序列化 legacy 角色定义和公开消息；新增简单枚举/布尔字段可由 System.Text.Json 自动往返，旧 JSON 使用字段默认值。
- `ChatRoomPersistence.ValidateRoleDefinitions` 当前只校验执行种类和“人类不能 Coding”，必须增加“人类不能是子代理”“子代理不能是管理者”“子代理不能 AlwaysParticipate”等不变量。
- `SavePublicMessageAsync` 把 `ChatRoomMessage` 临时转换成新的 `CopilotChatMessage`，当前不会传播 `IsPresetInfo`。若子代理用户结果仍走公开日志，必须传递 preset/上下文可见性，或者采用独立调用日志避免被误当普通公开聊天。
- legacy `ChatRoomSessionData.Messages` 会把所有消息计入会话摘要。若子代理调用请求和结果保留为可显示消息，需决定摘要 MessageCount 是否包含；建议 UI 计数包含，角色上下文不包含。
- 子代理角色要求无状态时，不应保存和恢复 `{RoleId}/agent-session-state.json`；加载链也不应调用 `RestoreAgentSessionStateAsync`。

#### 新 snapshot 持久化

- `StoredChatRoomRoleDefinition`、`StoredChatRoomMessage`、`ChatRoomSnapshotMapper` 和 `ChatRoomSnapshot.DeepClone` 都是显式逐字段映射，新增角色类别或消息上下文标记必须全部传播。
- 当前 `CurrentSchemaVersion = 2`，`FromStored` 只接受等于当前版本，没有兼容迁移。
- 新 schema 必须同时实现旧版本读取：旧角色类别默认为普通角色，旧消息默认为进入上下文；未知更高版本继续拒绝。
- 子代理调用的瞬态 execution detail 不应进入 snapshot。若保留用户可见结果消息，则只持久化稳定的请求摘要/最终输出和调用关联 ID，不持久化推理、工具流和中间状态。
- 子代理无状态时不应生成或提交 `ChatRoomRoleCheckpoint`，也不需要 `ConsumedThroughSequenceByRole`；普通角色的消费水位仍必须越过这些不可见消息，避免未来反复扫描。

#### Avalonia 角色配置与调用入口

- `RoleEditViewModel` 当前只有人类开关、模型、参与模式、人设和记忆；新建角色固定 Standard。
- 建议新增“角色调用方式”选项：`聊天室参与者` / `子代理`。选为子代理后：
  - 强制 `IsHuman = false`；
  - 强制退出普通参与模式，UI 隐藏或禁用参与模式；
  - 不允许管理者身份；
  - 说明“只可由代理工具或用户消息开头 `@名称 ` 调用”；
  - 说明每次调用独立，不读取聊天室历史，输出不参与后续上下文。
- `RoleListViewModel.ParticipationModeDisplay` 当前只显示“人类/AI 角色”，没有真实参与模式；应对子代理显示明显标签。
- `RoleListView.axaml`、消息头像和角色名称都提供“@ 提及角色”菜单。对子代理该菜单可以保留，但必须改为“调用子代理”，且插入内容时只能放到输入开头；若输入框已有非空内容，应替换为 `@名称 ` 开头或拒绝插入，而不能像普通 mention 一样追加到末尾。
- `ChatViewModel.InsertMention` 当前在输入已有内容时追加 ` @角色名 `，这对用户子代理调用必然无效；需分为普通提及和子代理调用两个命令。
- `ChatViewModel.SendAsync` 对输入执行 `Trim()`，会移除前导空白。这意味着用户输入 `  @子代理 任务` 会被转成有效前缀，与“只能内容开头生效”的字面要求冲突。严格语义下应保留原始开头用于路由，只校验 `IsNullOrWhiteSpace`，不能在解析前 Trim；发送后展示是否保留尾随空白可单独规范化。
- 当前发送后无条件调用 `StartAutoLoopAsync`。子代理直接调用必须由服务返回“普通消息/子代理调用”分流结果，UI 对子代理调用等待专用执行完成，不启动普通自动循环。
- 用户直接调用结果应显示为子代理角色消息或专用调用卡片，并标识为 preset/不参与上下文；普通角色通过工具调用继续显示在调用者助手消息的 `CopilotChatSubAgentItem` 内。
- 输入框占位文字应补充严格语法，例如“调用子代理必须以 `@子代理名 ` 开头”。

#### 测试基础和新增矩阵

现有测试已覆盖 mention 顺序、自动循环、角色工厂、工具创建编辑、模板复制、legacy JSON、snapshot 映射、角色编辑页和大厅添加。新增测试至少分为：

1. **严格解析器**：精确开头、ASCII 空格、无前导空白、消息中段无效、Tab/换行无效、未知角色、普通角色、同名前缀、大小写、空任务。
2. **普通 mention 隔离**：用户中段 `@子代理 ` 不触发；AI 任意位置 `@子代理 ` 不触发；普通角色 mention 仍保持原行为。
3. **调度保护**：子代理不进入默认队列、优先 mention 队列、管理者兜底和公开手动发言；错误调用命令被拒绝。
4. **工具调用**：普通 Standard 角色可按 RoleId 调用；模型只能看到可调用子代理列表；返回工具共享同一 collector；无返回工具、重复返回、取消、异常和递归调用均有确定结果。
5. **无状态与输入隔离**：每次调用只收到角色静态定义 + 本次任务；不读取公开历史、不恢复 checkpoint、不保存 AgentSession；两次调用互不记忆。
6. **用户直调**：请求和结果可显示，结果 `IsPresetInfo = true`，普通角色后续输入不含请求/结果，且不启动默认角色自动循环。
7. **上下文消费**：不可见消息仍被普通角色消费水位越过，后续不会补发；失败/取消按设计决定是否保留可重试调用记录。
8. **持久化**：legacy 新字段往返、旧 JSON 默认普通；snapshot 新 schema 往返和旧 schema 迁移；子代理无 checkpoint；preset 结果恢复后仍不进入上下文。
9. **UI**：角色类别联动、子代理标签、普通提及菜单隐藏或改名、前缀插入位置、Send 分流、结果卡片显示、消息计数和会话恢复。
10. **回归**：已有 MentionOnly、manager、Coding、角色模板和角色管理工具测试全部保持通过。

下一步基于以上事实收敛设计决策并编写最终方案文档。

## 8. 暂存风险

- “子代理角色输出设置 `IsPresetInfo = true`”可能同时影响 UI 是否显示；需避免把“上下文隔离”错误等同于“界面隐藏”。
- 如果现有工具调用只存在于单个 Agent 运行内部，则跨聊天室角色调用需要明确关联 ID、调用方和结果投递方式。
- 用户直接调用子代理时不存在上层 AI 调用者，需要定义结果最终呈现给谁，同时仍保持“只能通过工具返回”的执行协议。
- 若仅修改 MentionParser，自动发言或管理者工具仍可能绕过解析器调用子代理，因此必须从调度候选和执行入口双层设防。
