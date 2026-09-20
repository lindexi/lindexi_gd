# CodingChatRoom 工作任务运行时命名与职责重构设计

## 1. 文档定位

本文针对 `CodingChatRoom.AvaloniaShell` 当前多工作任务架构中的命名和职责边界进行设计，重点解决以下问题：

- `CodingChatApplication` 容易被理解为整个桌面应用级对象，但实际是每个工作任务独立创建一个实例；
- 工作任务身份通过创建后的 `SetWorkTask` 注入，生命周期约束不清晰；
- 工作任务身份、会话归属、会话显示快照和跨任务打开历史会话的规则没有被类型边界明确表达；
- `CodingChatRuntime`、`CodingChatStartup` 与 `CodingChatApplication` 的命名没有共同体现“每工作任务一个运行时”的事实。

本文只做设计，不修改业务代码、测试、项目文件或界面。

## 2. 当前对象层级结论

当前 `CodingChatApplication` 不是整个桌面进程唯一的应用对象，而是单个工作任务级对象。

实际组合关系为：

```text
App（桌面进程组合根）
└─ MainViewModel（多个工作任务的界面编排）
   ├─ WorkTaskItemViewModel A
   │  └─ CodingChatRuntime A
   │     ├─ CopilotChatManager A
   │     ├─ CodingAgent A
   │     ├─ CodingChatApplication A
   │     ├─ CodingWorkspaceController A
   │     └─ CodingAgentChatRunner A
   └─ WorkTaskItemViewModel B
      └─ CodingChatRuntime B
         ├─ CopilotChatManager B
         ├─ CodingAgent B
         ├─ CodingChatApplication B
         ├─ CodingWorkspaceController B
         └─ CodingAgentChatRunner B
```

`CodingChatStartup.InitializeAsync` 每执行一次，就会创建一套新的任务运行时对象。启动恢复多个任务、创建新任务和恢复归档任务时都会分别调用该方法。

因此，`CodingChatApplication` 的真实职责不是“应用程序”，而是：

- 协调一个工作任务中的会话；
- 协调该任务的发送、插话、停止、循环、压缩和收尾；
- 协调该任务的工作路径；
- 保存该任务当前会话；
- 向界面发布该任务的运行状态。

## 3. 推荐命名

### 3.1 首选名称：`CodingWorkTaskController`

建议将：

```text
CodingChatApplication
```

重命名为：

```text
CodingWorkTaskController
```

选择该名称的原因：

1. `CodingWorkTask` 明确表达实例归属于一个工作任务，而不是整个桌面应用。
2. `Controller` 符合其现有行为：它协调多个已有对象和一组用例，而不是单纯保存数据。
3. 它不与 Avalonia 的 `Application`、MVVM 的 ViewModel 或 AgentLib 的 ChatManager 概念混淆。
4. 它允许现有公开行为继续保持在同一类型中，不需要为了命名重构额外引入接口或服务层。
5. 与 `CodingWorkspaceController` 虽然同样使用 `Controller`，但领域前缀不同，职责可以清楚区分：
   - `CodingWorkTaskController`：任务级用例与生命周期协调；
   - `CodingWorkspaceController`：工作路径状态和切换协调。

### 3.2 不推荐的候选名称

#### `CodingChatService`

不推荐。`Service` 过于宽泛，无法表达它是有状态、每任务独立且拥有完整运行生命周期的对象。

#### `CodingChatCoordinator`

语义基本正确，但没有表达工作任务边界。若采用，应至少命名为 `CodingWorkTaskCoordinator`。相比之下，现有代码已经使用多个 Controller，`CodingWorkTaskController` 更一致。

#### `CodingChatSessionManager`

不推荐。该类型不仅管理 Session，还负责运行、停止、循环、压缩、工作路径同步和收尾；名称会缩小真实职责。

#### `CodingChatRuntime`

不能用于替代 `CodingChatApplication`，因为现有 `CodingChatRuntime` 已是依赖容器和释放边界。控制器与运行时容器是两个不同概念。

#### `WorkTaskApplication`

仍保留了 `Application` 的歧义，也没有体现编程聊天领域。

## 4. 配套命名调整

为使整套对象名称表达一致，建议采用以下命名表：

