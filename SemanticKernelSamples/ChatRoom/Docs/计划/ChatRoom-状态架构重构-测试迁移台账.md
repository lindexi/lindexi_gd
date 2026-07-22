# ChatRoom 状态架构重构测试迁移台账

## 状态

- 对应计划：`ChatRoom-状态所有权与恢复架构重构计划.md`
- 当前阶段：阶段 3
- 基线构建：通过
- 基线测试：
  - `AgentLib.Coding.Tests`：44/44 通过
  - `AgentLib.ChatRoom.Tests`：320/321 通过
  - `ChatRoom.Shell.Tests`：30/30 通过
- 已知基线失败：`ChatRoomSessionTests.AddMessageAsync_WithDispatcher_InvokesOnMainThread`
  - 原因：测试把 `CheckAccess()` 设置为 `true`，却断言 `InvokeAsync` 被调用，与生产分支相反。
  - 处置：在 UI projection + `UiThreadHarness` 替代测试通过前保留；阶段 7 替换后删除。

## 门禁规则

1. 生产观察孔删除前，表中对应替代测试必须通过。
2. 同一改动先加入替代测试，再删除旧断言。
3. 任何 `待迁移` 项不得在阶段 8 被直接删除。
4. 允许删除实现细节断言，但其保护的业务不变量必须保留。
5. 新并发测试使用显式 gate 和队列 drain；timeout 只作为死锁 watchdog。
6. 新持久化故障测试使用可注入故障点，不依赖偶发磁盘失败。

## 迁移台账

