# Win32 输入目标与候选窗口锚点开发计划

## m942 最终签收：后台学习有界改造完成，G02完整矩阵仍开放

本节优先于下方历史待实施清单；历史证据不覆盖、不删除。完整证据及哈希见[closeout](../../../../.coding-agent/g02-closeout-validation.md)，当前交接见[主交接](../../../../Docs/next-session-handoff.md)。

- [x] 实现不可变学习事件，生产者TryWrite，容量512/DropOldest；无前台Learn/GetEntries或逐事件Task.Run。
- [x] Host单消费者批次上限128，合并计数、批量学习/原子持久化；后台异常和容量/退出丢弃统计；退出等待预算250ms。
- [x] 后台准备完整快照及解码派生数据后发布，纯频次复用拓扑；输入继续旧快照，无学习诱发下一按键全表重建。
- [x] 活动有效旧候选按展示原目标和消费范围提交；真正过期revision/跨上下文ID拒绝，保留精确END/result/text合同。
- [x] 最终Release全解决方案构建成功，3个既有CA1416警告/0错误；实际重跑Core655、Dictionary413、Module226、Integration合并定向109，共1403通过/0失败/0跳过，非完整Integration。文档落盘后的Release增量复构建0警告/0错误、3.32s，先前CA1416未作范围外修改。
- [x] 最终源码NativeAOT/Host/UI/TestHost全部重发到 `.coding-agent/g02-bg-m942/`，配套runner已发布且实际执行安装，不留未发布执行器。
- [x] 沙箱task `22010d08ffc8436ea9623dfb1ebb8c1f` 三轮target-exit及总exitCode均0；已只回传并读取 `.coding-agent/g02-bg-m942-final-results/evidence/`。每轮真实多HIMC首EDIT切回Space START1/END1/Results=[你]，5项定位全PASS；另一会话先提交学习。
- [x] 首HIMC Host Space 0.038/0.051/0.093ms，原生入口到返回1.6220/1.8445/7.2639ms；n=3 nearest-rank原生P50=1.8445/P95=最大7.2639ms；500ms不变。逐次messages2/written2/return2，真实IMM加载与源/安装hash一致。
- [x] 本轮同步更新主交接、专门计划、词典规范、候选学习设计和closeout汇总。
- [ ] G02原有未覆盖矩阵仍独立开放：x86、多DPI/多屏、真实句柄复用、同HWND多HIMC、完整销毁重建、退出断连；三轮不等于真实长期饱和压力验证，也不等于完整Integration。

## 历史下一阶段：后台学习隔离输入延迟（当时文档合同已更新，实施未开始）