| 当前名称 | 建议名称 | 说明 |
| --- | --- | --- |
| `CodingChatApplication` | `CodingWorkTaskController` | 单任务用例、状态和生命周期协调器 |
| `CodingChatRuntime` | `CodingWorkTaskRuntime` | 单任务拥有的运行时对象集合与释放边界 |
| `CodingChatStartup` | `CodingWorkTaskRuntimeFactory` | 创建单个任务运行时，而不是启动整个应用 |
| `CodingChatOperationPhase` | `CodingWorkTaskOperationPhase` | 明确阶段只属于一个任务 |
| `CodingAgentChatRunner` | 暂时保留 | 已准确表达 CodingAgent 到聊天运行器的适配职责 |
| `WorkTaskItemViewModel` | 保留 | 已准确表达界面任务项 |
| `MainViewModel` | 保留 | 它确实负责桌面主界面的多任务编排 |

其中 `CodingWorkTaskRuntimeFactory` 建议由静态类改为普通 `internal sealed class` 还是继续使用静态类，应根据依赖注入和测试需要决定。当前没有替换工厂外部依赖的明确需求时，可以先保持静态结构，仅重命名，避免扩大重构范围。

## 5. 重构后的职责边界

### 5.1 `App`

`App` 是桌面进程的组合根，负责：

- 创建全局路径和共享配置；
- 加载 `WorkTasks.json`；
- 为每个活动任务创建一个 `CodingWorkTaskRuntime`；
- 创建 `MainViewModel`；
- 在窗口退出时释放所有任务运行时。

`App` 不负责：

- 发送聊天消息；
- 管理某个任务的当前会话；
- 修改任务内的工作路径；
- 给 Session 写入任务身份。

### 5.2 `MainViewModel`

`MainViewModel` 是多个任务的界面编排器，负责：

- 活动、归档任务集合；
- 当前界面选择了哪个任务；
- 创建、恢复、归档、删除和重命名任务；
- 将当前任务的聊天和历史 ViewModel 投影给界面；
- 保存任务元数据。

它不应直接操作 `CopilotChatSession` 的归属字段。

### 5.3 `CodingWorkTaskRuntime`

`CodingWorkTaskRuntime` 是单个任务的运行时对象容器和释放边界，负责持有：

- `CodingWorkTaskController`；
- `CopilotChatManager`；
- `CodingAgent`；
- `CodingWorkspaceController`；
- `CodingAgentChatRunner`；
- 与该任务运行相关的模型和日志对象。

它不承担业务协调逻辑。它的主要价值是：

- 明确“一任务一套可变运行时”；
- 统一释放任务拥有的资源；
- 为 `WorkTaskItemViewModel` 提供稳定的运行时所有权。

### 5.4 `CodingWorkTaskController`

`CodingWorkTaskController` 负责单个任务范围内的应用用例：

- 初始化和加载历史摘要；
- 新建会话；
- 打开会话；
- 删除、重命名会话；
- 发送、插话、循环运行和停止；
- 压缩会话；
- 保存会话；
- 管理任务级运行阶段和取消令牌；
- 协调任务的工作路径控制器；
- 发布任务级状态变化。

该类型不负责：

- 多任务集合管理；
- 任务归档和删除；
- 任务配置文件读写；
- 当前桌面导航；
- 跨任务转移 Session。

## 6. 工作任务身份模型

### 6.1 Id 与名称的语义不同

工作任务身份包含两个不同性质的数据：

- `Id`：稳定身份，不随重命名变化；
- `DisplayName`：当前显示名称，可以变化。

Session 中保存的字段语义应定义为：

- `WorkTaskId`：会话归属键；
- `WorkTaskName`：保存该会话时的任务名称快照，主要用于显示、搜索和诊断，不作为归属判断依据。

任何关联判断必须只使用 `WorkTaskId`。不得使用任务名称或工作路径判断归属。

### 6.2 构造时注入稳定 Id

`CodingWorkTaskController` 的构造函数应要求提供任务 Id，避免创建后再调用 `SetWorkTask`：

```text
CodingWorkTaskController(
    Guid workTaskId,
    string workTaskDisplayName,
    CopilotChatManager chatManager,
    ICodingChatSessionStore sessionStore,
    ICodingChatRunner chatRunner,
    CodingWorkspaceController workspaceController,
    CodingAgent codingAgent)
```

约束：

- `workTaskId` 必须非空；
- Id 创建后不可修改；
- 名称不能为空白；
- 构造完成时，控制器已经处于完整有效状态；
- 不保留允许同时修改 Id 和名称的 `SetWorkTask` 方法。

字段建议为：

```text
private readonly Guid _workTaskId;
private string _workTaskDisplayName;
```

如果需要公开只读属性，可使用：

```text
Guid WorkTaskId
string WorkTaskDisplayName
```

### 6.3 名称更新使用明确方法

任务重命名后，仅更新显示名称：

```text
UpdateWorkTaskDisplayName(string displayName)
```

该方法不得修改：