| 旧测试或断言 | 当前保护的不变量 | 新测试名称/层级 | 最早阶段 | 状态 | 删除旧断言的条件 |
|---|---|---|---:|---|---|
| `ChatRoomSessionTests.FromPersistence_*` | SessionId、CreatedAt、标题、消息顺序、角色消费进度恢复 | `ChatRoomSnapshotMapperTests`、repository round-trip contract | 2/6 | 部分迁移 | 新往返与完整加载测试通过 |
| `ChatRoomSessionTests.ToPersistence_*` | LastActivityAt、角色/消息完整性、容器隔离 | `ChatRoomDomainContractTests`、`ChatRoomSnapshotMapperTests` | 2 | 替代测试已建立 | 阶段 6 文件仓储契约通过 |
| `ChatRoomSessionTests.AddMessage*` | 消息追加、序号、水位推进、空/非法输入 | coordinator command tests | 5 | 待迁移 | `AppendHumanMessage` 与 `CompleteRoleSpeak` 测试通过 |
| `ChatRoomSessionTests.GetMessagesSinceLastSpeak_*`、`HasRoleSpoken_*` | 首次发言输入、增量消费、不重复、不跳过 | execution lease `InputThroughSequence` tests | 5 | 待迁移 | MessageSequence/ConsumedThroughSequence 测试通过 |
| `ChatRoomSessionTests.MainThreadDispatcher_*`、`AddMessageAsync_WithDispatcher_*` | UI 集合线程亲和性 | `ChatRoomProjectionUiThreadTests` + `UiThreadHarness` | 8 | 待迁移 | 工作线程事件只在 UI 线程更新集合的测试通过 |
| `ChatRoomSessionTests.DisplayText_*`、Title 的 UI 通知断言 | 会话标题显示与通知 | projection/view-model property tests | 8 | 待迁移 | UI projection 属性通知测试通过 |
| `ChatRoomRoleFactoryTests.Create*Executor` 中具体类型断言 | execution kind 映射 | runtime factory behavior tests | 4 | 部分迁移 | Standard/Coding 正式工厂行为测试已通过；待 coordinator 切换后删除旧具体类型断言 |
| `ChatRoomRoleTests.EndpointManager_*`、`Executor`、对象同一性断言 | 模型可用、runtime 连续性、唯一所有者 | runtime registry/factory tests | 4 | 待迁移 | 外部不可取得 runtime，checkpoint 连续性测试通过 |
| `ChatRoomRoleTests.SpeakAsync_*`、`SpeakFirstAsync_*` | 输入验证、流式结果、取消和异常 | candidate execution/runtime tests | 4/5 | 待迁移 | runtime 与 coordinator 完成/失败/取消测试通过 |
| `ChatRoomRoleTests.ReduceSession*`、`ClearSessionMemory*` | 私有会话维护 | coordinator checkpoint command tests | 5/6 | 待迁移 | checkpoint revision 与 durable commit 测试通过 |
| `ChatRoomManagerTests.StepAsync_*` | 当前发言者、失败消息、取消、流式消息清理、持久化失败语义 | coordinator execution state-machine tests | 5/6 | 待迁移 | 每个 execution 恰有一个终态且 candidate 按规则接受/丢弃 |
| `ChatRoomManagerIntegrationTests.StartAutoLoopAsync_*` | mention 顺序、参与模式、manager 仲裁、循环终止、插话 | `ChatRoomCoordinatorScenarioTests` | 5 | 待迁移 | 公共业务场景等价测试全部通过 |
| `ChatRoomManagerIntegrationTests.CreateCharacterTool_*` | AI 工具创建角色后可参与对话 | application/coordinator tool scenario | 5/8 | 待迁移 | 工具只经 ApplicationService command 且场景通过 |
| `ChatRoomManagerTests.AddRoleAsync_*`、`RemoveRoleAsync_*`、`UpdateRoleAsync_*` | 角色验证、provider 可用、保存、释放、更新语义 | coordinator + runtime registry + persistence receipt tests | 4/5/6 | 待迁移 | 不可变 definition、incarnation、迁移策略测试通过 |
| `ChatRoomManagerTests.RegisterRoleModelProviders_*` 的精确调用与对象断言 | 模型可用和选型结果 | runtime factory provider snapshot behavior tests | 4/7 | 待迁移 | 业务结果测试通过 |
| `ChatRoomManagerTests.SetWorkspacePathAsync*` | 跨角色切换、回滚、重复路径、关闭竞争 | Coding transaction + coordinator workspace saga tests | 3/5 | 待迁移 | 发布前回滚、发布后不可回滚、旧 lease 退休测试通过 |
| `ChatRoomRoleExecutorTests.StandardWorkspaceTool*` | 审批、拒绝、取消后工作区变更 | execution approval routing + workspace saga tests | 3/5 | 待迁移 | `ExecutionId + ApprovalId` 精确路由测试通过 |
| `ChatRoomRoleExecutorTests.CodingExecutor*` | Coding 输入、锁、取消与流式行为 | Coding runtime candidate tests | 3/4 | 待迁移 | 公共 workspace transaction 与 candidate checkpoint 测试通过 |
| `ChatRoomManagerTests.CloseAsync*`、`ClosedManager_*` | 停止、幂等关闭、不可取消执行、资源释放 | coordinator Close/Stuck/Abandoned tests | 5 | 待迁移 | Close 不提前成功且状态机测试通过 |
| `ChatRoomManagerTests.SaveAsync_ThenLoadAsync_*` | 公开消息和角色恢复 | repository + ApplicationService open/switch tests | 6/7 | 待迁移 | 完整 snapshot/load/switch 测试通过 |
| `ChatRoomManagerIntegrationTests.LoadAsync_*` | Standard/Coding 私有会话恢复与连续对话 | runtime checkpoint + ApplicationService load tests | 4/7 | 待迁移 | 两类 runtime 从 checkpoint 恢复并继续对话 |
| `ChatRoomPersistenceTests.SaveConfigAsync_*`、`LoadConfigAsync_*` | 序列化、执行种类验证、取消 | repository contract + serializer tests | 2/6 | 部分迁移 | 正式 stored DTO round-trip 和 schema 校验通过 |
| `ChatRoomPersistenceTests.SaveRoleAgentSessionStateAsync_*`、`LoadRoleAgentSessionStateAsync_*`、`DeleteRoleAgentSessionStateAsync_*` | 私有状态保存与删除 | aggregate repository checkpoint tests | 6 | 待迁移 | checkpoint 与公开消息同 revision/CommitId 提交 |
| `ChatRoomPersistenceTests.SavePublicMessageAsync_*`、`SaveRoleMessageAsync_*` | 日志可生成、参数验证 | generation 派生日志 tests | 6 | 待迁移 | 派生日志按 generation 幂等生成测试通过 |
| `ChatRoomPersistenceTests.ListSessionIds_*`、`Delete_*` | 列表过滤、删除、路径安全 | repository contract + file durability tests | 6 | 待迁移 | manifest-only list、tombstone delete、路径安全测试通过 |
| Persistence 路径穿越、绝对路径、Windows 目录别名断言 | 存储根边界 | file repository security tests | 6 | 待迁移 | 新仓储覆盖路径、大小写、链接越界测试 |
| `CodingWorkspaceToolProviderTests.SetWorkspacePathAsync_*` | 候选创建、发布顺序、失败保留 | public transaction contract tests | 3 | 替代测试已建立 | Prepare/Apply/Rollback/CommitAfterPublish、单一发布屏障、关闭竞态通过 |
| `CodingWorkspaceToolProviderTests.Lease*`、`Dispose*` | 旧 workspace lease 保活与退休 | public transaction lease tests | 3 | 保留并扩展 | 发布后旧资源仅在 lease 释放后退休；Prepared/Applied 关闭清理通过 |
| `CodingAgentTests.WorkspaceChangeDuringRun*` | 运行固定旧工具 lease，下一轮用新工具 | Coding runtime/workspace transaction integration | 3/4 | 待迁移 | workspace version 与 lease 测试通过 |
| `ChatRoomServiceTests.CreateNewSession_*`、`LoadSession*`、`CloseCurrentSession*` | 生命周期、切换、统一 factory | ApplicationService tests | 7 | 待迁移 | lifecycle gate、atomic switch、retired registry 测试通过 |
| `ChatRoomServiceTests.Save_*`、`ListSessions_*`、`DeleteSession_*` | 空会话策略、列表、删除 | ApplicationService + repository contract | 6/7 | 待迁移 | command 自动持久化且 current session 删除保护通过 |
| `ChatRoomServiceTests.AddRole*`、`UpdateRole*`、`RemoveRole*` | UI 用例委托与角色业务 | ApplicationService command tests | 7 | 待迁移 | UI 不再取得 Manager/Role 活对象 |
| `RoleLobbyViewModelTests.*` | 连续添加、重复角色错误、导航 | projection/ApplicationService ViewModel tests | 8 | 待迁移 | UI 线程与导航契约测试通过 |
| `TestMainThreadDispatcher` 相关测试 | shell 线程调度 | `UiThreadHarness` | 8 | 待迁移 | CheckAccess 基于真实线程且队列 drain 无异常 |
| 生产 ViewModel public 无参构造相关绑定 | 设计时数据 | 独立设计数据工厂验证 | 8 | 待迁移 | 生产 ViewModel 不再用 `null!` 依赖构造 |

## 阶段完成记录

| 阶段 | 验收结果 | 日期/说明 |
|---:|---|---|
| 1 | 通过 | 已建立正式不可变状态、消息序号、角色 identity/incarnation/runtime version、checkpoint、snapshot、Stored DTO、JSON 映射、内存仓储和 ScenarioBuilder；领域、映射与仓储契约测试 17/17 通过 |
| 2 | 通过 | 已公开 `IWorkspaceChangeTransaction` 与 `PrepareWorkspaceChangeAsync`，实现发布屏障、发布前回滚、发布后幂等提交、租约退休和关闭清理；已删除旧 Candidate API 与 ChatRoom friend access，Coding 全套 44/44 通过 |
| 3 | 进行中 | 已建立正式 runtime factory、provider snapshot、runtime registry 和 execution lease；运行时契约测试 6/6 通过 |
| 4 | 未开始 | |
| 5 | 未开始 | |
| 6 | 未开始 | |
| 7 | 未开始 | |
| 8 | 未开始 | |