本节为用户最新授权的两阶段工作：**先更新文档，再另行委派实现**。本次仅完成第一阶段，不新增代码、测试或验证成功结论。权威合同见 [词典规范 5.5.1](../../../../Docs/dictionary-spec.md#551-后台学习产品合同待实施优先于历史同步学习描述) 与 [候选语义设计](../../../../Docs/candidate-prefix-unified-design.md)。下方 l831、d953 等是历史证据，保留原样；481.236ms 单次成功不等于学习阻塞已解决，G02 不关闭。

### 已核对的实际链路

`ImeContext.SelectCandidate` 校验 ContextId/revision/展示 lexicon，按已选 interpretation 消费并刷新剩串后调用学习回调；`CommitCandidate` 在 lexicon 变化时先刷新。Host 回调在输入请求内同步 `UserDictionary.Learn`（含规范词段注册验证及计数）和 `GetEntries()` 排序复制，再把完整快照交给保存协调器。该协调器已有容量 1 / DropOldest / 单消费者，但只隔离写盘，`Dispose` 仍同步等任务排空。学习推进 `_version` 后 `GetLexicon()` 会全量映射；解码索引即使复用 Projections/Shapes/Starts，仍求总频率、逐项验证 Text/CanonicalReading 并重建 Syllables/Aliases 键映射。不能把已有保存队列称为完整的后台学习，也不能只异步调用原 Learn 而保留下一按键重建链。

### 实施顺序（全部待后续委派执行）

- [x] 文档阶段：读取实际调用链和最新交接，更新后台学习、快照发布、候选安全及有限退出合同；保留历史批次证据。
- [ ] 1. 在现有 Core/Dictionary/Host 测试布局补确定性 red 用例，覆盖慢/暂停消费者、共享词典双会话及旧显式 ID；记录现有键盘 Space/序号与过期 ID 拒绝基线，不用固定睡眠替代阶段同步。
- [ ] 2. 在现有学习入口收敛最小不可变事件（优先 readonly record struct），固定 Channel 容量及明确 DropOldest/丢弃策略；生产者仅 TryWrite，不验证全词表、不取保存快照、不等待后台任务，不逐事件 Task.Run；保存准确的已选规范语义，不捕获可变会话。
- [ ] 3. 用 Host 管理的单消费者按有界批次/时间窗合并学习键与计数，保留词段贡献及溢出饱和；批量应用和持久化，限制合并表/待发布快照/重试，沿用旧文件保护；核对注册、Forget 等管理操作的顺序与确认语义，不把管理操作改成可丢学习。
- [ ] 4. 拆分频次/排序层与拼写拓扑；纯频次不重建拓扑。后台准备结构变化、词典快照及所需解码派生索引后发布；输入只取已准备版本或继续旧版本，不等待后台锁、不全表复制/校验/映射，不留前台惰性重建。限制旧版本生命周期，不按 profile 永久缓存。
- [ ] 5. 保留展示语义和真实提交：活动组合可继续旧快照；同一有效上下文/revision/快照内旧候选仍按原目标和消费范围提交，真正过期/跨上下文 ID 仍拒绝。分别验证显式 ID 与键盘路径；不能用放宽 ID 校验、重排旧序号、重置组合或改弱 END/text 断言修复。
- [ ] 6. 监督任务异常并区分容量丢弃/学习失败/保存失败；给正常退出固定收尾预算及取消路径，预算耗尽可丢未完成学习，不为排空或慢磁盘拖住退出；避免后台任务访问已释放 Host 状态。为容量、批次和退出预算设内部常量并记录实际值，不新增用户设置。
- [ ] 7. 先定向 green，再运行受影响 Core/Dictionary/Host 相关 Integration 与 Module 回归及 Release 构建，准确记录数量、失败/跳过和范围；验证原子保存、重启后已持久化排序及故障诊断。禁止把既有295/295当成本次实现验证。
- [ ] 8. 发布包含新 ImeCore/Dictionary/Host 的唯一配套负载，在 Windows 沙箱实际复跑多 HIMC 切回首 EDIT 后 Space（含另一会话学习/队列压力及重复切换）；核对真实 IMM 加载与 Host 配套哈希、START/END/Results、ProcessKey/ToAscii 消息和 Host/桥接耗时，保留全部失败及每次证据。读回后更新主交接；不延长500ms桥接截止，不凭单次通过宣称稳定或关闭 G02 全矩阵。

### 必须逐项签收的验收项（当前均未验证）

1. **入队非阻塞/容量有界**：消费者暂停、满队列及关闭时提交仍立即返回；队列、批次、合并表及临时快照不无限增长，生产者无学习/快照/I/O等待。
2. **批次合并/丢弃**：重复规范键合并次数正确、不同读音不混、分段不漏不重、饱和无溢出；丢弃可观察且不补偿成无界重试，未丢弃事件在正常运行中最终应用，不要求下一键立即生效。
3. **前台无学习重建**：慢学习/准备/写盘时键入及查询仍读旧完整快照；用可观察计数或等价确定性断言证明学习不在下一按键执行全表复制/索引构建/全量映射，纯频次不重建拼写拓扑，不能仅测入队耗时。
4. **旧候选安全提交**：活动旧快照中的有效候选按展示的原目标、canonical segments 和消费范围提交；真正过期或跨会话显式 ID 拒绝；双会话 Space/序号不失去提交和 END，不借频次重排偷换目标，不学习残串/替代路径。
5. **异常可观察/有限退出**：注入学习、准备、保存失败后可诊断且输入不失败；暂停消费者/慢持久化时退出不超过实现记录的固定预算，允许丢弃，旧正式文件不被破坏，不留下未观察故障或释放后回调。
6. **真实多 HIMC Space 延迟**：当前配套 Host 与原生负载真实 SendInput，重复切回及学习压力下精确 result/END/text 通过；报告逐次及汇总延迟（P50/P95/最大值、样本数）、是否逼近500ms，不用单次481.236ms历史结果代签。句柄复用、同HWND多HIMC、退出断连、x86、多DPI/多屏仍按 G02 原矩阵单独签收。


## 最新 l831 有界收口：当前 Host 配套后原生提交通过，完整矩阵仍开放

本节优先于下方历史批次。完整真实记录见[收口验证](../../../../.coding-agent/g02-closeout-validation.md)及[主交接](../../../../Docs/next-session-handoff.md)。

1. 已读取前轮候选过期修复、`ImeContext.DecodingIndex.cs` 学习后拓扑不变复用实现及 `.coding-agent/g02-space-l831/native` 发布成品。未改变旧显式ID拒绝合同，未延长500ms截止或放松精确提交/END/text断言。
2. 实际先跑 l831 原生配 k720 Host，task `0a792db9d2aa4b7ab4dfcec0e29525af`：首HIMC仍失败，START1/END0/Results=[]，Host Space700.185ms，模块超时返回0消息。证据 `.coding-agent/g02-space-l831-results/evidence/`。确认优化位于Host引用的ImeCore，单换原生不足以部署该优化。
3. 最小配套修正：重新发布当前Host至 `.coding-agent/g02-space-l831/host`，组装 `.coding-agent/g02-space-l831-currenthost/runner` 独立负载，保留 l831 原生及既有 k720 UI/TestHost/词典。首跑 task `4b19c1c4cef7487695a46da7336b98c9` 在读取候选状态文件时遇共享占用IOException，提前失败；原始证据 `.coding-agent/g02-space-l831-currenthost-results/evidence/` 保留，不当提交验证通过。
4. 同一负载重跑 task **34a25b52265045c58b522827aee5595c**：target与总结果exitCode均0。证据 **`.coding-agent/g02-space-l831-currenthost-r2-results/evidence/`** 已回读。首HIMC `START=1/END=1/Results=[你]`、CallbackError为空；第二HIMC START2/END2/Results=[你]；精确text断言保持。首Space Host481.236ms，ToAscii逐次记录handled=True、commit=你、messages=2/written=2/return=2；多HIMC及5项无按键候选定位PASS。
5. 真实IMM加载证据：DLL/IME/安装及system32已加载文件hash均 `4D5483A417276F7E9A38535E227297A8095A18B3A6D10E8882DA2EBB49A357CB`，LoadedByImmBeforeProbe=True。Host ImeCore hash `7C4074EAEFDA81B07E8358234562AD67428FFDD828B69AA2FE94031BA8AB2E12`，完整负载哈希在execution.json。没有混批替代或人工身份注入。
6. 最终源码Module全测226/226，Integration生命周期/候选66/66，Core新增双会话键盘Space/候选序号及旧显式ID拒绝3/3，合计295/295，0失败/0跳过。Release解决方案构建成功，3个既有Runner CA1416警告/0错误，覆盖受影响项目。未跑完整Integration/Core全测；本轮未重发NativeAOT，使用既有l831成品。
7. 本轮有界验证和文档三项已完成，无新增产品源码改动。481.236ms距500ms裕量小，单次通过不保证稳定；候选状态文件共享冲突一次失败尚未修复。仅x64/96DPI单屏，不外推x86、多DPI/多屏、真实句柄复用、同HWND多HIMC、完整销毁重建及退出断连。G02完整矩阵仍开放；历史run10未知与j619/k720失败不改写。

## 最新d953实际验收：身份修复通过，多HIMC提交失败，G02开放

本节覆盖下方历史“身份发布仍超时/未回归”结论。原始证据：`.coding-agent/g02-fresh-20261020-d953-evidence/evidence/`；完整执行记录在工作区`.coding-agent/g02-closeout-validation.md`。

1. 本轮实际先跑Module全测226/226、Integration生命周期及Candidate定向66/66，均0失败/0跳过；Release解决方案构建成功（22警告/0错误）。未跑完整Integration。精简Runner发布成功，新增G02NativeRoot原生覆盖配置，排除负载PDB；d953原生配c842 Host/UI/TestHost与词典。
2. b731/c842原始stderr均为actual native identity publication超时；c842真实IMM生命周期在ImeSelect时已有对应焦点，却没有初始active=1 export。d953保留WM_IME_SETCONTEXT及焦点匹配的选择期激活补偿，不假定选择必然触发active=1，不放宽断言、不手工IPC注入。
3. d953实际沙箱task=`d69453d7726f43fc8351f3c922dce21f`，只回传evidence成功。DLL/IME/安装文件及实际system32模块hash=`5AD14F7472A33665062891E86FF63AF27A8E3136CF754D95B7F92EADC67941C1`，LoadedByImmBeforeProbe=True。整体exitCode=1，不是未知。
4. GUID身份真实发布通过：owner=158a4fb35ff84fcbbc59ec99ce64c55f；单上下文fe4998bc2647437d9d92e7608adf7edc；多上下文4c6303c4fc1d495fab0d6ecfc65bdb00 / 37229880ae214fde99e7dd7930fc3c01，窗口属性世代2/3/4。真实SendInput xx提交通过，ProcessKey4/handled2、ToAscii2/handled2。组合生命周期START2/END2/Results=[你]通过。
5. 真实候选窗无按键first/second/back切换、移动、工作区边界共5项PASS（96 DPI单屏），有POSITION-EVIDENCE与最终match=True。不能因中间等待样本match=False判最终失败，也不抹除其时序记录。
6. 新阻塞：real-ime-multi-himc-isolation在first HIMC Space result/END/text超时，START1/END0、Results=[]、Updates=[n,ni]，原始堆栈见target.stderr.txt。下一步应跟踪切回首HIMC后的真实Space ProcessKey/ToAscii/IMM消息链和提交状态，先证实根因再修复，不用重置组合或改弱合同规避问题。任何后续产品修正须新唯一负载及真实沙箱回归。
7. G02不关闭。句柄复用没有发生、未覆盖；同HWND多HIMC、完整销毁重建、退出断连、x86、多DPI/多屏继续未覆盖，不外推。历史run10未知仍独立保留。本轮无新增未回归产品代码，未改本机系统。

## G02 最新有界收口：托管验证成功，原生未通过，run10未知

本节覆盖下方历史批次的“本轮未发布/未运行”等表述；G02保持开放。详见 [本轮执行记录](../../../../.coding-agent/g02-closeout-validation.md) 与 [最新主交接](../../../../Docs/next-session-handoff.md)。

### 本轮实际尝试及结果

1. 先读取实际源码、资源、发布目标及run10本地证据；发现目录混批，不能按目录名给报告归属。外层 `evidence/execution.json` 属任务 `5c74ddf9fec34fb0b73f8ddf7b312135`、hash `7EC389ABE76ED76362DF2C0E09577A08F96CE3125009F8A3E495CD01CEA0ED4C`。安装返回非零 `0xFFFFFFFFE0230804` 同时 error1419，非零返回时不能用残留last-error判失败；该次 `target.stderr.txt` 是实际原生身份发布超时，START/END=0，仍不是加载闭环。
2. 内层 `evidence/evidence/execution.json` 属任务 `d8970a5c27ad439f81b2a04020e4f9d6`，hash `BA03F8F30314968D87A6BABCA7B02E415C98E88A5DF8AC9DCDA70A9AD0B77FD3`，随机 `XiaoXiG02_*.ime`，安装返回0/error1419/exit1，另有 `failure.txt`。它不是正式资源文件名run10结果。
3. 用户前序run01–10均未形成当前新IME闭环，run08/09新NativeAOT hash为7EC389...，1419安装阻塞未解决；run10改正式资源文件名已执行，但回传磁盘不足，结果未知。这些是前序记录，不冒充本轮重跑。
4. 新增只读 `G02EvidenceRecovery.cs` 和 `g02-recover-evidence` 分发，两次发布成功。首次扫描层级错误，修正后第二次真实沙箱扫描 `C:\CodingAgentSandbox\Tasks`，records为空，无法恢复历史任务最小报告/stdout/stderr。仅指定 `outputRelativePath=recovery`；最终证据 `.coding-agent/g02-closeout-recovered-final/recovery/recovery.json`。不以本地混批旧证据替代run10，不无限新增大包回传。未删本机或沙箱文件。
5. 当前Module、Host、IntegrationTestHost、SandboxRunner分别Release build均成功；Module全测 **226/226**，InputTargetLifecycleTests + CandidateAnchorTests **30/30**，均0失败/0跳过。没有跑完整Integration。
6. 为核实发布复制风险，当前Module实际Release win-x64 NativeAOT publish至 `.coding-agent/g02-closeout-native`，退出0。`CreateImeModule` 的 `TargetDir/native` 复制仅在Publish后且RID非空执行，当前普通build和发布均成功，未修改该Target；未独立比对复制后哈希/PE，未安装本次新成品，不作正式安装包通过声明。

### 明确结论及未覆盖场景

- **成功**：上述当前构建、托管回归、诊断发布执行及小目录回传。
- **失败/仍阻塞**：前序返回0的1419安装、实际原生身份发布超时均未解决；旧负载不能当本轮通过，G02不通过、不关闭。
- **未知**：正式资源文件名run10远端结果本轮无法恢复，磁盘回传失败不能推断产品成功或失败。
- **未覆盖**：当前新IME真实IMM32/UI全链路、真实HWND/HIMC销毁重建与复用、同HWND多HIMC通知顺序、无按键激活刷新、窗口属性/GUID实机、断连/退出清理、x86、多DPI/多屏。托管模拟不能补签，不扩TSF。
- 环境无可用子智能体探索/编码入口，本轮实际由当前代理完成；未宣称满足子智能体执行要求。产品实现未改，仅新增只读回收诊断入口。

## 1. 目标

本计划的发布状态以 [主交接第 4.8 节](../../../../Docs/next-session-handoff.md#48-统一发布差距清单) 为准。第二轮真实证据已回读归档，本次未重新执行；下文设计建议不等于已批准的应用矩阵。

补齐以下输入目标/候选定位能力（已有结构、会话和 UI 基础，不从零重建）：

1. 从 `HIMC` 对应的 Win32 输入上下文中取得正确的目标窗口 `HWND`，用于：
   - 采集目标窗口诊断及定位快照；原生 `TRANSMSG` 没有 HWND 字段，不改其 ABI；
   - 标识本次输入所属的窗口和输入会话；
   - 通过 IPC 向 Host/UI 提供必要的目标窗口上下文。
2. 从 Win32 输入上下文中取得候选窗口锚点，将有效的屏幕坐标写入 `ImeUiState.AnchorX/AnchorY`，使独立候选窗口能够跟随应用插入符或应用指定的候选位置。

## G02 收尾修复：当前工作区托管验证通过，原生验收仍开放

本节覆盖下方旧批次对“未授权身份开发／尚无独立激活／仅修改 reader”的当前状态描述；旧记录仅保留历史含义。用户已授权候选定位及身份开发。本轮接续两轮中间修改，未回退已有实现。

### 已核对的实现链路

- IMM 私有块 `ContextIdentity` GUID 经 `ImeInputIdentity.Resolve` 生成会话身份；写回私有块时保留 GUID。窗口属性由实际窗口对象保存世代，Anchor 携带属性名和值，Presenter 在定位前再次校验 HWND/PID/TID/属性身份。
- `ImeSetActiveContextManaged` 独立发布激活/失活观察，`NI_CONTEXTUPDATED` 经 `RefreshInputTarget` 刷新已激活上下文；失活不重置组合、不等于销毁。Host 按 Owner 序列拒绝旧观察，同 ContextKey 更换会话身份时退役旧会话，拒绝其激活、按键和组合请求。
- `ClientExclusion` 经 IPC 传输，在 Presenter 转换完整客户区矩形至屏幕坐标，再由 Placement 尝试下方、上方和左右避让。组合区域不再误读 CurrentPosition；初始化及来源有效性仍有门控。
- 本轮移除 `Resolve` 中入口已排除非原生访问器后仍重复判断 `UsesNativeContext` 的冗余分支；未删除身份保护，未扩大 TSF 或 DPI 范围。

### 本轮修复与实际验证

1. 实际复现转换列表 3 个失败：首次定向运行 10 通过、3 失败，共 13；均在 required > 0 断言失败。根因是测试使用虚拟 HIMC=1 而未注入内存上下文，身份解析进入原生访问器；不是生产身份校验应被绕过。
2. `ImeExportsTests.cs` 的三个转换列表测试注入 `InMemoryImmContext(1)`，finally 恢复访问器，保留候选内容、长度及不写目的缓冲等原断言。修后定向运行 13/13，通过、无跳过。既有 `Marshal.SizeOf<ImePrivateData>()` 断言已实际运行通过。
3. Module 全测：226/226，通过，0 失败、0 跳过（当前测试清单；不沿用旧批次 227 或更旧计数）。
4. Integration 的 `InputTargetLifecycleTests` + `CandidateAnchorTests`：30/30，通过，0 失败、0 跳过；包含已改为 `Composition.Reading` 的失活再激活保留组合断言。未运行完整 Integration，不作整项目通过声明。
5. Module、Host、ImeUi.Avalonia 三个项目分别 Release 构建，均退出 0，各 0 警告、0 错误。此为托管构建，不是 Native AOT 发布或正式安装包验证。

### 尚未执行的真实验收

本轮没有发布或安装原生 IME，没有执行 Windows 真实 IMM32/UI 全链路、x86 ABI/私有块布局、真实 HWND/HIMC 销毁重建与数值复用、同 HWND 多 HIMC 激活通知顺序、无新按键激活/刷新、窗口世代属性与原生私有块身份的实机验收。托管测试只证明模拟合同，不证明这些原生场景通过。断连/进程退出后的清理合同仍需真实验收，不由序列拒绝测试代替。多 DPI/缩放/多屏继续后置，不扩 TSF；G02 不关闭。

## 历史：源码调查与最小修复（G02 仍开放）

本节是当前源码核对及本次实际执行结果；下方“第二轮”“阶段一”和第 2 节以后的原始设计保留为历史背景，不应据旧的“当前”描述重复判定功能缺失。

### 原状态与证据

- 定位主链路已有实现，并非从未实现：`ImeModuleRuntime.ProcessKey` 调用 `ImeCandidateAnchorReader.Read`，通过 Bridge/可选 Anchor IPC 传递；`ImeHostService.ProcessKeyAsync/GetUiStateAsync` 按 SessionId 缓存、校验并返回锚点；`CandidateWindowPresenter` 查询焦点、重新校验窗口/PID/TID、转换客户区坐标并定位 UI。
- `ImeSessionId.FromHimc` 当前实际格式是 `himc:<pid>:<handle>`；旧设计中无 PID 的描述已过时。Host 对同 HWND/PID 匹配多个会话时保守隐藏，不等于已解决激活身份。
- 明确实现缺口：原 reader 未检查 `FdwInit`，并允许 `COMPOSITIONFORM.Style == CFS_RECT` 时读取 `CurrentPosition`，可能把未初始化值或非点位字段当作有效来源。
- Windows SDK 10.0.26100.0 `um/immdev.h` 1019–1025 行确认 `INIT_COMPFORM=0x10`，没有候选窗初始化位；`um/imm.h` 621–627 行确认样式值。不能把组合窗初始化位套用于候选窗。

### 本次修复范围

仅修改 `ImeModule/ImeCandidateAnchorReader.cs`、新增 `ImeModule.Tests/ImeCandidateAnchorReaderTests.cs` 及本计划：

1. 组合窗回退必须设置 `INIT_COMPFORM`，且仅接受 `CFS_POINT`/`CFS_FORCE_POSITION`。`CFS_RECT` 是排版矩形，不从其 `CurrentPosition` 虚构点位；无可用候选窗/caret 时沿用 unavailable 路径。
2. 提取纯快照解析，保留候选列表 0、POSITION 优先、caret 次之、composition 最后的既有顺序；零值及负坐标不作无效判断。
3. 保留既有 EXCLUDE 的 CurrentPosition.X、Area.Bottom/Top 定位约定，拒绝倒置排除矩形。此为快照有效性防护，不宣称完整矩形避让合同已验收。
4. 未改变 IPC、三字段 TRANSMSG ABI、会话格式、PMv2 配置或 TSF；未修改全局交接文档。

### 本次实际验证

- 新增来源解析测试：15/15，通过，0 失败/0 跳过。
- ImeModule.Tests 项目全测：227/227，通过，0 失败/0 跳过（当前数量，不沿用历史 148）。
- IntegrationTests 的 CandidateAnchorTests 定向测试：19/19，通过，0 失败/0 跳过。
- IntegrationTests 项目全测调用超过工具默认 5 分钟，被终止；没有完整结果，不记作通过或断言某个用例失败。随后定向测试成功。
- ImeModule 项目 Release 构建：退出 0，0 警告/0 错误；不是解决方案构建、Native AOT 发布或 x86 验收。
- 没有重跑 Windows 安装/真实 IME 全链路。当前工作区未找到下文引用的 `.coding-agent/imm32-validation/g02-real-run-02-review.md`（按名称搜索也未找到），故第二轮真实结果仅作为计划内历史记录保留，本次无法重新回读其原始证据。

### 尚未闭环与规格边界

- 真正 HWND/HIMC 销毁及句柄复用/世代没有完成：PID+handle 不是世代标识；`ImeSelectManaged(select=0)` 有重置，但 `ImeSetActiveContextManaged` 仅记录活动切换、不传播活动会话。
- 同 HWND 多 HIMC 的当前策略仍是歧义隐藏；新激活无缓存锚点也没有独立采集/传播路径（当前仅按键采集）。这些是实际能力缺口，不能用已有双 EDIT 不同 HWND 的焦点回切测试替代。
- 进一步实现前须确定跨进程活动身份的权威来源、失效/断连清理合同、世代开始/结束边界（不能简单把暂时失活或取消组合等同 HIMC 销毁），以及同 HWND 重关联时的原生通知顺序和真实验证宿主。现有计划是演进建议，没有确定上述合同；本次不擅自改变全局会话协议或猜测生命周期。
- CFS_RECT 如何从允许排版区域生成候选点、EXCLUDE 的完整矩形避让及真实应用行为仍需原生合同/真实测试补齐。本次保守拒绝非点位来源，不等同完成这些布局能力。
- 基本客户区到屏幕转换仍沿用现有 API，未扩展 DPI 专项。按用户决定继续后置多 DPI/缩放/多屏与跨屏；不扩展 TSF。
- 下一阶段真实 IME 验收必须覆盖本次初始化门控的回退结果、真正销毁/重建/复用、同 HWND 多 HIMC 激活、新激活无缓存锚点，且保留候选实际矩形/目标关联/提交取消回归。单元测试不能将 G02 标记关闭。

## 第二轮实际证据与剩余范围（G02 未关闭）

已完成 [第二轮逐项回读归档](../../../../.coding-agent/imm32-validation/g02-real-run-02-review.md)。原始路径 `.coding-agent/imm32-validation/g02-real-run-02/final-evidence`，x64管理员任务 `346c21d6abff471f9f4a0d447d4c1cfa`。xx、单HIMC取消/重组/提交、多HIMC交错均PASS；真实候选窗口双EDIT位置、无新按键回切、窗口移动、工作区边缘分别PASS，非首轮exit0推断。五项均DPI=96/scaling=1，实际rect依次 `(130,204,646,293)`、`(130,249,646,338)`、`(130,204,646,293)`、`(260,284,776,373)`、`(1404,921,1920,1010)`，工作区 `(0,0,1920,1032)`；展开日志112–127行及冻结宿主476–487/677–710行固定断言边界。

已扩展真实宿主/runner并组装发布负载，冻结源码身份与发布清单对应；系统实际加载SHA=`CE79122E014152350ED842094D88367999D9C7E71D932F0E6EA3A20DE6DC31B7`，安装源/manifest一致。两HIMC销毁、UI主动退出与布局/IME清理有原始记录；无全系统残留进程枚举或截图证据。本次仅证据回读/文档编辑，无代码追加、重跑或扩大多屏。

既有CandidateAnchorTests19/19、Integration76/76；本轮增量0警告/0错误与历史非增量18 CA1416 + 1 CS8604（19警告）/0错误严格区分。阶段一模块148/148、IPC27/27及更旧测试不计作第二轮重跑；解决方案exit0只有还原输出不计全测试。

FdwInit初始化/来源有效性、COMPOSITIONFORM/CFS_EXCLUDE合同、坐标单位/多DPI、句柄真正销毁复用/世代、同HWND多HIMC激活身份及无缓存锚点新激活仍是发布合同/验收缺口及本批未覆盖风险，不能伪称完成或确定失败。多DPI/多屏视觉归G03，未批准扩展环境未执行。当前限定场景无失败，不需要定向修复；只闭环本批断言，G02整体保持开放。

## 本阶段用户决定与接续顺序（优先于下方历史实施顺序）

1. **本阶段跳过/后置 DPI 专项问题与验证，不继续推进多 DPI、多缩放比例视觉、多显示器或跨屏实验。** 用户理由是预期设置 PMv2 后正常；该预期不作为验证证据。实测仍仅单屏 96 DPI、scaling=1，不标记 DPI 专项通过。
2. 下一步先核对原生锚点 `FdwInit` 初始化/来源有效性、`COMPOSITIONFORM` / `CFS_EXCLUDE` SDK 合同，保留基本正确的客户区/屏幕坐标及单位换算要求；跳过专项不等于跳过基本坐标正确性。
3. 随后推进窗口/会话生命周期、真正 HWND/HIMC 销毁与句柄复用/世代、同 HWND 多 HIMC 激活身份、新激活无缓存锚点。下方第 12.6 节多 DPI/跨屏矩阵及第 13 节相关专项工作本阶段不执行，原始设计/后续验收要求保留。
4. G02/G03 保持开放，主交接 14 项未闭环及 A=1/B=7/C=6 计数不变。本次仅阶段优先级调整，不是永久排除发布范围，也未要求改变 PMv2 配置。

PMv2 只读核对：UI `Program.cs` 使用 `UsePlatformDetect()`，项目引用 Avalonia.Desktop 11.3.7；已读取入口、App 和项目文件，未见显式 PMv2 选项、DPI awareness API 调用或 ApplicationManifest 配置，`XiaoXiIme/src` 文本检索未命中显式 PMv2 标识。只能确认所核对文件未显式设置，不能推断框架默认/运行时是否启用 PMv2；未改配置、未作运行时验证。

本次只更新本计划和主交接；已读取第二轮回读归档及阶段结果，未修改原始验证报告、业务/测试代码，未构建、发布或运行测试。

## 阶段一历史进展（不代表第二轮未执行）

已阅读当前上一轮会话限定与 exclude 实现；本阶段保留 `himc:<pid>:<handle>`、`CFS_EXCLUDE` CurrentPosition.X 及三字段 TRANSMSG ABI。新增可选焦点 HWND/PID UI 查询；Host 在已有有效锚点中唯一匹配，显式会话优先，未知或歧义焦点返回空候选。Windows Presenter 查询前台 GUI 焦点，使有缓存锚点的会话能无新按键回切。新增四项测试覆盖回切、PID 不符、显式查询优先及歧义目标隐藏。

最终定向 CandidateAnchorTests 19/19；IntegrationTests 项目全部 76/76；ImeModule.Tests 全部 148/148；ImeIpc.Tests 全部 27/27；均 0 失败/0 跳过。Release 解决方案成功，历史非增量 18 个 CA1416 + 1 个 CS8604（共19警告）、0 错误；本轮增量0警告不否定该历史边界。解决方案测试调用虽退出 0，但只有还原日志，不能计为全测试通过。测试初次 CS0121 已通过显式请求类型修复，最终验证后未再改业务代码。

未完成：真实宿主与 runner 扩展、专用发布负载、Windows xx/单 HIMC/多 HIMC 回归、实际候选 UI 位置/EDIT 关联/切换/移动/边界断言、模块身份归档、截图及清理；真实 DPI/多屏覆盖为零。句柄失效与真正 HIMC 销毁/复用必须区分，当前仍未闭环；同 HWND 多 HIMC 的焦点查询歧义保守隐藏，尚须原生激活身份合同；新激活且未缓存锚点的场景还需采集路径。锚点初始化有效性、composition 来源及 exclude 坐标单位必须继续核对原生合同，不能以本次托管通过代替。

准确结果与未覆盖范围见 [阶段结果](../../../../.coding-agent/imm32-validation/g02-phase1-results.md)。下文是原始设计基线/建议，不能把旧描述当当前实现盘点或完成证据。

## 2. 当前实现基线

### 2.1 输入目标 HWND

当前 `ImeToAsciiExManaged` 已接收 `HIMC inputContext`，但处理流程为：

1. 将 `virtualKey` 转换为 `ImeKey`。
2. 通过 IPC 获取 `ImeProcessResult`。
3. 把结果写回 `HIMC`。
4. 调用 `ImeTransMsgBuilder.BuildMessages(result)`。

`ImeTransMsgBuilder.BuildMessages` 当前接受结果及内部 `wasComposing` 参数，没有 `hwnd` 参数。`TransMsg` 仅有 `Message/WParam/LParam`，符合当前 IMM32 消息结构；Windows 通过当前 HIMC 调用链分发消息，不应添加 `TRANSMSG.Hwnd`。

当前 `InputContext` 已有 `Hwnd`、组合窗、候选窗数组、组合/候选/消息缓冲及保留字段，没有 `HWndIme`。`ImeExportsTests.InputContext_MatchesNativeLayout` 和 `TransMsgTypes_MatchNativeLayout` 已有大小/关键偏移断言，按运行进程架构选择期望值；测试代码存在不证明本次已运行 x86/x64 或 Native AOT 布局复验。读取定位字段前仍需核对相关字段偏移及坐标语义，不再将“结构仅有部分字段”作为当前缺陷。

### 2.2 候选窗口锚点

当前 `ImeUiState` 已包含：

- `AnchorX`
- `AnchorY`

但 `ImeUiState.FromSnapshot` 不接收锚点，默认返回 `(0, 0)`。Host 当前已通过 `Dictionary<ImeSessionId, ImeContext>` 隔离上下文，并解析指定/最近活动会话；`GetUiStateAsync` 仍用不带锚点的 `FromSnapshot`。尚未见 Win32 Target/Anchor 的采集与 IPC 传播链路。

Avalonia UI 已将 `ImeUiState.AnchorX/AnchorY` 映射到 `CandidateWindowViewState`，因此 UI 状态模型已经具备最小消费入口，但尚未定义：

- 坐标是否有效；
- 坐标系是客户区还是屏幕；
- 锚点代表插入符左上、左下还是排除矩形；
- 目标显示器和 DPI；
- 锚点不可用时的降级行为。

## 3. 设计原则

### 3.1 Win32 边界留在 ImeModule

`HWND`、`HIMC`、`CANDIDATEFORM`、`COMPOSITIONFORM`、客户区坐标和屏幕坐标都属于 Win32 平台细节，应由 `XiaoXiIme.ImeModule`/`XiaoXiIme.ImeInterop` 读取和归一化。

IPC 不应直接传输可供远端解引用的指针，也不应要求 Host 锁定 `HIMC`。Host/UI 只接收稳定的值类型快照。

### 3.2 IPC 使用结构化上下文

不要只给 `ImeProcessKeyRequest` 增加一个 `long Hwnd`。应建立结构化输入上下文，至少区分：

- 输入会话身份；
- 目标窗口句柄；
- 目标进程/线程；
- 候选锚点；
- 坐标有效性和来源。

这样可以为后续多窗口、多输入框、DPI 和会话隔离留出扩展空间。

### 3.3 IPC 统一使用屏幕物理像素坐标

建议跨进程传输时统一使用 Win32 屏幕坐标，并明确单位为物理像素：

- ImeModule 负责把客户区坐标转换为屏幕坐标；
- Host 不进行窗口坐标转换；
- UI 在最终定位窗口时，根据 Avalonia 当前使用的坐标单位和显示器缩放完成物理像素到 UI 坐标的转换。

不要仅传 `AnchorX/AnchorY` 而不标明坐标系，否则在非零窗口原点和多显示器环境中会产生位置错误。

### 3.4 HWND 只作为瞬时上下文

`HWND` 可以用于当前调用和诊断，但窗口句柄可能被系统复用。长期会话身份应由 ImeModule 生成的 `SessionId` 或由组合生命周期管理的标识承担。

## 4. 建议的数据模型

具体命名可在实现时按项目风格调整，但模型职责应保持稳定。

### 4.1 输入会话标识

复用已有不可变 `ImeSessionId`，不要另建重复的 `ImeInputSessionId`。现有 `FromHimc` 生成 `himc:<句柄>` 字符串，跨进程身份冲突、句柄复用和会话回收语义仍待核对；下列内容是演进建议，不是现状。

建议内容：

- 保留现有字符串 `Value` 合同；若需强化身份，先核对跨进程唯一性与协议兼容，不直接改成新的数值/Guid 模型；
- JSON 可稳定序列化；
- 不包含进程内指针；
- 同一活动组合会话保持不变，组合结束后可释放；
- 目标窗口、线程或 `HIMC` 发生变化时创建新会话。

会话标识已在契约及 Host 中使用；定位增量应复用既有合同并核对身份及回收，不新增平行会话体系。

### 4.2 Win32 输入目标快照

新增 `ImeInputTarget` 或等价模型，建议包含：

- `ImeSessionId SessionId`
- `long WindowHandle`
- `int ProcessId`
- `int ThreadId`
- `bool IsWindowValid`

说明：

- `WindowHandle` 使用固定宽度整数进行 JSON 传输，不在 Host/UI 中转换为可操作指针，除非明确调用 Win32 API。
- `IsWindowValid` 表示采集时 `HWND != 0` 且通过 `IsWindow` 校验；它不是永久有效承诺。
- `ProcessId`、`ThreadId` 通过 `GetWindowThreadProcessId` 获取，用于诊断、会话区分和句柄复用防护。

### 4.3 候选锚点快照

新增 `ImeCandidateAnchor` 或等价模型，建议包含：

- `bool IsValid`
- `int X`
- `int Y`
- `ImeCoordinateSpace CoordinateSpace`
- `ImeCandidateAnchorKind Kind`
- `ImeCandidateAnchorSource Source`
- 可选 `ImeScreenRectangle ExclusionArea`

建议枚举：

- `ImeCoordinateSpace.ScreenPhysicalPixels`
- `ImeCandidateAnchorKind.CaretBottomLeft`
- `ImeCandidateAnchorKind.CandidatePosition`
- `ImeCandidateAnchorKind.ExclusionAreaBottomLeft`
- `ImeCandidateAnchorSource.CandidateForm`
- `ImeCandidateAnchorSource.CompositionForm`
- `ImeCandidateAnchorSource.Caret`
- `ImeCandidateAnchorSource.Fallback`

如果读取到 `CFS_EXCLUDE`，仅传一个点会损失信息，因此建议同时传排除矩形。UI 可优先显示在排除矩形下方，若屏幕空间不足则翻转到上方。

### 4.4 IPC 请求上下文

现有 `ImeProcessKeyRequest` 已包含 `Key`、`SessionId`、`Generation`、`SequenceNumber`。在保留会话/时序合同的基础上，建议扩展 Target/Anchor 上下文，而不是从只有 Key 的旧基线重建：

- `ImeKey Key`
- `ImeInputTarget Target`
- `ImeCandidateAnchor CandidateAnchor`

为控制协议演进风险，可选择以下方式之一：

1. 直接扩展现有 record 构造参数，并同步更新所有调用和测试；
2. 新增 `ImeInputContextSnapshot Context`，把 Target、Anchor 和后续修饰键状态集中放入；
3. 新增 V2 路由，保留旧路由一段兼容期。

若核对后确认没有稳定协议兼容要求，可考虑第二种方式；外部消费者与协议兼容状态尚待确认，不能凭本计划假定。

### 4.5 UI 状态

将 `ImeUiState.AnchorX/AnchorY` 替换或补充为结构化 `CandidateAnchor`。

过渡方案：

- 保留 `AnchorX/AnchorY`，由 `CandidateAnchor` 派生；
- 新增 `AnchorIsValid`、`AnchorKind`、`CoordinateSpace`；
- UI 完成迁移后再考虑删除裸坐标字段。

若不需要兼容旧序列化数据，可以直接用 `ImeCandidateAnchor CandidateAnchor` 替代两个整数。

## 5. Win32 Interop 开发细节

### 5.1 校验并补全原生结构布局

在读取 `InputContext.Hwnd`、候选窗位置前，先对照当前目标 Windows SDK 的 `INPUTCONTEXT`、`CANDIDATEFORM`、`COMPOSITIONFORM`、`POINT`、`RECT`、`LOGFONTW` 定义。

需要完成：

1. 为所有结构显式使用 `StructLayout(LayoutKind.Sequential)`。
2. 使用与原生字段一致的类型和顺序。
3. 正确表达固定长度候选窗数组。
4. 检查 x64 下 `BOOL`、`DWORD`、句柄和指针字段的宽度及对齐。
5. 增加 `Unsafe.SizeOf<T>()`/`Marshal.OffsetOf<T>()` 测试，对关键字段偏移进行断言。
6. 确认 Native AOT 下结构布局与普通 JIT 一致。

当前布局修复及核心偏移测试已有实现。后续应复用并扩展这些断言，对定位相关字段、x86/x64 实际执行及 Native AOT 做复验；本次没有重新比对本机 SDK 或运行原生探针，不声称所有字段已独立验证。

### 5.2 扩展 Win32 API 声明

按最终采集方案增加必要的 P/Invoke：

- `IsWindow`
- `GetWindowThreadProcessId`
- `ClientToScreen`
- `GetGUIThreadInfo`
- 可选 `GetCaretPos`
- 可选 `ImmGetCandidateWindow`
- 可选 `ImmGetCompositionWindow`

优先从已锁定的原生 `INPUTCONTEXT` 读取 IME 内部数据；公共 IMM API 可作为校验或降级路径。所有 P/Invoke 应：

- 使用精确入口名；
- 指定返回值布尔封送；
- 不在 Host/UI 层重复声明；
- 用可替换的访问器接口封装，便于单元测试。

### 5.3 扩展上下文访问器

避免让 `ImeExports` 直接散落 Win32 调用。建议新增：

- `IImeInputContextReader`
- `ImeInputContextReader`
- `IWin32WindowContextAccessor`

职责划分：

- `ImeInputContextReader`：锁定 `HIMC`，读取目标 `HWND`、候选窗/组合窗信息。
- `IWin32WindowContextAccessor`：校验窗口、查询 PID/TID、执行坐标转换和读取 caret。
- 现有 `IImmContextAccessor`：继续负责 IMM 锁定、解锁、内存调整和消息生成。

读取失败必须返回无效快照，而不是抛出异常穿过非托管导出边界。

## 6. 目标 HWND 实现方案

### 6.1 读取流程

在 `ImeToAsciiExManaged` 中，处理按键前读取一次输入上下文快照：

1. 校验 `inputContext != 0`。
2. 调用 `ImmLockIMC`。
3. 从正确布局的 `INPUTCONTEXT.hWnd` 读取应用目标窗口。
4. 在解锁前复制所有需要的数据，禁止保存原生结构指针。
5. 调用 `IsWindow` 验证句柄。
6. 调用 `GetWindowThreadProcessId` 获取线程和进程标识。
7. 生成或解析当前 `SessionId`。
8. 在 `finally` 中调用 `ImmUnlockIMC`。

`hWnd` 为 `0`、窗口已失效或读取失败时：

- IPC 仍可发送按键；
- Target 标记为无效；
- 保持原生 `TRANSMSG` 三字段结构，不添加窗口句柄；
- 不因位置采集失败丢失输入功能。

### 6.2 保持 TRANSMSG ABI

不向 `ImeTransMsgBuilder` 或 `TRANSMSG` 添加 HWND。目标窗口快照只用于 IPC 定位和诊断；返回消息仍只有 message、wParam、lParam，由 Windows 当前 HIMC 链路分发。复用现有结构布局测试及组合/提交/结束消息分支测试，真实行为由生命周期及多 HIMC 场景验收。

### 6.3 与 HIMC 写回的一致性

读取 Target 和写入 composition context 应针对同一个 `HIMC`。不要仅依赖 IPC 响应中返回的窗口句柄覆盖当前 Win32 调用的目标，因为 IPC 响应可能延迟，且窗口可能在请求期间切换。

建议规则：

- Win32 写回使用本次调用的 HIMC，Target 只作为同次采集的定位/诊断快照；
- IPC 返回只负责输入法语义结果；
- Host 不允许修改 Win32 目标窗口。

## 7. 候选窗口锚点实现方案

### 7.1 位置来源优先级

建议按以下优先级采集：

1. 应用在输入上下文中指定的候选窗位置 `CANDIDATEFORM`。
2. 应用指定的组合窗位置 `COMPOSITIONFORM`。
3. 目标 GUI 线程当前 caret 矩形。
4. 目标窗口客户区中的安全默认点。
5. 无效锚点。

必须记录 `Source`，便于 UI 选择定位策略和排查应用兼容问题。

### 7.2 CANDIDATEFORM 处理

读取候选窗索引 `0` 作为当前第一候选列表的位置。根据样式处理：

- `CFS_CANDIDATEPOS`：使用 `ptCurrentPos`。
- `CFS_EXCLUDE`：优先使用 `rcArea` 作为排除区域，锚点取排除区域左下角。
- 其他不支持或未初始化样式：进入下一降级来源。

候选窗坐标通常需要结合目标窗口客户区解释。实现时必须通过 Windows SDK 文档和实际测试确认每种样式的坐标空间，再统一调用 `ClientToScreen` 转为屏幕物理像素。

不要把尚未确认坐标系的原始值直接写入 `AnchorX/AnchorY`。

### 7.3 COMPOSITIONFORM 处理

若候选窗信息无效，读取组合窗位置：

- `CFS_POINT`/`CFS_FORCE_POSITION`：使用当前点；
- `CFS_RECT`：使用矩形左下角或按排除区域策略处理；
- 未初始化或不支持样式：继续降级。

组合窗位置用于候选窗口跟随时，应将 `Kind` 标记为组合位置来源，避免 UI 将其误认为精确 caret 矩形。

### 7.4 Caret 降级

当输入上下文没有有效候选/组合位置时：

1. 使用 Target 的线程 ID 调用 `GetGUIThreadInfo`。
2. 读取当前 caret 所属 `hwndCaret` 和 `rcCaret`。
3. 若 `hwndCaret` 有效，将 caret 客户区矩形转换到屏幕坐标。
4. 锚点使用 caret 左下角，使候选窗出现在文本插入位置下方。
5. 如果 caret HWND 与 Target 不同，保留实际 caret HWND 作为诊断信息，或至少记录来源。

不建议跨线程直接依赖全局 `GetCaretPos` 作为唯一来源，因为它对调用线程和 GUI 线程状态有约束。

### 7.5 坐标转换与 DPI

ImeModule 输出屏幕物理像素后，UI 定位时需要：

1. 根据锚点所在显示器选择 Avalonia `Screen`。
2. 将物理像素转换为 Avalonia 使用的坐标单位。
3. 对候选窗尺寸执行工作区边界约束。
4. 下方空间不足时显示在排除矩形或 caret 上方。
5. 多显示器存在负坐标时不得把坐标钳制到 `0`。
6. 跨显示器或 DPI 变化后重新计算，不缓存永久转换结果。

如果 Avalonia 窗口定位 API 本身接受物理像素，应在实现前通过一个最小验证测试确认，避免重复缩放。

## 8. IPC 与 Host 开发细节

### 8.1 扩展序列化上下文

所有新增 DTO 和枚举必须加入 `XiaoXiImeIpcJsonSerializerContext`，确保 Native AOT 下可序列化。

新增测试应覆盖：

- 有效 HWND 和 PID/TID；
- 无效 Target；
- 有效候选点；
- `CFS_EXCLUDE` 对应的排除矩形；
- 负屏幕坐标；
- `SessionId` 往返一致。

### 8.2 Host 状态管理

Host 已有按 `ImeSessionId` 隔离的上下文和指定会话查询。定位增量应复用现有隔离，在各会话保存 Target/Anchor，不能回退为全局最近一个输入上下文。下列清单是定位增量/回收语义的建议，不意味着现有会话处理缺失。

但为了避免窗口 A 的候选状态被窗口 B 使用，推荐直接按 `SessionId` 隔离：

- 复用现有 `Dictionary<ImeSessionId, ImeContext>` 保存每个会话的输入状态；
- 同时保存每个会话最近一次 Target 和 Anchor；
- `ProcessKey` 按 SessionId 选择上下文；
- `GetSnapshot`/`GetUiState` 明确请求哪个 SessionId；
- 会话结束、窗口失效或超时后清理。

定位快照应独立于 `ImeContext` 的词典转换逻辑，并关联既有会话；不要用单一全局快照覆盖已实现的多会话基础。

### 8.3 UI 状态生成

`ImeUiState.FromSnapshot` 当前接收 Snapshot 和诊断/降级参数，但不接收锚点。建议扩展既有组装入口或由 Host 专门组装：

- Snapshot 决定 composition、candidate、guideline 和可见性；
- InputContextSnapshot 决定 Target 和 CandidateAnchor；
- 锚点无效时 `CandidateWindowVisible` 是否仍为 true，应与 UI 降级策略一致。

建议候选内容可见但锚点无效时：

- 状态仍标记候选内容存在；
- UI 不在 `(0,0)` 显示；
- UI 保持上一次同 SessionId 的有效位置，或暂时隐藏并等待下一次有效位置。

## 9. Avalonia UI 开发细节

### 9.1 Mapper

更新 `CandidateWindowStateMapper`：

- 映射结构化 Anchor；
- 保留 `IsAnchorValid`；
- 不把无效锚点转换成 `(0,0)`；
- 保留排除矩形和 AnchorKind，供窗口摆放算法使用。

### 9.2 Controller

`CandidateWindowController` 应按 SessionId 维护最近有效锚点，至少满足：

- 同一会话锚点短暂读取失败时可继续使用最近值；
- 会话改变时不得沿用旧窗口位置；
- composition 结束时清除缓存；
- Target 失效时隐藏或等待状态恢复。

### 9.3 窗口定位

在实际 Avalonia Window 接入时实现：

1. 先测量候选窗期望尺寸。
2. 选取锚点所在显示器的工作区。
3. 默认放在锚点或排除矩形下方。
4. 越过底部时翻转到上方。
5. 越过左右边界时仅做必要平移。
6. 保证窗口不激活、不抢占目标应用焦点。
7. Anchor 或候选内容变化时在 UI 线程更新位置。

实际窗口入口已经存在：`CandidateWindow.axaml/.cs` 和 `CandidateWindowPresenter.ApplyState/PositionWindow`。Presenter 使用 `PixelPoint` 消费非零裸锚点；缺失时使用主屏工作区固定点，尚未见锚点有效性、显示器选择、边界翻转和排除矩形逻辑。应在该入口扩展定位，不再寻找或重建缺失的窗口宿主。

## 10. 消息与生命周期

### 10.1 组合生命周期

建议定义：

- 首次进入 `IsComposing=true`：创建或激活 SessionId。
- 组合中：持续更新同一 SessionId 的 Target 和 Anchor。
- `CommitText != null`：提交后结束会话。
- Escape/清空组合：结束会话。
- `ImeSelect(..., select=0)`、目标窗口销毁或 Host 超时：清理会话。

### 10.2 SnapshotChanged

完成锚点传播后，建议同步实现现有 `XiaoXiIme.SnapshotChanged` 通知，否则 UI 只能轮询 `GetUiState`。

通知内容应包含：

- SessionId；
- Snapshot；
- Target；
- CandidateAnchor；
- 单调递增版本号或时间戳。

UI 应丢弃旧版本通知，避免 IPC 时序变化导致候选窗回跳。

如果本次严格限定为两个现状，可把通知实现拆成后续里程碑，但 IPC DTO 应避免阻碍主动推送。

## 11. 错误处理与降级

所有 Win32 读取都必须是“增强信息”，不能阻断按键输入主链路。

### 11.1 读取 HWND 失败

- Target 标记无效；
- 不修改 `TRANSMSG` 布局或为其添加 HWND；
- 继续写回当前 `HIMC`；
- 记录可诊断但限频的日志。

### 11.2 坐标读取失败

- Anchor 标记无效；
- 不发送伪造的 `(0,0)` 有效坐标；
- UI 使用同 SessionId 最近有效坐标或隐藏；
- 下一个按键重新尝试采集。

### 11.3 IPC 失败

现有按会话维护的 fallback `ImeContext` 仍应工作。Target 采集不能影响当前 HIMC 写回和消息构造，也不能为 `TRANSMSG` 增加 HWND；定位快照与 Host 返回的输入语义分离。

### 11.4 窗口在请求期间销毁

- 使用定位快照前可选择再次校验 `IsWindow`，但不能更改原生消息 ABI；
- 即使句柄失效也不得向其他新窗口主动发送消息；
- 当前实现只是填充 Win32 返回缓冲区，不应额外调用 `SendMessage`/`PostMessage`；
- SessionId 与 PID/TID 一起用于降低 HWND 复用导致的状态串用风险。

## 12. 测试计划

### 12.1 ImeInterop 布局测试

新增测试验证：

- `INPUTCONTEXT` 总大小；
- `hWnd`、候选窗数组、组合窗及缓冲字段的关键偏移（当前结构没有 `HWndIme`）；
- `CANDIDATEFORM`、`COMPOSITIONFORM`、`POINT`、`RECT` 大小；
- x64 目标下布局与 Windows SDK 定义一致。

布局数值应来源于对应 SDK/本机原生探针，不应凭手工猜测。

### 12.2 ImeModule 单元测试

使用假的上下文访问器覆盖：

1. 从有效 `INPUTCONTEXT` 读取 Target HWND。
2. HWND 无效时返回无效 Target。
3. PID/TID 正确写入快照。
4. `ImeToAsciiExManaged` 的 Target 采集不改变三字段 `TRANSMSG` ABI，并与当前 HIMC 写回一致。
5. 提交文本、组合中、结束组合三条消息分支。
6. `CFS_CANDIDATEPOS` 转换为屏幕坐标。
7. `CFS_EXCLUDE` 保留矩形并使用左下锚点。
8. CandidateForm 无效时降级到 CompositionForm。
9. Candidate/Composition 均无效时降级到 caret。
10. `ClientToScreen` 失败时 Anchor 无效。
11. 负坐标保持不变。
12. 任一 Win32 API 抛出或失败时，非托管导出不抛异常。

### 12.3 IPC 测试

扩展 `XiaoXiImeIpcTests`：

- 新 DTO 的 AOT JSON 往返；
- `ImeProcessKeyRequest` 包含完整 Context；
- `ImeUiStateResponse` 保留 Anchor 有效性、来源、矩形和负坐标；
- 新旧默认值不会把缺失锚点误判为有效 `(0,0)`；
- 路由名称保持稳定，或为 V2 路由增加稳定性断言。

### 12.4 Host 测试

覆盖：

- ProcessKey 保存最新 Target/Anchor；
- GetUiState 返回同一会话的 Anchor；
- 两个 SessionId 不串用状态；
- 旧版本位置更新不覆盖新版本；
- composition 结束后清理状态；
- 无效 Anchor 不覆盖同会话最近有效 Anchor，若采用该策略。

### 12.5 UI 测试

覆盖：

- 有效锚点映射到 ViewState；
- 无效锚点不会显示在屏幕原点；
- 多显示器负坐标保留；
- 窗口越过底部时翻转；
- 左右边界约束；
- SessionId 改变后不复用旧锚点；
- 候选隐藏后清理位置缓存。

### 12.6 Windows 集成验证

单元测试不能替代真实 IMM 行为。增加手工或自动化集成验证矩阵：

- 记事本/标准 Edit 控件；
- RichEdit；
- WPF TextBox；
- WinForms TextBox；
- Avalonia TextBox；
- 浏览器输入框；
- 多显示器，包含副屏位于主屏左侧的负坐标；
- 100%、125%、150%、200% DPI；
- 窗口移动到不同 DPI 显示器；
- 中英文切换、提交、取消、候选翻页；
- 快速切换两个窗口输入；
- 目标窗口在 IPC 请求期间关闭。

验证时记录 Target HWND、PID/TID、原始 Candidate/CompositionForm、转换后 Anchor 和最终 UI 位置，便于判断偏差发生在哪一层。

## 13. 实施顺序

### 阶段一：原生结构与读取基础

1. 对照 Windows SDK 补全并验证原生结构布局。
2. 增加窗口、坐标和 caret 所需 P/Invoke。
3. 建立可替换的输入上下文读取器。
4. 完成 Target 和 Anchor 的纯读取单元测试。

交付条件：无需 IPC 和 UI，也能从测试输入上下文稳定生成结构化 Target/Anchor。

### 阶段二：目标快照与 HIMC 一致性

1. 在 `ImeToAsciiExManaged` 采集本次 Target，供 IPC 定位/诊断使用。
2. 保留 `ImeTransMsgBuilder` 与三字段 `TRANSMSG` ABI。
3. 验证采集与写回属于同一 HIMC，覆盖消息各分支。
4. 保证读取失败时保持现有输入能力。

交付条件：Target 快照对应当前 HIMC，不改变 Windows 返回消息结构或主动向另一个窗口发送消息。

### 阶段三：IPC 上下文传播

1. 复用 `ImeSessionId`，新增 Target、Anchor DTO。
2. 扩展 `ImeProcessKeyRequest`。
3. 更新 AOT JSON 上下文。
4. 更新 Client、Server、Bridge 和 Host handler。
5. 增加 IPC 往返测试。

交付条件：Host 可获得与本次按键对应的 Target 和屏幕坐标 Anchor。

### 阶段四：Host 会话与 UI 状态

1. 保存每个会话最近的 Target/Anchor。
2. 将 Anchor 合并进 `ImeUiState`。
3. 明确无效位置和最近有效位置策略。
4. 复用已有按 `ImeSessionId` 隔离的 `ImeContext`，核对位置与会话回收语义。

交付条件：`GetUiState` 返回非默认、可验证来源的候选窗锚点。

### 阶段五：Avalonia 候选窗定位

1. 扩展 ViewState 和 Mapper。
2. 在 Controller 中处理会话和最近有效锚点。
3. 接入实际窗口定位入口。
4. 完成 DPI、多显示器和工作区边界处理。

交付条件：候选窗口稳定显示在目标插入点附近，不出现在 `(0,0)`，不抢焦点。

### 阶段六：集成验证与文档

1. 运行 ImeModule、ImeIpc、Host 和 UI 测试。
2. 进行 Windows 应用兼容矩阵验证。
3. 更新 ImeIpc、ImeModule、ImeInterop 和 UI README。
4. 记录坐标单位、来源优先级、降级策略和协议版本。

交付条件：自动测试通过，真实应用验证结果可复现，文档与实现一致。

## 14. 预计涉及的文件

以下为预计范围，实施前应再次确认实际依赖关系。

### XiaoXiIme.ImeInterop

- `Win32Types.cs`：复用已有 Point/Rect/CandidateForm/CompositionForm/InputContext，核对定位字段，不重建已有结构。
- `ImeConstants.cs`：增加 Candidate/Composition Form 样式常量。
- 可新增 Win32 API 文件：窗口验证、线程信息、坐标转换和 caret API。

### XiaoXiIme.Foundation

- 复用 `ImeSessionId`，新增 Target、Anchor、坐标空间和矩形模型。
- `ImeUiState.cs`：接入结构化锚点。

### XiaoXiIme.ImeModule

- `ImeExports.cs`：读取输入目标/定位快照，保持当前 HIMC 写回。
- `ImeTransMsgBuilder.cs`：保留三字段消息 ABI，不增加 HWND 参数。
- `ImmContextAccessor.cs`：按职责扩展或配合新的 reader/accessor。
- 可新增 `ImeInputContextReader.cs`：封装 Target/Anchor 采集。
- `ImeHostBridge.cs`：传递扩展后的 ProcessKey 请求上下文。

### XiaoXiIme.ImeIpc

- `ImeProcessKeyRequest.cs`：增加输入上下文快照。
- `XiaoXiImeIpcClient.cs`、`XiaoXiImeIpcServer.cs`：更新 handler 签名。
- `XiaoXiImeIpcJsonSerializerContext.cs`：注册新增 DTO。
- 如选择版本化协议，更新 `XiaoXiImeIpcRoutes.cs`。

### XiaoXiIme.ImeHost

- `ImeHostService.cs`：在已有 Session 隔离上保存并返回会话 Target/Anchor。

### XiaoXiIme.ImeUi.Avalonia

- `CandidateWindowViewState.cs`：表达锚点有效性和排除矩形。
- `CandidateWindowStateMapper.cs`：映射结构化 Anchor。
- `CandidateWindowController.cs`：管理会话位置缓存。
- `CandidateWindow.axaml/.cs`、`CandidateWindowPresenter.cs`：窗口已存在，在既有定位入口补齐屏幕选择、边界翻转和 DPI 处理。

### Tests

- `XiaoXiIme.ImeModule.Tests/ImeExportsTests.cs`
- 可拆分新的 `ImeInputContextReaderTests.cs`
- `XiaoXiIme.ImeIpc.Tests/XiaoXiImeIpcTests.cs`
- 复用现有 Host/Core 测试及 `XiaoXiIme.IntegrationTests` 的 `CandidateWindowControllerTests`、`CandidateWindowScreenshotTests`，按职责补充定位验收；不假定 UI 测试缺失。

## 15. 风险与待确认事项

1. **原生结构布局复验**：当前已有布局修复与大小/关键偏移测试；定位相关字段、实际 x86/x64 和 Native AOT 运行仍需复验，本次未做 SDK/原生探针对照。
2. **坐标语义风险**：CandidateForm/CompositionForm 不同样式的坐标解释必须以 Windows SDK 和真实应用验证为准。
3. **DPI 风险**：Win32 物理像素与 Avalonia 坐标单位之间可能存在缩放差异，需要最小实验确认。
4. **多会话风险**：Host 已按 `ImeSessionId` 隔离 `ImeContext`，并把 HIMC 映射为会话标识；完成 HWND/Anchor 后仍需确认不会把窗口 A 的位置发给窗口 B。
5. **IPC 同步延迟**：`ImeToAsciiEx` 当前同步等待 IPC；位置数据必须视为本次按键采集快照，不能假设响应返回时窗口仍然有效。
6. **实际 UI 定位不完整**：Window/Presenter 已存在；当前裸坐标/固定点策略不等于 caret 跟随、工作区约束或多 DPI 通过。
7. **兼容协议选择**：需要确认当前 IPC 是否已有外部消费者。如果已有，应使用可选字段或 V2 路由；如果没有，可直接演进现有 DTO。

## 16. 验收标准

### HWND

- 有效 `HIMC` 关联目标窗口时，采集的 Target HWND 对应本次输入上下文；返回 `TRANSMSG` 仍只有 message、wParam、lParam。
- 无效或缺失 HWND 时不抛异常、不破坏当前 HIMC 的 composition/result 写回，也不添加消息 HWND 字段。
- IPC 中可观察到 Target HWND、PID、TID 和有效性。
- 快速切换窗口时不会把窗口 A 的状态或候选位置发送给窗口 B。

### 候选锚点

- 标准 Win32、WPF、WinForms、Avalonia 和浏览器输入框中，UI 能获得非默认且来源明确的屏幕坐标。
- 无有效坐标时不会把候选窗显示在 `(0,0)`。
- 多显示器负坐标、不同 DPI 和窗口跨屏移动时位置正确。
- `CFS_EXCLUDE` 场景下候选窗不覆盖应用指定的排除区域。
- 组合结束或会话切换后不复用错误的旧位置。

### 工程质量

- 新增 DTO 全部通过 AOT JSON 往返测试。
- 原生结构大小和关键字段偏移有自动测试保护。
- ImeModule、ImeIpc、Host 和 UI 相关测试全部通过。
- 非托管导出边界不传播托管异常。
- README 明确记录坐标系、单位、来源优先级、降级策略和协议版本。

## 17. 参考资料

- [Input Context](https://learn.microsoft.com/windows/win32/intl/input-context)
- [ImmGetCandidateWindow](https://learn.microsoft.com/windows/win32/api/imm/nf-imm-immgetcandidatewindow)
- [IME Messages](https://learn.microsoft.com/windows/win32/intl/ime-messages)
- [Status, Composition, and Candidates Windows](https://learn.microsoft.com/windows/win32/intl/status--composition--and-candidates-windows)