- `WorkTaskId`；
- 历史 Session 的归属；
- 会话标题；
- 工作路径。

是否立即批量改写所有历史 Session 的 `WorkTaskName` 不建议由重命名操作完成。更合理的规则是：

- `WorkTaskId` 始终保持不变；
- 当前已加载会话可更新名称快照；
- Session 下次正常保存时写入最新任务名称；
- 未加载的旧 Session 允许继续保留旧名称快照；
- 历史界面若能通过任务存储按 Id 找到当前名称，应优先显示当前名称，并可将 Session 内名称作为已删除任务或数据恢复时的回退文本。

这样可以避免一次任务重命名触发全历史目录批量写入。

## 7. Session 归属规则

### 7.1 新建会话

由当前 `CodingWorkTaskController` 新建的 Session 必须立即设置：

```text
WorkTaskId = 当前控制器的 WorkTaskId
WorkTaskName = 当前控制器的 WorkTaskDisplayName
```

这应在 Session 创建流程内完成，而不是等待首次运行后补写。否则用户创建空会话后立即切换或退出时，历史归属可能缺失。

### 7.2 当前任务产生的运行

每次保存 Session 前应确保：

- Session 的 `WorkTaskId` 与当前控制器一致；
- `WorkTaskName` 更新为当前显示名称；
- 工作路径按现有规则保存。

这里的“确保”只适用于由当前任务创建或已经明确归属于当前任务的 Session。

### 7.3 打开已有会话

打开已有 Session 时不得无条件覆盖 `WorkTaskId`。

应按以下规则处理：

#### Session 已归属于当前任务

正常打开，并可刷新 `WorkTaskName` 快照。

#### Session 没有 `WorkTaskId`

这是旧格式会话。建议采用显式兼容规则：

- 从“当前任务历史”入口打开时，可在用户明确操作后将其归入当前任务；
- 从“全部历史”入口打开时，不应静默认领；可以只读展示归属缺失状态，或在切换前提示用户选择“关联到当前任务”；
- 在产品尚未提供确认界面前，优先保持未归属，不写入当前任务 Id。

#### Session 归属于其他任务

不得因为在当前任务界面点击了它，就把归属改为当前任务。

可选产品策略有两种：

1. 在对应任务中打开：根据 `WorkTaskId` 激活目标任务，再打开 Session；
2. 明确执行“移动到当前任务”：用户确认后修改归属。

首选策略是第一种。只有存在明确产品需求时才实现第二种。

### 7.4 删除或归档任务

- 归档任务不改变 Session 的 `WorkTaskId`；
- 恢复归档任务继续使用原 Id；
- 删除任务时，Session 可以保留原 `WorkTaskId`，历史界面显示 Session 保存的 `WorkTaskName` 快照；
- 不应因任务删除而批量删除或重写会话，除非产品提供独立且明确的“同时删除历史”操作。

## 8. 历史列表与搜索边界

历史搜索继续支持：

- 会话标题；
- 工作路径；
- `WorkTaskName`；
- `WorkTaskId`。

“当前工作任务的历史会话”必须按 `WorkTaskId` 精确过滤，而不是把 Id 字符串当作普通模糊搜索文本。长期设计建议为 `SessionListViewModel` 增加独立过滤状态：

```text
Guid? WorkTaskIdFilter
string SearchText
```

两者语义不同：

- `WorkTaskIdFilter`：结构化归属过滤；
- `SearchText`：用户输入的模糊检索。

当前通过把任务 Id 写入 `SearchText` 达到过滤效果，只适合作为过渡实现。结构化过滤可以避免未来某个工作路径、标题或名称偶然包含相同字符串时产生错误结果。

## 9. 创建流程设计

建议将单任务运行时创建入口调整为：

```text
CodingWorkTaskRuntimeFactory.CreateAsync(
    WorkTaskIdentity taskIdentity,
    CodingChatRoomPaths paths,
    IMainThreadDispatcher mainThreadDispatcher,
    WindowsSandboxToolSource? windowsSandboxToolSource = null)
```

其中 `WorkTaskIdentity` 可以是内部不可变 record：

```text
internal sealed record WorkTaskIdentity(Guid Id, string DisplayName);
```

是否实际增加该 record，应以减少参数错位和明确语义为判断标准。它不是通用领域抽象，不需要接口，也不应包含工作路径、模型或运行状态。

如果不增加该 record，也应让工厂直接接收 `workTaskId` 和 `workTaskDisplayName`，确保控制器创建时身份完整。

创建顺序建议为：

1. 从 `WorkTaskRecord` 取得稳定 Id 和名称；
2. 创建任务运行时依赖；
3. 使用 Id 和名称构造 `CodingWorkTaskController`；
4. 创建 `CodingWorkTaskRuntime`；
5. 创建 `WorkTaskItemViewModel`；
6. 恢复工作路径、模型和思考强度；
7. 加载历史摘要。

不得再由 `MainViewModel.CreateRuntimeTask` 调用 `SetWorkTask` 完成补充初始化。

## 10. 重命名传播流程

任务名称修改成功后的流程应为：

1. 校验并规范化新名称；
2. 保存 `WorkTasks.json`；
3. 更新 `WorkTaskItemViewModel.DisplayName`；
4. 调用 `CodingWorkTaskController.UpdateWorkTaskDisplayName`；
5. 更新当前已加载 Session 的显示名称快照；
6. 刷新历史摘要中的名称显示；
7. 不修改任务 Id、会话标题和工作路径。

为了避免保存失败后内存状态与文件状态不一致，具体提交顺序应遵循现有项目的失败回滚约定。推荐先构建待保存记录并成功写盘，再提交 ViewModel 和控制器名称；若现有 UI 必须先显示新名称，则必须保留旧值并在保存失败时完整回滚。

## 11. 建议的迁移阶段

### 阶段一：纯重命名

- `CodingChatApplication` → `CodingWorkTaskController`；
- `CodingChatRuntime` → `CodingWorkTaskRuntime`；
- `CodingChatStartup` → `CodingWorkTaskRuntimeFactory`；
- `CodingChatOperationPhase` → `CodingWorkTaskOperationPhase`；
- 更新字段、属性和局部变量名称；
- 不改变行为。

### 阶段二：构造完整性

- 将任务 Id 和名称加入控制器构造函数；
- 删除 `SetWorkTask`；
- 任务 Id 改为只读；
- 增加仅更新名称的明确方法；
- 让运行时工厂在创建时接收任务身份。

### 阶段三：修正 Session 归属

- 新建 Session 时立即写入归属；
- 打开 Session 时不再无条件覆盖归属；
- 保存前校验归属；
- 明确旧 Session 和跨任务 Session 的处理方式。

### 阶段四：结构化历史过滤

- `SessionListViewModel` 增加 `WorkTaskIdFilter`；
- “当前任务历史”使用精确 Id 过滤；
- 搜索框继续负责用户模糊搜索；
- 补充归档和已删除任务的显示回退规则。

每个阶段都应独立构建和测试，避免一次提交同时进行大规模符号重命名与行为修复。

## 12. 测试设计

### 12.1 生命周期测试

- 每个工作任务拥有不同的 `CodingWorkTaskController` 和 `CodingWorkTaskRuntime`；
- 停止任务 A 不影响任务 B；
- 删除任务只释放对应 Runtime；
- 窗口退出释放全部 Runtime。

### 12.2 身份测试

- 控制器构造后任务 Id 不可修改；
- 任务重命名不改变 Id；
- 新建 Session 立即带有任务 Id 和名称；
- Session 保存和重新加载后保留任务 Id；
- 多个 Session 可以共享同一个任务 Id。

### 12.3 归属测试

- 打开当前任务 Session 不改变归属；
- 打开其他任务 Session 不会静默改成当前任务；
- 归档和恢复任务后 Session 仍按原 Id 检索；
- 删除任务后 Session 仍保留原归属和名称快照；
- 旧 Session 无 Id 时遵循明确兼容策略。

### 12.4 历史过滤测试

- 当前任务历史只返回 `WorkTaskId` 精确匹配的 Session；
- 相同工作路径的两个任务不会互相混入；
- 任务重命名后仍能按 Id 找到旧 Session；
- 用户搜索可匹配路径、名称和 Id；
- 结构化任务过滤与文本搜索可以同时生效。

## 13. 最终命名结论

推荐使用以下核心命名：

```text
CodingWorkTaskController
CodingWorkTaskRuntime
CodingWorkTaskRuntimeFactory
CodingWorkTaskOperationPhase
```

其中最重要的改名是：

```text
CodingChatApplication → CodingWorkTaskController
```

该名称最准确地表达了当前类型的真实生命周期和职责：它是一个工作任务内部的有状态用例协调器，而不是整个桌面应用程序。

重构不应止于符号改名。最终目标是让类型结构强制表达以下约束：

- 一个工作任务拥有一套独立运行时；
- 一个控制器在构造时就拥有稳定任务 Id；
- 任务名称可变，但任务 Id 不可变；
- Session 归属不能因打开历史会话而被静默修改；
- 跨任务操作必须由产品显式表达，而不是由当前界面上下文隐式决定。
