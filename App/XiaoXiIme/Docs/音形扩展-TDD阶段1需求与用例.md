# 音形扩展：TDD 阶段 1 需求与用例设计

状态：阶段4已新增可审查的人工夹具与红测试，实际结果为契约编译红，尚无行为断言执行/通过；没有生产代码或签名桩。阶段4文件、72行逐组映射及明确缺口见第12节。第8–11节保持冻结合同；第5–7节为历史审查记录。阶段3“仅编辑文档”与第11节“下一轮加桩”为历史交接，本阶段按用户要求仅实现测试，不加桩。

## 1. 交付、顺序与依据

唯一需求/用例事实源为本文。主交接入口：`../../Docs/next-session-handoff.md`。主智能体负责监督并安排另一独立子智能体检查。

严格顺序：**思考用例（本阶段）→独立检查→公开 API 设计→实现测试→引擎实现**。检查意见先回写本文，再进入 API 阶段。下文的配置代号、证据记法和事件记法只是需求描述，不预定 C# 类型、方法、枚举或 Host 序列化字段。本阶段没有新增测试、业务代码、编译或测试执行。

已读取的依据：

- `输入方案的思考.md` 全文；本轮用户要求优先于原文的“逐字/词后互斥”及原文未涉及的大小写、模糊规则。
- `src/XiaoXiIme.ImeCore/ImeContext.cs` 的输入入口、xx、候选查询与 shape 分支；核心 README。
- `tests/XiaoXiIme.ImeCore.Tests/ImeContextTests.cs` 的组合、前缀、选择、学习、xx、全拼/小鹤、整串替换用例；测试 README。
- Dictionary 的 README、`PhoneticInputProjection.cs`、现有 Ranking、Contract、InMemory 和 ShapeSourceParser 测试；Dictionary 测试 README。
- `../../Docs/dictionary-spec.md` 的自有词库、内存同步查询、不依赖 RIME 等边界；`../../Docs/next-session-handoff.md` 的现有产品边界。
- 项目 TFM 为 net10.0、Nullable enable；本项目 `Directory.Build.props` 不指定新语言版本。未找到 XiaoXiIme 下独立 AGENTS/instructions 或 global.json；不套用另一仓库 SeWZC_IME 的局部规则。

本轮为已有无 UI 引擎扩展，优先 core/Dictionary 内存单位测试。不得切换活跃 UI 输入方案、改安装/原生 ABI、修改 Host 持久合同、替换正式词库或扩大 UI 功能。原 dictionary-spec 将“完整整句、复杂模糊”排除：本轮只覆盖有限词段路径组合和显式声韵等价规则，不宣称完整整句/纠错系统；该范围增量以本轮授权为依据，不反向扩大旧规范。

## 2. 最终需求边界

### 2.1 兼容与配置生命期

1. 无新配置的现有构造/调用保持 legacy 行为，包括逐键 `xx` 自动提交“小希”，`SetComposition("xx")` 不自动提交，以及已有纯形/符号/查询规则。legacy 是本文兼容标签，不声称仓库已有同名枚举。
2. 新音形能力必须显式构造配置启用；文章倾向的“可选 0/1/2”仅是新模式的建议，不替换 legacy 默认。新模式以构造时显式配置决定是否保留 legacy 的 xx 快捷触发，新模式默认关闭该快捷触发；关闭不禁止词库中 xx 的普通查询/手动选取，开启仅复用 legacy 的逐键 xx 触发，不使批量替换自动提交。无新配置的调用仍保留原行为；此配置属于无 UI core 合同，不要求新增 UI 决定。
3. 对“组合中切换大小写/位置/长度/排序/自动上屏策略”未定义的组合：本批暂不暴露变更，只在构造新上下文时配置；不自创迁移、重解码或自动提交语义。例外是明确要求的运行时模糊规则更新，见 2.6。
4. 配置边界：逐字必带两码仅搭配逐字位置；固定四键自动上屏仅在双键音节投影 + 逐字必带两形码时可用。全拼不可用“固定四键”。不支持的组合不得静默降级，API 阶段确定校验反馈形式。

### 2.2 大小写与独立纯形查询

三策略互斥：

- **U**：仅大写可作形码，小写只作音码。
- **UG**：大写强制形码，小写仍可枚举音码和有证据的形码解释。
- **L**：仅小写可作形码，大写可作音码；大写/小写复杂混排边界的产品语义后置。

组合逐字符保存原始字母和大写标记，不能整串 lower 后靠某个“首个大写位置”推断后续语义。Shift 产生的大写字符有效；处理到字符层后 Shift 已释放不能改变历史标记。插入、删除、局部消费后的标记与字符一一对应。

CapsLock 来源识别、粘贴来源、批量文本 replacement 的新模式大小写推断不在本批；legacy 的 SetComposition 合同照旧。大小写策略不依赖模拟 CapsLock/粘贴事件来验证。

纯形查询是独立操作意图，不把“只有形码而没有合法前置音节”的字串冒充音形证据。独立纯形查询仍可按规范 shape 码匹配，字母查询大小写规范化不等于组合大小写策略；不改变组合、选择、学习或上屏。是否以及怎样公开入口留给公开 API 阶段。混排起始识别、纯形候选与音形候选跨域混排、L 中复杂大写/小写拼接均后置；无明确规范的组合不强行给预期。

### 2.3 补码位置与长度

位置：**C** 仅逐字；**W** 仅词后；**B** 两者均可枚举。B 的含义是同时保留每条合法解释，不强行合并为一个解释。

可选长度集：**O1={0,1}**、**O2={0,2}**、**O12={0,1,2}**。0 表示该补码位置不补；两键模式的一键中间态只能是待完成证据，不能宣称完整补码。**M2** 是逐字必带两码，每个已完成字音节后必须跟合法完整 shape 别名；与 O2 不同。

逐字 shape 约束该字；词后 shape 约束匹配词段，不是整句。单字词取该字两个 shape 键；多字词取首字首键 + 末字首键。一键取首位。多别名时枚举合法取码组合，不把不同别名首尾任意拼成未登记的单字别名。词后补完可继续下一个词段，无须先上屏。M2 的词后/两者组合本批不支持。

### 2.4 路径、候选、消费和学习

- 所有配置允许且有词库证据的音、逐字 shape、词后 shape、前缀路径共存；奇数长度不证明尾字母是 shape。
- shape 是**路径内硬约束**，错误字不得通过同一路径混入；另一个真正合法的纯音解释仍可存在，不能用“全串出现 shape”做全局过滤。
- 候选必须区分整串完整、待完成前缀、可局部提交的已完成词段；记录消费边界、剩余原始输入和路径证据。没有音节证据不能生造 shape 路径。
- 同文本且相同提交语义只显示一次，保留所有合法证据；提交语义包括所消费区间、提交文本、残留及完成性。**同文本但消费边界/残留不同不能合并成无法选择的单项**。禁止仅 Text 去重覆盖新候选语义。
- 局部选择只提交对应完成词段，剩余输入、标记顺序保持，重新查询残留；不能学习未消费尾部。新模式 Q/H 必须在查询/快照结果中可观测其文本、覆盖终点、完成性和证据，但不可选取提交；选择尝试无 Commit、无学习、组合/光标不变。Space 无可提交项时也不提交 Q/H，Unique/Four 不计 Q/H。E/L 为可选取项，同文本不同消费各有可区分的选择身份，不要求本批 UI 展示。legacy 前缀选择保持原合同。
- 查询、排序、编辑、Reset 均不学习。一次实际候选提交只发一次学习，路径/别名/模糊命中数不增加次数；学习用规范词条 reading，不把 shape 或模糊替代串写成新读音。原始已消费输入可作证据但不替换规范 reading。具体复用/扩展学习数据合同留给 API 阶段，不改 Host 持久格式。
- 不在 shape 硬过滤前按纯音频率/top-N 截断；最终去重、约束、排序之后才使用结果上限。可有有证明的内部优化，不能漏掉低频合法 shape 命中。

### 2.5 排序与自动提交（两个独立维度）

三排序：**P** 音码优先、**S** 形码优先、**A** 自动。优先是合法完整解释之间排序，不排除另一类解释。完整/前缀/局部候选不得靠模糊或 shape 数量伪装完成性。legacy 的 user exact/system exact/prefix/fallback 和同层稳定比较规则保留。

新模式自动排序必须可重复、基于固定频率/明确规则，不以候选更少、shape 别名更多、路径更多、模糊变体更多加权。确切跨层/局部/前缀优先级和 A 评分尚待独立检查/API 前决定；矩阵中未定者标为部分序，不用文章的某个排序例子替代通用规则。

三自动上屏：**Off** 等待确认；**Four** 固定四键，限上述配置；**Unique** 当前唯一完整可提交候选自动提交，可在还有更长前缀时提前结束（这是选择该策略的明确取舍）。不能因为只剩前缀或若干路径合并前恰有一个对象就自动提交。

自动提交后可启用 **Eat1**：只吞紧跟的一次确认空格，无 CommitText，无学习；第二次空格正常交还/按既有空闲处理。任何非确认空格操作（字母、编辑、选择移动、取消、Reset 等）解除令牌。令牌不跨上下文、不跨 Reset；手动提交不设自动吞空格令牌。关闭 Eat1 时不吞。空闲空格具体 Handled 以 legacy 合同为基线。

### 2.6 运行时模糊规则

从词条规范 reading 的声母/韵母组件派生输入别名，两种投影共用该层；不在键串上替换字母，不改原词条/shape/package，不增加词频。开关默认关闭。派生别名不必是规范普通话音节：组件可编码且属于明确开启规则的组合就必须接受，不得用规范音节白名单删除 zuang 等输入别名；别名不作为新规范读音写回词库或学习。

本轮明确规则对：zh↔z、sh↔s、ang↔an、eng↔en，双向。同一音节同时启用声韵规则时接受全部组件组合闭包；各词音节独立展开并取笛卡尔积。TS 额外显式启用 uan↔uang。精确/多个模糊路径到同一提交语义合并证据，不能叠加权重。更多规则和连锁冲突不自动推定为产品规则。派生组件无法投影时必须报告明确的不支持限制，不能静默删结果或硬赋标准音节映射。

运行时替换规则后，在下一次查询/快照刷新前使用新规则，当前原始组合、标记、光标不变；不提交、不学习，旧模糊证据清除，候选重新计算。关闭必须立即恢复精确集合；多个上下文各自规则隔离。具体触发/调用入口留给 API 阶段。

## 3. 稳定人工夹具（禁止读取正式词库）

### 3.1 通用原则与事件/预期记法

夹具名 **InventedV1**，内容完全人工指定；汉字及规范音节可以是真实词，但别名、频率、组合条目、音码映射均是测试自定义，不代表实际拆字正确性或真实语料统计。每例新建内存词库/上下文/学习记录，无共享静态状态、无磁盘/网络/环境配置。投影验证组另列，不能混用。

`T(s)` 逐字符输入 s；大写来自 Shift+对应字母，逐键检查而非一次 SetComposition。`Pick(id,b)` 选文本 id 且消费到 b 的候选（不是假定固定列表下标）；`Space`、`Enter`、`Esc`、`Left`、`Backspace`、`Reset`、`Rule(...)` 为操作意图。默认配置 **UG/C/O12/P/Off/Eat1关/模糊关**；所有新模式例显式构造，不继承 Host。例内只加载“词集”列指明的条目，不额外加载默认词库。候选容量默认 100，指定 limit 的例除外。

预期 `文本@b|尾串|类:证据` 表示消费半开区间 [0,b)，提交剩下的确切原始尾串，类为 **E** 全串完整、**L** 完成词段可局部提交、**Q** 当前串是更长编码前缀（不是已完成尾部）、**H** 半个 shape 的待完成前缀。E 默认尾串空；L 不许误标 E。Q/H 的 b 是已输入且可追踪的覆盖终点，**不是授权提交消费边界**；本文不为 Q/H 猜测提交残留。H 包含 M2 已输入完整音节、尚缺零到一键 shape 的待完成状态。Q/H 可见但不可提交；E/L 可选取。

证据：`p` 纯音，`c(i,码)` 第 i 字补码，`w(码)` 词段后补码，`f(输入音节→规范音节)` 模糊，`a(别名)`。证据必须带原始源区间/大小写标记，记法省略区间时按所列分解唯一推出。未提 Q/L 的例默认查询 **完整集合**；输入编辑快照需另检查列出的 Q/L。只断言指定类时，不假装排除了其他类；`exact={...}` 是完整集合精确相等，`order` 是所列同类候选顺序，`≺` 只是部分序。空完整集合仍可有明确列出的 Q/H。

### 3.2 音码映射与字符表

自定义双键投影 **F**：ni→ni，hao→hc，hang→kr，rang→rh，cao→cc，ma→ma，wo→wo。前缀只有这些码的真实前缀，无裸 r 音节。需要 path 共存的 `nirhc` 分解为 `ni|rh|c`，c 是 cc 的前缀。

自定义投影 **X**：与 F 相同但 hao→hk；只用于 `nihkr` 边界例。**hk、kr、rh 等均为 fixture 自定义，不能称为小鹤投影**。F 的 ni/hc 恰与小鹤部分相同不意味着整个映射是小鹤。

| 字 | 规范音节 | shape 别名（有序集合） | 字级频率 |
|---|---|---|---:|
| 你 | ni | rx,re | 40 |
| 泥 | ni | dx | 500 |
| 妮 | ni | nx | 300 |
| 好 | hao | nz,rz | 30 |
| 号 | hao | kz | 400 |
| 夯 | hang | lr | 90 |
| 让 | rang | yr | 50 |
| 草 | cao | cc | 20 |
| 吗 | ma | km | 60 |
| 我 | wo | wt | 70 |

shape 别名只整条合法，如你 rx/re 不允许凭 r+x/e 之外再生 rs。字级频率只在加载单字词时使用，不与词频求和。

### 3.3 规范词条表

| ID/文本 | 规范 reading | 固定词频 | 词后码推导 |
|---|---|---:|---|
| N/你 | ni | 40 | rx,re |
| D/泥 | ni | 500 | dx |
| I/妮 | ni | 300 | nx |
| A/你好 | ni hao | 120 | rn,rr |
| B/泥好 | ni hao | 900 | dn,dr |
| C/你号 | ni hao | 700 | rk |
| V/你好啊 | ni hao a | 1500 | 本组只用于真实全拼前缀，不接入 F/X |
| J/你夯 | ni hang | 300 | rl |
| K/泥夯 | ni hang | 600 | dl |
| R/你让草 | ni rang cao | 250 | rc |
| M/吗 | ma | 60 | km |
| W/我 | wo | 70 | wt |

默认中文拼词不合语义是有意的人工数据。词频直接指定，不从字频生成。只启用 F 支持音节的条目。

隔离词集：**AB={A,B}**、**ABC={A,B,C}**、**N3={N,D,I}**、**PATH={A,R}**、**TAIL={A,M}**、**J2={J,K}**、**XBOUND={A} 使用 X**。下文每例均明确词集。

### 3.4 模糊专用夹具 Z 与 TS

Z 的四条单字词（其他词不加载）：张/zhang/80/zc，脏/zang/70/od，毡/zhan/60/mz，簪/zan/50/vv。输入也依次取 zhang、zang、zhan、zan。规则 ZI=zh↔z、ZF=ang↔an；完整集合按开启组合精确列在 F 组。

TS 保留 **16 条**人工双字词，不依赖真实词库：首字槽位依次为转、钻、庄、纂；人工指定规范 reading 首音节分别 zhuan、zuan、zhuang、zuan（纂仅是人工文本，不宣称其真实读音为 zuan）；末字依次身/shen、森/sen、升/sheng、僧/seng。每个首/末槽位组合各有一个词，固定词频 `200-4*i-j`（i/j 从0起），无 shape 数据；例如转身200、转森199、钻身196、纂僧185。词条身份按文本区分，钻/纂两行共用人工规范首读音不合并词条。输入四元组仍为 zhuan、zuan、zhuang、zuang；其中 zuang 是规则派生输入别名，不是规范 reading。不得替换四元组、删除纂行或把16条缩水；用户“张4/转身4×4”目标不变。

TS 规则集合：zh↔z、uan↔uang、sh↔s、en↔eng。输入 zhuan shen 应命中上述全部16条；另逐一输入四首别名×四末别名的16组，检查双向闭包。频率不随路径数变动。zuang 输入通过 z+uang 的派生证据回到词条各自规范 reading，不为纂生成 zuang 规范读音。

### 3.5 排序受控与边界变体

完整解释冲突专用词集 **CONFLICT**：只加载 A/你好/ni hao/120 和 O/你好哪/ni hao na/600；哪的 shape 为 nk，另只在该隔离夹具给 F 加映射 na→nz。输入 nihcnz 有 A@6,E:c(2,nz) 和 O@6,E:p(ni,hc,nz)，没有其他完整词。这个 nz 音码也是自定义投影，不是小鹤。

自动排序不预设算法。**AUTO-high** 为 CONFLICT 的副本仅把 A 频率设为 9000；**AUTO-low** 把 A 设为 1；用于检查固定数据影响和别名不增权，跨解释排序的确切输出须在检查后补齐再写测试。

低频 shape 例用 N3，纯音 order D,I,N；limit=1 仍须在 `niR` 硬过滤后返回 N。同词多码增权例把 N 的别名从 {rx} 改为 {rx,re}，都只取输入一键 r，候选文本、分数和相对次序不变。

## 4. 用例矩阵（72 个设计行，8 组各 9 行）

下列是测试设计而非已实现测试。参数行要展开成独立/参数化测试；每次参数实例仍需全新夹具。存在待决定项的行不得写成已冻结测试预期。可在后续一行拆成多个单一行为断言。

### A. 文章 nihc 系列与词段位置（9）

| ID | 词集/配置差异 | 输入与精确预期（完整集合） | 操作/排序 |
|---|---|---|---|
| A01 | ABC | T(nihc)：A@4,E:p；B@4,E:p；C@4,E:p | order B,C,A；Off 不提交 |
| A02 | ABC | T(nirhc)：A@5,E:c(1,r)；C@5,E:c(1,r) | order C,A；无 B |
| A03 | ABC | T(nirhcn)：仅 A@6,E:c(1,r)+c(2,n) | Pick A,6 提交你好，残留空 |
| A04 | ABC | T(nirxhc)：A@6,E:c(1,rx)；C@6,E:c(1,rx) | order C,A；无 B |
| A05 | ABC | T(nirxhcnz)：仅 A@8,E:c(1,rx)+c(2,nz) | 不得等价为固定四键配置 |
| A06 | ABC / W | T(nihcr)：A@5,E:w(r)；C@5,E:w(r) | order C,A；无 B |
| A07 | ABC / W | T(nihcrn)：仅 A@6,E:w(rn) | 约束首末字，不约束整句 |
| A08 | AB / B | 分别 T(nirhc)、T(nihcr)：各仅 A@5,E，第一例c(1,r)，第二例同时w(r)与c(2,r)（好rz） | 两个参数，第二例合并相同提交语义证据，不能以另一种优先排除 |
| A09 | TAIL / W | T(nihcrnma)：完整词段序列 你好+吗@8,E:w(rn)+p；局部 A@6,L 尾ma | Pick A,6：Commit你好，残留ma，候选 M@2,E:p；词段序列的排序暂不冻结 |

A09 要求保留词段组合证据，不承诺任意整句语言模型。完整序列与局部候选不可仅按“你好”文本混淆。

### B. 大小写附着与独立 shape（9）

| ID | 词集/配置 | 输入/查询 | 精确预期与操作 |
|---|---|---|---|
| B01 | ABC/U/C | T(niRhcn) | exact=∅：n 只能音，不能完整消费为好首shape；不得把小写 n 附到好 |
| B02 | ABC/U/C | T(niRhcN) | exact={A@6:c(1,R)+c(2,N)}；大小写标记 [0,0,1,0,0,1] |
| B03 | ABC/U/C | T(niRXhcNZ) | exact={A@8:c(1,RX)+c(2,NZ)}；标记 [0,0,1,1,0,0,1,1] |
| B04 | ABC/U/W | T(nihcRN) | exact={A@6:w(RN)}；R/N 附词段，不按首个大写截掉后续音节 |
| B05 | ABC/UG/C | T(niRhcN)，另 T(nirhcn) | 两参数均 exact={A@6}；前者强制 shape，后者合法猜形，标记不同 |
| B06 | AB/L/C | T(NIrHC) | exact={A@5:c(1,r)}；只验证明确定义的音音形音音，复杂任意混排不扩展 |
| B07 | N3/U/C | T(niR)，Shift释放，Left，Backspace | 删除 i 后原串 nR、标记 [0,1]，无完整 N；不得把 R 小写化；无学习 |
| B08 | N3/U/C | T(niR)，独立纯形查询 rx / RX | 两查询 exact shape={你}，规范别名 rx；前后组合 niR、光标3、选择不变，无提交学习 |
| B09 | N3/U/C | T(RX) | 音形 exact=∅，没有前置音节；独立 pure-shape(rx)仍={你}；不冻结混排/纯形自动入口 |

B01 仍可有已完成词段的局部候选，表中空集合仅指全串完整候选。

### C. 长度、必带、合法配置（9）

| ID | 词集/配置 | 输入 | 精确预期/边界 |
|---|---|---|---|
| C01 | N3/O1 | ni、nir、nirx | 三参数：exact order D,I,N；exact={N@3:c(1,r)}；exact=∅（不得完整消费两形） |
| C02 | N3/O2 | ni、nir、nirx | 三参数：D,I,N；exact=∅且 N@3,H:c(1,r待x/e)；exact={N@4:c(1,rx)} |
| C03 | N3/O12 | ni、nir、nire | 三参数：D,I,N；N@3:c(1,r)，合并 rx/re 首码证据；N@4:c(1,re) |
| C04 | AB/C/M2 | nihc | exact=∅；未补首字两形，不能完整词匹配 |
| C05 | AB/C/M2 | nirxhc | exact=∅；你好@6,H，第二字缺两形；不得局部自动提交成你好 |
| C06 | AB/C/M2 | nirxhcnz | exact={A@8:c(1,rx)+c(2,nz)}；Off 无提交 |
| C07 | N3/C/M2 | nir、nirs | exact=∅；前者 H 待完成，后者无合法 shape；不把 rs 当你别名 |
| C08 | AB/W/O2 | nihcr、nihcrn | 前者 exact=∅且仅A@5,H:w(r待n/r)；后者 exact={A@6:w(rn)} |
| C09 | 构造边界 | W+M2、B+M2、O12+Four、全拼+M2+Four | 四参数均不允许构造有效此组合；不自动改为 C/M2/Off，反馈形式待 API |

### D. 路径共存、硬约束与消费（9）

| ID | 词集/配置 | 输入 | 预期 |
|---|---|---|---|
| D01 | PATH | nirhc | exact={A@5:c(1,r)}，同时 R@5,Q:p(ni,rh,c前缀cc)；不是奇数尾 c shape |
| D02 | PATH | nirhcc | exact={R@6:p}；A@5,L 尾c:c(1,r)；纯音路径不能被旧 shape 路径排除 |
| D03 | ABC | nidhc | exact={B@5:c(1,d)}；无 A/C 的 c(1,d)证据 |
| D04 | ABC/W | nihcd | exact={B@5:w(d)}；不以词频让 A/C 绕过显式 shape |
| D05 | AB/B | nihcr | exact={A@5}，证据同时有 c(2,r)（好 rz）和 w(r)；显示一次，保留两种附着 |
| D06 | XBOUND/X/C | nihkr | exact={A@5:c(2,r)}；另 A@4,L 尾r:p；必须两个提交语义，不按Text合成一个 |
| D07 | XBOUND/X/C | nihkr，Pick A,4 | 提交你好；残留r（小写标记0）；下一查询 E/L/Q/H 均为空（XBOUND仅A，r不是ni的前缀）；保留原始r；学习仅A一次，不含r |
| D08 | XBOUND/X/C | nihkr，Pick A,5 | 提交你好；残留空；学习A一次，证据含好首r；与 D07 分开上下文 |
| D09 | PATH | nirhc，继续 c | 第一次保留 D01 证据；第二次完整 R 及局部 A，同 D02；不得缓存第一次猜测挡住新的完整解释 |

D06 的 ni/hk/r 全为 X 自定义，**不得在真实小鹤测试中照抄 nihkr**。L 候选 b=4 的尾 r 是下一词段的尚未完成输入，不是假定它必须与上段的 shape 合并。

### E. 排序、去重与不截断（9）

| ID | 词集/配置 | 输入/变体 | 断言 |
|---|---|---|---|
| E01 | N3/U，limit=1 | niR | exact={N@3:c(1,R)}；低频40必须穿过纯音500/300后过滤，不能先截断 |
| E02 | N3/UG | nir，N别名 {rx}→{rx,re} | 两个新上下文输出同一 N；分数40不变，证据可增；不学习 |
| E03 | AB/B | nihcr，重复登记好 rz | A 同提交语义只一次，频率120；重复别名不得增权 |
| E04 | CONFLICT/P | nihcnz | exact={O@6:p,A@6:c(2,nz)}；order O,A；两类保留 |
| E05 | CONFLICT/S | nihcnz | 同 E04 完整全集；order A,O；不得删除纯音O |
| E06 | AUTO-high / A | 对 E04 冲突输入迁入高频变体 | exact={O@6:p,A@6:c(2,nz)}；固定输入重复查询输出稳定；确切总序/评分未决定，不预设“高频必胜全部规则” |
| E07 | AUTO-low / A | 与 E06 仅固定频率不同 | exact={O@6:p,A@6:c(2,nz)}；检查规则对统计的响应，不以候选更少认定意图；确切对比待决定 |
| E08 | AB/P/S/A | nihc，三配置分别新上下文 | 同类纯音 exact order B,A；排序策略不改变全集，不以 A别名多提升频率 |
| E09 | legacy现有 Ranking | 复用现有用户精确/系统精确/前缀/回退夹具 | 原排序与 Text 跨来源去重基线不变；新语义去重只应用新匹配结果，不擅改旧合同 |

E06–E07 的自动排序总序是明确的检查阻塞项，不伪称完整冻结。CONFLICT 只是受控输入实例，不规定产品音节投影；自动排序在下一阶段必须补成精确、可执行的期望后才写测试。

### F. 模糊声韵组合与更新（9）

此组默认全拼声韵别名查询意图，Off；Z 为规范单字夹具，TS 为人工词条与派生输入别名夹具。只加载系统词，无学习权重；F01–F06 的 order 仅冻结该组同类完整系统候选按固定词频降序，精确/f证据不另增权，不推出跨来源、完成性或自动策略的普适顺序。

| ID | 词集/规则 | 输入 | 精确完整集合与操作 |
|---|---|---|---|
| F01 | Z/全关 | zhang,zang,zhan,zan | 四参数分别仅 张、脏、毡、簪；b 为各输入长度，证据p |
| F02 | Z/仅ZI | 同四输入 | 分别 {张,脏}、{张,脏}、{毡,簪}、{毡,簪}；按固定频率降序 |
| F03 | Z/仅ZF | 同四输入 | 分别 {张,毡}、{脏,簪}、{张,毡}、{脏,簪}；order依次张,毡 / 脏,簪 / 张,毡 / 脏,簪；各替代音节记录f |
| F04 | Z/ZI+ZF | 同四输入 | 每个 exact={张,脏,毡,簪}，order 张,脏,毡,簪；张↔簪证明同音节声韵同时变化 |
| F05 | TS/四规则开 | zhuanshen | exact 为 3.4 全部16词，b=9，残留空；order 200到185；转身精确，其余按词条规范 reading 记录变化组件证据；纂行规范首音节为zuan |
| F06 | TS/四规则开 | 四首输入别名×四末输入别名的16组逐一连接（包含zuang） | 16参数，每个 exact 都为同16词；逐音节闭包相乘，不只改第一词/一个位置 |
| F07 | Z/动态 | T(zhang)，Rule(ZI+ZF)，Rule(全关) | 快照完整集合 {张}→四词→{张}，reading始终zhang、光标5；无提交学习，词条/频率快照不变 |
| F08 | Z/ZI+ZF，再别名或重复规则去重 | zhang | 分数80/70/60/50，不按f路径数叠加；shape zc 不变，独立查询 zg不因ang规则命中张 |
| F09 | 两上下文各Z | 上下文1开ZI+ZF，2全关，各zhang | 1为四词，2仅张；Reset1 不影响2/词库；动态更新不改 package 或规范reading |

F05 的输入长度是 zhuan(5)+shen(4)=9；F06 每参数按实际长度消费，不统一写9。

### G. 局部编辑、学习与 Reset（9）

| ID | 词集/配置 | 事件 | 精确预期 |
|---|---|---|---|
| G01 | TAIL/C/U | T(niRXhcNZma)，Pick A,8 | Commit你好一次，残留ma/标记[0,0]/光标2；exact={M@2:p} |
| G02 | 同G01 | 再Space | Commit吗一次，残留空；累计学习A一次、M一次，不学习拼成的“你好吗” |
| G03 | N3/U | T(niRX)，Left，Backspace | 删除R而非X，reading niX，标记[0,0,1]，光标2；exact=∅（X非首shape），N无错误证据 |
| G04 | AB/U | T(niRhcN)，Backspace | reading niRhc，exact={A@5:c(1,R)}；末N证据删除；选择合法，无学习 |
| G05 | AB/B | T(nihcr)，查询/排序/移动选择，Pick A,5 | 只在Pick发一次学习A；规范 ni hao，消费串nihcr；两证据不发两次 |
| G06 | Z/ZI+ZF | T(zan)，Pick 张,3 | Commit张，学习规范zhang一次、原始输入zan；不新增zan读音条目 |
| G07 | N3/U | T(niR)，Reset | reading空、候选空、标记空、光标0、选择初态、待消费路径清空、学习0 |
| G08 | N3/U，两上下文 | 1 T(niR) 后Reset再T(ni)；2 T(niR) | 1完整 D,I,N；2仅N；旧shape/学习/吞空格令牌不跨上下文 |
| G09 | N3/U | T(niR)，Enter；另新上下文T(niR)，Esc | Enter提交原始niR且学习0；Esc无Commit、组合清空、学习0；新模式Enter明确提交当前原始串（含大小写），不选择候选、不学习；Esc清空输入，不重置构造配置/模糊规则，不暗改legacy |

G03/G04 保留编辑中非法/未完成原串，不能为了恢复候选偷偷删除字符或转小写。

### H. 上屏、空格与真实投影兼容（9）

| ID | 词集/配置 | 事件 | 精确预期 |
|---|---|---|---|
| H01 | AB/Off | T(niRhcN) | 唯一完整A仍不提交；Space提交你好；手动提交不设Eat1 |
| H02 | N单字/C/M2/Four/Eat1 | 逐键 n,i,R,X | 前3键无Commit；第4键Commit你，reading空，学习一次；固定4边界 |
| H03 | N单字/C/M2/Unique | n,i,R | ni也不是完整候选，R后仅半码H，不自动提交；再X完整唯一才Commit你；不能用O2让ni提前Unique提交 |
| H04 | 真实全拼 {A,V}/U/Unique | T(nihao) | 当前完整A唯一，即使V为前缀仍自动Commit你好；Off变体同输入不提交 |
| H05 | 同H02 | 自动提交后Space，Space | 第一次Handled=true、Commit=null、学习无新增；第二次空闲Space按legacy交还，不连续吞 |
| H06 | 同H02 | 自动提交后分别字母m/Left/Esc/Reset，再Space | 四参数，非空格均解除令牌；Space不得因上一自动提交被吞；字母m可开始新组合，不要求Space一定交还 |
| H07 | 同H02但Eat1关；另手动提交Eat1开 | 提交后Space | 两参数均不设置自动吞空格行为；空闲Space基线，不多学习 |
| H08 | 真实全拼/真实小鹤独立规范源 {A,B,C} | nihao / nihc；另形码 niRhaoN / niRhcN | 两投影完整全集分别 B,C,A；带形参数仅A，消费7/6；规范reading均ni hao，shape不被投影 |
| H09 | legacy默认内存及已编译缩写包；另新模式显式xx开/关 | legacy T(xx)、SetComposition(xx)；新模式xx开/关各T(xx)，Off；项目术语全拼/小鹤 | legacy逐键xx保持Commit小希、替换xx不自动提交；新模式开启复用触发，关闭无自动Commit且原串xx保留（夹具只载小希/xx缩写条目，关闭仍可手动选取）；规范xiao xi ai mu yi的真实键xiaoxiaimuyi/xnxiaimuyi不变；本阶段不执行测试 |

H01 的输入为六字母 niRhcN，无分隔键。H08 完整音形全拼 niRhaoN 长度7，小鹤 niRhcN 长度6。H08 的声韵模糊共享投影还需补充真实合法音节样本：zhang/vh、zang/zh、zhan/vj、zan/zj，ZI+ZF 两规则组合在 Z 上各输出四词（uan↔uang、sh↔s 不影响本组）；以及 zhuan/vr、zuan/zr、zhuang/vl 与 shen/uf、sen/sf、sheng/ug、seng/sg 的规范组件样本，连同下述zuang派生别名完整覆盖TS输入，不截成12条受支持子集。不得把zuang硬称标准读音。这些均为 H08 独立参数，不继承 F/X。小鹤新模式派生别名按现有组件键表编码：z→z、uang→l，故 zuang 别名键为 zl；这是派生组件编码，不声称 zuang 是标准小鹤普通话读音。TS 开四规则时，输入 vr/zr/vl/zl 与 uf/sf/ug/sg 的16种连接均须返回TS全16条、消费4、残留空，规范reading保持3.4定义；全拼16组按实际长度消费。现有组件表有 uang/iang→l 等碰撞：必须反查保留所有有词库及规则证据的组件路径，再按提交语义合并，不能把一个键强映唯一读音。TS隔离夹具不加载碰撞外词，不据此宣称全局无碰撞；现有编码器未校验标准音节合法性不能充当标准读音认证。

## 5. 独立检查清单与待决定项（进入 API 前的门禁）

1. **复核 E04/E05 的 CONFLICT 完整音/形冲突夹具**：ni|hc|nz→你好哪与ni|hc+nz→你好必须并存，均消费6。不得拿 D01 的“完整对前缀”冒充该测试；不得从实现反推预期。
2. **A 自动排序**的确定评分、与来源层级/完成性/局部候选的交互未定义；E06/E07 暂不硬定总序。不能把“文章高频例子”或“少候选更可信”当通用排序要求。冻结后补精确期望。
3. **TS 4×4 已冻结**：保留四首输入别名含zuang及16条人工文本；规范reading与派生别名分离，见3.4和H08。不得再以普通话合法性删组合/替换目标。真实组件键支持、碰撞和不可投影限制必须在公开合同中可表达。
4. **Q/H 可见不可提交已冻结**：查询/快照可观测，选择尝试不提交、不学习、不改组合；E/L可独立选取。公开API如何承载状态/身份留设计阶段，不增加仅供测试的入口。
5. **同文本不同提交语义展示/选择**应在 core 合同可区分；不要求本批 UI展示。路径证据可怎样通过公共 API 可测、复用现有候选模型的方式留待下一阶段。
6. **xx 兼容已冻结**：legacy不变，新模式构造配置显式开/关、默认关闭快捷触发；H09用Off隔离该触发。xx开启与Four/Unique同时生效时不重复Commit/学习；不要求UI选项。
7. **复杂混排/纯形入口/组合时改配置**后置，按构造配置隔离；只有明确的动态模糊规则例外。B06 仅断言已定义的简单形附着，不自创大小写词段切换。
8. **Four 与多个完整候选/更长词**的提交选择及四键单字是否提前于已存在词段前缀待补规范；H02 仅唯一单字fixture，不覆盖未定义交互。不得用 H02 的一个例子推出任意四键强制第一候选。
9. **Enter/Eat1/Reset 已冻结**：Enter提交原始大小写且不学习；Eat1在非确认空格操作到达上下文时即解除，不以Handled为条件；Reset清空组合、候选、路径、选择和令牌，保留该上下文构造配置与当前模糊规则，不重置共享词库/他人配置。
10. **学习合同**：保持Host持久格式，候选规范reading与原始已消费输入分离；如何复用现有 ImeDictionaryLearning 留 API 阶段，不能将每条证据当独立学习事件。
11. **有限词段组合与资源预算**：不加整句LM；不能因候选上限提前丢低频shape。枚举预算/安全上限需有完备性边界，再设计单独规模测试，不用正式词库或随机频率解释排序。

独立检查需要逐行检查消费字符数、残留、大小写标记、完整/前缀类别和允许规则；特别核对 fixture别名好 rz 导致的新合法路径，不能漏枚举。发现其他完整路径应修正预期全集，而不是修改引擎禁止合法解释。

## 6. 主监督交接

- 交付路径：`XiaoXiIme/Docs/音形扩展-TDD阶段1需求与用例.md`。
- 数量：**72个矩阵设计行**，A–H共8组，每组9行；参数展开数量未冻结（例如F06单行16输入，每输入完整候选16条；F04四输入各4候选）。未把参数行、断言数、候选数混称测试数量。
- 夹具：InventedV1 F/X人工投影；Z四单字；TS16双字；真实全拼/小鹤投影另列H08/H09，禁止nihkr冒充小鹤。
- 完成：只写需求/用例文档与主交接引用。**未设计公开API、未新增测试/引擎代码、未执行构建/测试**。
- 下一步：阶段2检查已回写，先处理第7节仍阻断项；已冻结部分可作为公开API合同输入，不能把未冻结排序/自动提交写成通过的测试预期。

## 7. 阶段2独立审查记录

### 7.1 依据与结论

完整重读本文全部72设计行及用户文章全文；只读核对现有 ImeContext 的逐键输入、局部编辑、全串提交、Enter、xx、首大写shape分支，PhoneticInputProjection 全文、DictionaryCandidateRanking 正向排序、ImeDictionaryLearning 及 core/Dictionary README。第1节列出的测试文件阅读属于阶段1依据，不冒称本轮已逐个复验测试实现。

现有引擎首大写截断、Text去重及全串Clear不能直接表达新路径/局部消费合同；当前shape前先截断也不能作为E01预期依据。现有Enter保留原串、不学习，空闲Space交还；xx仅逐键自动触发。现有小鹤编码器组合声韵键而非验证普通话合法音节，因此可依据组件表定义zl派生别名，但不能宣称标准读音合法。以上仅是只读发现，不在本轮改代码。

结论：**独立审查完成，整体尚阻断**。词库/shape/长度/残留及已列同类排序预期可据本文设计真实公开合同；不应立即声称72行均已具备完整自动排序测试预期。未编写API、测试或实现，未执行构建/测试。

### 7.2 逐行核校与修订ID

共核校72行：A01–A09、B01–B09、C01–C09、D01–D09、E01–E09、F01–F09、G01–G09、H01–H09；仍保持8组各9设计行，不将参数数混称测试数。

- A01–A08：完整集合、字符消费数及好rz路径正确；A08第二参数须合并c/w证据。A09消费8、局部消费6尾ma正确；完整序列输出文本为“你好吗”，局部为“你好”，不定义合成词频或二者总序。
- B01–B09：U/UG/L已限定参数、标记及删除位置正确；B01只断言E空，不声称无L；B08独立查询无状态变更；B09不要求组合自动纯形识别。未引入CapsLock/任意混排边界。
- C01–C09：长度集合、M2及非法构造正确；C02/C05/C07/C08的H必须可见不可选，缺零键shape的M2状态也属H；C07的nirs无H，完整空。H/Q存在不授权局部提交整个预测词。
- D01–D09：路径、完整/前缀/局部分离正确；**修订D07**，残留r在仅A的词库没有任何候选，不可含糊承诺前缀。D06保持A@5/E和A@4/L两个独立选择身份；D07/D08分别只学习一次，残留不学习。nihkr只属于X，不能放入标准小鹤。
- E01–E05/E08/E09：约束后limit、证据去重、CONFLICT同为消费6及legacy稳定顺序正确；E06/E07集合冻结为E04同一全集，但A总序/评分仍阻断。分数40/120指固定词频权重，不强制新增公开“分数”字段。E03重复登记属于输入数据去重，不预定parser必须接受重复行。
- F01–F09：Z开关全集正确，**修订F03**补齐各参数同类顺序；**修订F05/F06**分离TS规范reading与别名。F05消费9；F06按实际连接长度消费，所有16输入仍命中16文本。F07/F09保留原组合、规则隔离，Reset不改规则；F08声韵不作用shape。
- G01–G08：消费8尾ma、编辑删除位置及一次学习正确；**修订G09**冻结Enter原始大小写与Esc合同。序列提交学习事件数仍为一次，但跨词段学习载荷见剩余事项。
- H01–H07：六/四键长度、唯一完整与前缀取舍、Eat1一次及解除合同正确；H02只证明唯一单字四键，不推导多候选提交。**修订H08**增加真实声韵组件编码和碰撞合同参数，**修订H09**增加新模式xx显式配置与legacy保留参数。

直接修订矩阵 **9行ID：D07、E06、E07、F03、F05、F06、G09、H08、H09**（E06/E07只补冻结完整全集，不编造排序）。同时修订第2/3/5/6节规范：影响A09、C02/C05/C07/C08、D01/D06/D09、E06/E07、F07/F09、H03/H04/H06等行的解释，但不改其输入/既定目标。F05/F06的16文本和4×4输入数量不变；不是减少覆盖后通过。

### 7.3 冻结的排序与可观测性语义

- 所有未声明跨类总序的矩阵只比较E完整子集；E的order不要求E位于L/Q/H之前，也不要求整份快照只有E。Q/H必须可观测，但非选取对象；E/L选择身份不依赖固定列表下标。
- 系统词、同完成类/同解释优先类别的本夹具以给定固定词频降序。P/S只冻结E04/E05等合法完整冲突的p/shape相对顺序，不删除另一解释；别名、模糊及路径数量不增权。已列order含不等词频，无需凭空补通用平局规则。
- F组仅同类系统完整结果按词频比较，不规定“精确总在模糊之前”；F03/F04的精确词可能位于高频模糊词之后。学习及其他来源层级不由F组推导。
- AUTO-high/low的完整集合相同；稳定性、无别名增权可测试，但未定义A评分前不得断言跨解释高频必胜。A09完整序列与局部、D01完整与前缀、D06同文不同消费不冻结相互顺序；消费身份及可选取性必须独立验证。

### 7.4 剩余事项（不以范围外问题代替门禁）

1. **阻断E06/E07及A策略全面排序验收**：尚缺自动评分/规则偏好、来源层级和完成性之间的比较合同。需明确受控输入的相对顺序与稳定平局规则后再写排序测试；不能从现有排序器猜新策略，不能编造普适硬顺序。
2. **阻断Four一般化自动提交验收**：四键出现多个完整候选、同消费不同词和更长词段前缀时的提交对象/是否提交未定义；H02唯一单字预期有效，不能据此自动选择第一项。
3. **有限词段序列公共提交/学习合同需补齐**：A09完整文本与消费已冻结，Pick局部学习一次已冻结；选择完整序列时一次事件如何承载多个规范词段reading及其边界未定，不写成现有单词条学习载荷能自然支持。不得学成未登记新整句。
4. **资源预算需显式完备性边界**：候选limit不限制中间硬约束路径；未来若设枚举安全上限需表达拒绝/限制，不能静默漏掉TS16或低频shape。规模参数及限制反馈尚未冻结，不用正式词库填补。

公开API设计可整理已冻结合同的承载方式，但以上阻断未解决前不能宣告完整阶段门禁通过。CapsLock、复杂混排、自动纯形起始识别、UI决策及Host持久格式均不拉回本批。

## 8. 阶段3：阶段2剩余合同的最终裁定

本节是算法设计，不是从当前实现反推测试。优先最小、确定、可解释版本；无需新增独立引擎或接口。第7.4节四项设计门禁在此关闭，不代表代码或测试验收通过。

### 8.1 合法图、完成性与资源边界

原始ASCII字母序列的UTF-16半开区间为输入坐标；每个字符保留原字形及大写布尔值。沿输入位置构建有向无环图，边消费至少一个字符：规范词条音节投影、开启规则派生的音节投影、合法逐字shape、合法词后shape。状态包含词条/音节位置、附着方式、尚缺shape键数、已确认词段边界；不得仅按输入位置合并不同约束状态。每条完整词段边拥有词条身份、规范reading和原始区间。终点覆盖全部输入产生E，已完成词段但后有残留产生L，合法词编码未完产生Q，尚缺必需shape产生H；H优先于同一路径的Q分类，其他独立路径仍保留。

L枚举每个非空已完成词段序列前缀，消费从0开始；不得凭一个字已补完就在未完成多字词内部制造单字提交。没有登记单字词就没有该单字提交边。E序列也只能由实际登记词条连接，不能制造新词。Q/H只有覆盖范围，不授予消费范围。

完全搜索全部合法状态/边后按提交语义合并、排序，最后分页/topK。不使用beam、词库Query的maxCount、编译prefix缓存上限作为完整搜索边界。安全边界仅为新模式原始输入最多64个字母，有限加载词库、每次路径至少消费1键，词段数至多64；65键的插入明确拒绝且状态不变。不设隐藏候选/路径数量预算；规模优化必须与穷举oracle等价。字典新能力直接访问完整规范词条，而非旧截断prefix索引。旧包仍可由完整candidate shard构建完整索引；缺规范reading/投影定义时明确NotSupportedException，不把旧缩写当规范音。新包须保存自定义投影定义，不能fixture专用旁路。64键以上的只读QueryInterpretations抛ArgumentOutOfRangeException；查询含非ASCII字母则ArgumentException；空串返回TotalCount=0。逐键非法输入则交还Handled=false，不修改状态。

查询返回准确TotalCount和HasMore；offset>=TotalCount返回空页。offset>=0、limit=1..100，否则ArgumentOutOfRangeException；默认limit100。快照展示页大小9，跨页可达全部结果，旧MaxCandidateCount=100仅约束legacy及单次新查询页，不截断新模式总集合。Unique和Four在分页前全体可提交语义上判断。64以内不存在静默截断状态；如果内存不足不能返回假装完整的部分结果。

### 8.2 确定排序（P/S/A）

使用字典序比较而非难解释的加权总分。所有策略首先E、L、Q、H；L内部覆盖终点降序，其余同类覆盖终点降序；随后来源层级、策略特征、固定频率、稳定平局键。完成性优先避免高频预测压过实际完整转换。来源层级为User、System、Fallback；序列取最弱来源（最大序号）。User词段用UserFrequency，其余用BaseFrequency；序列频率取各词段频率的最小值，不求和、不造合成词频。最小值保守且不奖励拆分段数；单词条分数即原固定频率。对同提交语义所有解释取最佳排序键，不累加。

P策略特征为有纯音合法解释者优先；S为有至少一个shape约束合法解释者优先。A策略不猜输入意图，以固定频率降序为唯一统计特征（策略优先值恒0）；来源/完成性仍在频率之前。shape已经是路径内硬约束，再额外奖励shape会把同类频率及F组语义扭曲；所以最小版本不另给shape、精确/fuzzy、别名或路径数量分。此即确定可解释的A，不是机器学习或未来自适应意图识别。

平局依次：词段数升序、规范reading去空格长度升序、文本UTF-16长度升序、Text ordinal、规范reading序列ordinal、稳定词段身份ordinal；不同完成性/消费语义早已比较。词库加载次序、hash枚举次序不参与。频率非负int，比较不得用减法溢出。纯音/形双解释按策略分别取可用解释，不删除另一证据。

精确裁定：E06(A9000/O600)为A,O，频率9000/600；E07(A1/O600)为O,A，频率600/1。A09为你好吗@8/E在你好@6/L之前，序列频率min(120,60)=60；D01的A/E在R/Q之前；D02、D09的R/E在A/L之前；D06的A@5/E在A@4/L之前。Q在H之前，同类再按上述键排序。E02的N频率40、E03的A频率120、F08四频率80/70/60/50不变。legacy仍调用原排序合同，包括原Text去重，不套用新序列比较器。

### 8.3 Four/Unique与更长词

Four只接受双键投影+C+M2。触发检查仅在逐键字符插入后，光标位于串尾、从当前组合起点每字恰为2音键+2完整shape键且长度为4的正整数倍；混合不同token宽度不得冒充触发。只在全串E、且该解释满足上述完整逐字结构时选取。四键音节/shape完整但只有一个更长词的H/Q时绝不提交半词。多候选：在触发资格子集中按第8.2节排序选第一项（明确为该自动策略的确定默认选择，而非任意半路径commit）；同提交语义多路径只算一项。不能因为当前总排序首项有非Four解释就提交它。

若同时有登记单字E及更长词Q/H，在第4键提交最高排序单字，牺牲更长词；若没有登记单字，仅登记两字词，则第4键不提交，第8键完整时提交两字词；三字词同理第12键。选择Four就是接受逐字边界的提前截断；欲输入被短词截断的长词需构造Off上下文，不暗设等待时限或半词合成。Unique只统计全串E语义数量，不统计L/Q/H；有一个E就提交，即使更长Q/H存在。每次键处理至多一个Commit/学习事件。显式xx触发优先，其后不再执行Four/Unique；Eat1统一在实际自动提交后设置。编辑、动态规则刷新、查询、SetComposition不触发自动提交。

### 8.4 学习：一次事件、完整词段、不污染reading

复用Action<ImeDictionaryLearning>，一次实际候选提交调用一次。扩展学习对象Segments，列出所选解释的已确认完整登记词条，每项含Text、CanonicalReading、InputRange、OutputRange；外层OriginalInput仅为被消费的原始前缀。局部残留不在载荷内；完整序列选择不学习一个未登记整句。相同提交语义有多个规范分段/读音时，按第8.2节最佳解释及稳定平局键选择一个明确解释作为Segments，所有证据仍可观测，不能向多个读音都学习。

UserDictionary.Learn新载荷对每个被确认词段增频一次（同词多次出现按出现次数），一次调用可更新多个既有词条键，返回最后一段更新后频率；先校验整份载荷再原子更新。reading键使用CanonicalReading经既有lookup规范化，而非OriginalInput。Candidate仍是提交文本与所选规范reading序列，仅作兼容摘要。空Segments保留旧Learn行为，保障Host旧调用及注册词不变；新引擎候选提交必须非空Segments。持久JSON仍为既有词条键/频率格式，不新增混输键、shape键或派生读音字段。

A09完整选择：一次事件，OriginalInput=nihcrnma；Segments为你好/ni hao/[0,6)/输出[0,2)，吗/ma/[6,8)/输出[2,3)。局部选择仅第一项，残留ma再Space独立第二次事件。G06为张/zhang/[0,3)，OriginalInput=zan。D07为你好/ni hao/[0,4)，OriginalInput=nihk，Remaining=r；D08区间[0,5)、OriginalInput=nihkr。查询/更新/Enter/取消无学习。

## 9. 冻结公开API（设计签名，尚未编写代码）

以下所有签名是本批红测试目标；表中public为唯一新增公开表面，其余图、解码、排序比较器及索引帮助器private/internal。不新增任何接口，不wrap IImeDictionary、不另造引擎。集合构造时防御复制为只读快照；record不能借外部可变List改变会话。公共方法实现时补XML说明，用户错误文字走已有资源。

### 9.1 现有兼容入口

保留ImeContext现有 `public ImeContext(IImeDictionary dictionary, Action<ImeDictionaryLearning>? learn = null)`、Snapshot、ProcessKey、SetComposition、QueryConversionList、QueryReverseConversionList、RegisterWord、UnregisterWord、EnumerateRegisterWords全部签名和legacy行为。现有ImeCandidate(Text,Reading,Score)、ImeSessionSnapshot和ImeProcessResult位置构造签名不改，不把新对象送入Host/IPC序列化合同。新模式下旧候选列表只是新详情候选的一一对应摘要（不可仅Text去重），页索引选择Q/H必须拒绝提交。详细消费/身份由下列core入口提供。

### 9.2 ImeCore命名空间：输入配置与运行规则

新增public枚举：

| 类型 | 成员（显式依次从0开始） |
|---|---|
| ImeShapeCasePolicy | UppercaseOnly, UppercaseOrGuess, LowercaseOnly |
| ImeShapePlacement | PerCharacter, AfterWord, Both |
| ImeShapeLengthPolicy | OptionalOne, OptionalTwo, OptionalOneOrTwo, RequiredTwo |
| ImeCandidateSortPolicy | PhoneticFirst, ShapeFirst, Automatic |
| ImeAutoCommitPolicy | Off, Four, Unique |
| ImeFuzzyRule | ZhZ, ShS, AngAn, EngEn, UanUang |
| ImeInterpretationKind | Complete, Partial, Prefix, IncompleteShape |
| ImeEvidenceKind | Phonetic, PerCharacterShape, AfterWordShape, Fuzzy |

`public sealed record ImeInputOptions`：public required init属性CasePolicy:ImeShapeCasePolicy、Placement:ImeShapePlacement、LengthPolicy:ImeShapeLengthPolicy；public init属性SortPolicy:ImeCandidateSortPolicy=PhoneticFirst、AutoCommit:ImeAutoCommitPolicy=Off、EatConfirmationSpace:bool=false、EnableLegacyXx:bool=false、FuzzyRules:IReadOnlyList<ImeFuzzyRule>=空。required禁止忘记三维策略；没有可变全局Default。RequiredTwo仅PerCharacter合法；Four额外要求Projection.IsFixedTwoKey=true。undefined枚举、重复非法数据及非法配置抛ArgumentException（C09），null抛ArgumentNullException。重复已知fuzzy规则规范化去重。

新构造为 `public ImeContext(IImeDictionary dictionary, ImeInputOptions options, Action<ImeDictionaryLearning>? learn)`，第三参数必须显式传null或回调，不给默认值；这样现有 `new ImeContext(dictionary, null)` 无重载歧义。配置必须被防御复制。投影由词库能力提供，不在上下文重复指定相互冲突的InputScheme。

### 9.3 ImeCore命名空间：候选、查询、快照、选择

| public类型 | 冻结签名/只读属性 |
|---|---|
| readonly record struct ImeInputRange | `(int Start, int Length)`；public int End { get; }；检查非负及加法溢出 |
| readonly record struct ImeSelectionId | `(Guid ContextId, long Revision, int Ordinal)` |
| sealed record ImePathEvidence | `(ImeEvidenceKind Kind, ImeInputRange Range, int SyllableIndex, string Input, string CanonicalReading, string? ShapeAlias)` |
| sealed record ImeCandidateRank | `(int CompletionPriority, int CoverageEnd, int SourcePriority, int StrategyPriority, int Frequency)` |
| sealed record ImeInterpretation | `(ImeInterpretationKind Kind, int CoverageEnd, ImeInputRange? ConsumedRange, string RemainingInput, IReadOnlyList<ImeConfirmedSegment> Segments, IReadOnlyList<ImePathEvidence> Evidence)` |
| sealed record ImeConversionCandidate | `(ImeCandidate Candidate, ImeSelectionId? SelectionId, IReadOnlyList<ImeInterpretation> Interpretations, ImeCandidateRank Rank)` |
| sealed record ImeConversionPage | `(IReadOnlyList<ImeConversionCandidate> Candidates, int TotalCount, int Offset, int Limit)`；public bool HasMore { get; } |
| sealed record ImeDetailedSnapshot | `(ImeSessionSnapshot Session, IReadOnlyList<bool> UppercaseMarks, ImeConversionPage Page, long Revision)` |

Q/H的ConsumedRange和SelectionId为null，RemainingInput固定空串（不是伪造授权残留）；CoverageEnd为已有输入长度。E/L的ConsumedRange=[0,b)，RemainingInput为原始串[b..]，不可选类型与可选类型不会合并。Interpretations保留每条合法不同证据路径，证据可去完全重复，不能做跨别名未登记拼接；数组按规范分段和证据字段ordinal排序保证稳定。范围一律相对本次组合，输出区间也以UTF-16计。SyllableIndex从0开始；词后证据对应词段末音节；纯音/fuzzy记录规范音与输入音节，shape记录规范完整别名（如一键r可有rx/re两条解释），各Range精确覆盖实际键，UppercaseMarks由原串逐位推导。

提交语义合并键为Kind、消费范围、提交文本、残留；Q/H按预测词身份、规范reading、CoverageEnd和未完成状态合并。E/L同文本不同b仍有独立SelectionId。同一上下文同Revision的重复查询/翻页ID不变，不随排序和页offset变；Ordinal按语义规范键序号分配而非显示下标。跨上下文/旧Revision/伪造或不可提交ID均无提交、无学习、状态不变。修改输入、成功消费、Reset、规则更新改变Revision；仅查询、翻页/选择移动不改变Revision。词库外部修改在下一次查询/快照显式刷新时建立新Revision并废止旧ID，图使用一致词库版本，不允许读取半次学习状态。

ImeContext新增public成员：

- `ImeDetailedSnapshot DetailedSnapshot { get; }`：页为当前9项页；legacy可读取，解释集合为空，不伪造新模式语义。
- `ImeConversionPage QueryInterpretations(string composition, int offset = 0, int limit = 100)`：仅新模式，纯只读，保留字母大小写，不替换会话，不自动提交/学习。对等于当前原始输入的查询给当前Revision可选ID；非当前输入的查询ID均null，结果可用于断言不可用来提交另一个输入。legacy调用抛InvalidOperationException。
- `IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 100)`：复用IShapeDictionary能力，独立纯形意图，rx/RX一致；不支持shape能力抛NotSupportedException，不静默空冒充无命中；maxCount1..100。
- `ImeProcessResult SelectCandidate(ImeSelectionId selectionId)`：Pick入口；成功仅消费所选前缀，残留标记逐位保留，光标为max(0,旧光标-b)，重新查询剩余，成功不再递归自动提交剩余。无效ID返回Handled=false及原状态。
- `ImeProcessResult UpdateFuzzyRules(IReadOnlyList<ImeFuzzyRule> rules)`：仅新模式，防御复制/去重，立即重新解码，原串/光标不变，返回Handled=true、CommitText=null、学习0；不支持派生投影则NotSupportedException且规则/状态原子保持旧值。
- `ImeProcessResult Reset()`：Handled=true、CommitText=null，清组合/候选/光标/选择/令牌，保留构造选项与当前规则；legacy等价现有取消/清空，不改共享词库。

新模式SetComposition仅接受空串清空，或原串完全相同的无操作；其他非空批量replacement返回Handled=false且原状态不变。本批明确延后粘贴/批量来源语义，不能隐式用它代替矩阵逐键T。字符入口只接ASCII字母，新模式非字母交还；legacy符号/纯形/xx合同原样保留。Space无E/L时Handled=true且无提交/学习、保持组合；空闲Space沿legacy为false。候选移动可以观察Q/H但不能提交；First/Last/翻页按全体展示序处理。Enter提交原串不学习。

### 9.4 Dictionary命名空间：同能力真实词库与projection

优先合理扩IImeDictionary，新增 `ImeLexicon GetLexicon()` 默认实现抛NotSupportedException；不新增能力interface或adapter。仅启用新模式才调用它，legacy自定义实现保持可用。InMemoryImeDictionary、CompiledImeDictionary、UserDictionary各直接实现；UserDictionary必须复用原system词库规范条目并叠加规范学习频率，不能新增wrapper。已有IShapeDictionary/ISymbolDictionary保留，InMemory扩展构造必须同时真正实现IShapeDictionary。

新增public类型及签名：

| 类型 | 签名/属性 |
|---|---|
| sealed record ImePhoneticEntry | `(string Text, string CanonicalReading, int BaseFrequency, DictionaryCandidateSourceKind SourceKind = System, int UserFrequency = 0)` |
| sealed record ImeProjectionEntry | `(string Initial, string Final, string Input)`：规范reading按已有最长声母分解；空声母允许；精确映射也保留组件身份 |
| sealed class ImeProjectionDefinition | `public ImeProjectionDefinition(string name, IReadOnlyList<ImeProjectionEntry> entries)`；public string Name { get; }；public IReadOnlyList<ImeProjectionEntry> Entries { get; }；public bool IsFixedTwoKey { get; }；public static ImeProjectionDefinition FullPinyin { get; }；public static ImeProjectionDefinition XiaoheDoublePinyin { get; } |
| sealed record ImeLexicon | `(IReadOnlyList<ImePhoneticEntry> Entries, IReadOnlyList<ShapeDictionaryEntry> ShapeEntries, ImeProjectionDefinition Projection, long Version)`；另public init属性LookupAliases:IReadOnlyList<ImeLookupAlias>=空 |
| sealed record ImeLookupAlias | `(string Text, string CanonicalReading, string Input)`：显式缩写别名，必须引用已登记规范词条 |
| sealed record ImeConfirmedSegment | `(string Text, string CanonicalReading, int InputStart, int InputLength, int OutputStart, int OutputLength)` |

ImeProjectionDefinition是数据定义而非服务抽象，不公开独立编码器接口。内置定义复用现有编码规则；自定义F/X/CONFLICT可直接列表构造。组件→键表保留一对多/多对一碰撞；对规范音先查精确条目，再对规则闭包查输入组件映射。FullPinyin可编码全部明确定义派生组件，Xiaohe按现有组件表生成规则所需条目含(z,uang,zl)。F/X不声称支持其表外模糊组件；启用规则有词条派生组件不可编码就明确失败，不删路径。自定义投影完整条目有限且Input非空；重复相同组件/键去重，冲突的不同输入作为合法别名，不丢反向路径；固定双键要求全部Input恰2字母。内部复用现有PhoneticInputProjection，不把该internal类提升public。

InMemory新增 `public InMemoryImeDictionary(ImeLexicon lexicon)`，旧IEnumerable<ImeCandidate>构造保持legacy；旧构造GetLexicon不可假定reading=xx是规范音，抛NotSupportedException。可直接构造规范条目、固定频率、shape、F/X投影，无磁盘。同一人工词库可交给真实compiler加载流程，不限定fixture。频率重复规范词条不得叠加（按最大频率及来源优先规范化）；shape别名完全重复去重。

编译新增不同名入口避免旧Compile重载null歧义：`public static DictionaryPackageManifest CompileLexicon(ImeLexicon lexicon, string outputDirectory, IReadOnlyList<DictionaryPackageSource>? sources = null)`。返回的包可经现有 `DictionaryPackageLoader.Load(string packageDirectory)` 加载；CompiledImeDictionary.GetLexicon必须恢复规范音、shape、固定频率、完整projection，同输入结果与内存一致。新格式版本2保存完整projection及规范词条/来源（User持久更新不编进系统包），读取v1兼容；旧v1可从全条目与已知InputScheme构造规范能力，不能借MaxPrefixCandidatesPerKey漏结果。编译接口现有Compile及Load签名不改；需升级格式时旧包继续可读，未知格式/缺元数据用既有DictionaryPackageException，不fallback到假精确词库。内存/编译相同能力是本批范围，磁盘roundtrip测试是必要真实集成例，路径隔离，不读取正式词库。

LookupAliases供H09的xx普通查询/快捷触发复用：词条小希规范xiao xi，别名xx；“只载xx缩写”指仅此词及此别名，而不是将xx当规范音。别名可产生完整E且手动学习规范xiao xi，不参与声韵fuzzy展开、不构造逐字shape或Four资格；开启xx快捷触发仅改变逐键自动行为。CompileLexicon必须保存并Load恢复LookupAliases；旧v1中不能关联规范reading的缩写仅legacy可用，启用新模式须明确拒绝，不从Text任意猜读音。附加H09参数验证关闭快捷仍可手动选择xx，一次Segments规范xiao xi；开启一次自动学习同规范音。

ImeDictionaryLearning位置构造仍 `(ImeCandidate Candidate, string OriginalInput)`，仅增加 `public IReadOnlyList<ImeConfirmedSegment> Segments { get; init; } = 空`。既有UserDictionary.Learn签名和Host持久对象不改；新Segments由core构造，经同一个回调传递，Host无须新增协议。实际生产使用的回调必须原样将对象送Learn，不拆成每路径事件。

## 10. 72行最终用例审定与补充红测试期望

A–H原矩阵仍为72设计行，所有已有exact、消费边界、事件和证据均保留；“未定”按本节覆盖，不删除合法路径。矩阵的exact仍指E集合，不凭本节宣称其他类都不存在。完整/局部/前缀/H全集另外通过穷举图oracle验证；未列非E集合的行不可写成整份候选空。

| 行范围 | 本轮最终裁定（可直接展开红测试） |
|---|---|
| A01–A08 | 全部原预期冻结；A08/D05的同消费c/w解释合并显示，证据均保留 |
| A09 | E你好吗@8频率60先于L你好@6频率120；Pick局部剩ma。另选择完整序列一次学习事件含两段，不新增你好吗词条 |
| B01–B09 | 原大小写/编辑/shape预期冻结；独立QueryShape不变更Revision/组合/选择/学习；B06复杂任意混排明确延后，不阻断该简单参数 |
| C01–C08 | 原E/H预期冻结；H的ConsumedRange/SelectionId=null，不可Space/Unique/Four提交；nirs不得有N的合法H |
| C09 | 四非法构造均ArgumentException；不静默降级；规则不可投影另测NotSupportedException |
| D01–D09 | 原E/L/Q及剩余冻结；D01 E前Q，D02/D09 E前L，D06 A@5/E前A@4/L，两ID不同；D07学规范ni hao一次且remaining r无候选，D08消费5 |
| E01–E05 | 全部原exact/order冻结，limit仅最终分页；E02/E03频率40/120，不按证据增权 |
| E06–E07 | E06 A,O且9000/600；E07 O,A且600/1；重复查询/投影登记顺序反转输出一致 |
| E08–E09 | 三新策略同纯音B,A；legacy旧来源层级/平局/Text去重不变 |
| F01–F06 | 原Z/TS全部集合及固定频率顺序冻结；F06/H08全部16参数×16词，按实际输入长度消费，zuang规范reading不写回 |
| F07–F09 | 更新规则立即刷新且保留原串光标；关闭恢复精确；规则/词库隔离，重复规则不增频，Reset保留当前规则 |
| G01–G04 | 原局部残留/光标/大小写逐位预期冻结，编辑/删除无学习；候选旧ID失效 |
| G05–G06 | 只发一次带完整规范词段Segments的事件；G06 OriginalInput=zan、CanonicalReading=zhang；多解释不重复学习 |
| G07–G09 | Reset保留配置/规则清所有组合派生状态；Enter原串学习0、Esc无Commit学习0；Reset后旧ID不得提交 |
| H01–H03 | Off仍唯一不提交；Four第4键只选择完整登记N；Unique仅E，不计半码H |
| H04–H07 | Unique牺牲更长前缀已授权；Eat1仅自动提交一次空格，所有非空格调用到达时解除（包括不Handled）；手动提交不设令牌 |
| H08–H09 | 全部真实投影、组件碰撞、xx显式参数保留；新xx触发与Four/Unique合计最多一个提交/学习，旧替换不触发 |

额外单行为测试（不冒称已存在，也不改72行计数）：

1. Four多候选：N40与新增人工“拟”/ni/shape rx/频率80，同F+C+M2，T(niRX)第4键提交拟一次。相同频率时按既定稳定平局键提交固定文本，与加载顺序无关。
2. Four长词取舍：{N,A}第4键niRX提交你，A的未完路径不阻止；仅{A}第4键无提交、第8键niRXhcNZ提交你好一次；仅登记人工三字“你好我”/ni hao wo/100，无短词，niRXhcNZwoWT第12键提交完整词，之前不提交。
3. Four不半词：仅A的niRX为H且不可提交；仅A的8键双字E可提交，但插入中间/Backspace/规则更新不能触发Four；下一串尾合法字符插入才重新检查全串资格。
4. Q/H选择：D01查R/Q，C02查N/H，SelectionId=null；Space若只剩Q/H不Commit、不Learn、不动光标；无效/跨上下文/过期ID相同无副作用。
5. 规范序列学习：A09完整选择一次事件两Segments；真实UserDictionary频率A/M各+1，没有你好吗条目及nihcrnma键；G06没有zan新reading；局部remaining不学习。
6. 完备性：人工101个同音合法词，低频目标唯一shape匹配仍可见；无shape时TotalCount=101，页offset100得最后一项、HasMore=false；Unique不得因第一页仅显示一个就触发。旧编译prefix缓存限制1与内存结果仍同全集。
7. 穷举对照：短输入有限字母表、枚举所有合法分段/shape/模糊闭包作oracle；新查询全页集合及证据与oracle一致。oracle只在测试实现，不新增生产公共“测试引擎”。包括同键组件碰撞和低频硬约束。
8. 安全边界：64字母可查询/逐键输入，65键插入Handled=false且快照完全不变；offset负/limit0/101抛ArgumentOutOfRangeException；无支持词库新构造NotSupportedException，legacy构造仍可用。
9. Roundtrip：同人工ImeLexicon内存与CompileLexicon→Load，F/X投影、规范reading、shape、频率、TS/真实投影碰撞、新查询结果等价；绝不以mock CompiledImeDictionary冒充。旧v1加载/legacy xx与原排序回归另测。
10. 新非空SetComposition拒绝且不猜粘贴大小写；同串no-op与空串Reset明确测试。实际启用新模式的逐键路径不借SetComposition造输入。

所有本批设计行现在可执行；明确延后仅CapsLock/粘贴来源、任意复杂L混排、自动纯形混排、组合中改构造选项、UI/Host协议/安装、完整整句LM。没有需用户再选择才能关闭的算法阻断；这里选择的Four长词牺牲/A频率比较都是最小确定算法版本，已记录理由而非无限待定。

## 11. TDD实现顺序与主监督交接（阶段3最终）

严格 **公开API设计（本轮完成文档）→红测试→最小实现→回归**，不得直接先改解码器再从输出编测试。

1. 下一轮首先按第9节公开签名写测试项目调用；新API不存在导致编译红可记录为API缺失红，不伪称运行断言失败。然后仅加签名/校验桩使测试可运行，桩抛NotImplementedException，不写解码算法。
2. 第一条行为红测试：新InMemory(ImeLexicon)支持N3/U的niR低频硬过滤E01，输入逐键，期望N@3、规范ni、shape证据区间[2,3)，无提交学习。测试红后才实现词库规范能力与最小匹配。
3. 按C09/三维配置→A/B/C/D路径与可选身份→D07/G01局部消费→G05/G06规范学习→A09序列→E06/E07确定排序顺序推进，每次一个行为先红后最小实现。
4. F规则闭包/动态刷新及H08真实组件碰撞：先16×16全部预期红，再实现共享声韵层；不可删zuang、缩TS或靠fixture特判。
5. H02/Four补充多候选/长词/H不提交、Unique、Eat1：先固定事件和边界测试，再实现自动提交；最后legacy xx、旧SetComposition、旧排序与纯形/符号全回归。
6. 完备图/topK/101规模/64边界及oracle红测试先于相关优化；实现不得beam漏合法。公共查询分页、跨页身份、原子规则更新逐项测试。
7. CompileLexicon/Load真实roundtrip和旧v1回归先红，再实现格式2完整元数据；内存与编译同能力不能作为未来fixture便利延后。最后按相关项目构建及现有测试验证，实际结果逐条记录，未运行不宣称通过。

阶段3只改本文与主交接。第6节“未设计API/下一步处理阻断”为阶段2历史状态，由本节覆盖。没有生产代码、没有测试代码、没有签名桩、未构建/运行测试；文档人工核对不是独立代理复验，也不是72个测试通过。主监督下一步按第9–10节直接编写红测试；若独立复核发现额外合法路径，修正oracle/预期并记录原因，不能禁掉合法解释去凑矩阵。

## 14. 独立复核阻断与本轮修复登记

状态：本批引擎验收归档完成。R修复与30组合独立oracle在本轮开始前已落盘；本轮代码冻结，仅依序全量验证并同步本文第14节及主交接，没有新增功能、测试或生产修复。以下14.3为最终实测，优先于14.2、14.1及第12–13节历史计数/开放状态；第8–11节合同不改。R01有已登记的真实缺陷red→green；R02–R09没有取得修复前原始失败日志，不虚构red。本次全量green与已有合同证据足以完成本批引擎范围验收，不外推Windows、性能或无限输入。

| 复核ID | 合同映射 | 阻断及要求 | 状态 |
|---|---|---|---|
| R01 | E排序/D07学习 | 系统来源词条的UserFrequency须参与Decode排序 | 已验收；历史真实red→green，本次green |
| R02 | B双附着/D路径/E排序/G学习 | segments+evidence稳定身份保留整词与分词合法解释；最佳解释须为Interpretations首项且学习一致 | 已验收；本次green，red未取得 |
| R03 | G01/H前缀 | Q/H分组包含规范reading及未完成状态，禁止合并同文不同读音 | 已验收；本次green，red未取得 |
| R04 | F/Four | 在合资格E子集中选首；完整多词段不因Segments.Count被禁 | 已验收；本次green，red未取得 |
| R05 | F/xx/Eat1 | 显式xx快捷优先于Four/Unique，统一自动提交吞空格令牌 | 已验收；本次green，red未取得 |
| R06 | D07/D08/G05 | 学习按词段出现次数计频；连续完整输出/输入边界顺序、已登记reading校验；整份无效原子拒绝 | 已验收；重复段+2及整份原子校验green，red未取得 |
| R07 | Dictionary/v1兼容 | 已知scheme和规范reading从完整candidate shard恢复lexicon，仅不可恢复缩写拒绝 | 已验收；两个scheme恢复110词green，red未取得 |
| R08 | Dictionary/配置边界 | 统一拒绝空reading、空lookup alias、负frequency、悬空alias；编译前验证，Load转精确package异常，不用深度截断掩盖零消费 | 已验收；validator回归green，red未取得 |
| R09 | A–H/完备性 | 独立短输入oracle覆盖case/placement/length/fuzzy及同文多音多段，不调用生产helper构造期望 | 已验收有限oracle范围；30组合green，red未取得 |

R06裁定依据：第2.4节和第8–11节的一次实际提交只发一次学习事件，并不去掉该事件内重复词段的出现次数。重复规范词段出现两次应+2；第13.2节`RepeatedCanonicalSegmentInOneEventIncrementsOnlyOnce`及其+1结论为已撤销的错误历史，当前`RepeatedCanonicalSegmentInOneEventIncrementsTwice`已验证+2，没有削弱断言。

此前CS8620及xUnit1026已在本轮开始前修复，本次Release不再报告这两类；未suppress。既有CLI19警告如实保留。五项目全量精确数、Release错误/警告分类和实际红绿依据均已回读登记于14.3。

### 14.3 最终全量验证与归档（本批最终事实）

本轮按用户指定顺序执行无筛选全项目测试，每次立即完整读取日志。全部为Debug/net10.0，退出码均0；无真实失败，故未修复/新增任何代码或测试。

| 顺序 | 项目路径（相对XiaoXiIme） | 通过 | 失败 | 跳过 | 总计 |
|---|---|---:|---:|---:|---:|
| 1 | tests/XiaoXiIme.ImeCore.Tests/XiaoXiIme.ImeCore.Tests.csproj | 371 | 0 | 0 | 371 |
| 2 | tests/XiaoXiIme.Dictionary.Tests/XiaoXiIme.Dictionary.Tests.csproj | 163 | 0 | 0 | 163 |
| 3 | tests/XiaoXiIme.IntegrationTests/XiaoXiIme.IntegrationTests.csproj | 113 | 0 | 0 | 113 |
| 4 | tests/XiaoXiIme.ImeIpc.Tests/XiaoXiIme.ImeIpc.Tests.csproj | 27 | 0 | 0 | 27 |
| 5 | tests/XiaoXiIme.ImeModule.Tests/XiaoXiIme.ImeModule.Tests.csproj | 148 | 0 | 0 | 148 |
| 合计 | 五个全项目；不重复累计oracle等子集 | 822 | 0 | 0 | 822 |

随后构建`XiaoXiIme/XiaoXiIme.slnx` Release：退出码0，0错误、19警告。76行完整日志全部回读；警告汇总重复展示不重复计数。CS8604×1来自CLI `WindowsImeInstaller.cs(358,54)`；CA1416×18为Windows专用调用（CLI×12，CLI.Tests×6）。项目分类为CLI 13、CLI.Tests 6；无CS8620、xUnit1026，本轮未抑制警告。既有CLI19警告真实保留，不扩修复范围。测试日志亦已全部读取，无待读日志。

逐项证据：Core前缀`tests/XiaoXiIme.ImeCore.Tests/`，Dictionary前缀`tests/XiaoXiIme.Dictionary.Tests/`；以下全部包含在上述全量green，不冒称另跑筛选。

| ID | 实际测试名/文件 | 验证事实与red/green |
|---|---|---|
| R01 | Core `SoundShapeReviewRegressionTests.R01_LearnedSystemEntryRanksAheadOfUnlearnedSystemEntry` | 学习系统词你后优先于未学习泥；14.1记录有效行为red为Expected你/Actual泥（1失败），修复后green；本次green。夹具/编译错误不算缺陷red。 |
| R02 | Core `SoundShapeReviewRegressionTests.R02_WholeWordAndSegmentedPathsBothSurvive`、`R02_BestInterpretationMatchesCandidateAndLearning`；`SoundShapeCompletenessTests.Pagination_SelectionIdentityDoesNotDependOnPageOffset` | 你好同时保留1/2词段；最佳nin与学习CanonicalReading一致；同一候选跨分页SelectionId相同。身份编辑失效另有`SoundShapeSessionTests.Selection_QueryPreservesIdentityButEditInvalidatesOldIdentity`，该方法实际断言编辑后拒绝旧ID，不将名称冒称额外查询稳定断言。本次green，修复前red未取得。 |
| R03 | Core `SoundShapeReviewRegressionTests.R03_PendingHomographsKeepCanonicalReading`（n/Prefix、niR/IncompleteShape）、`R03_PendingWholeWordAndSegmentedStatesStaySeparate` | Q/H同文ni/nin规范reading均保留；nih未完成整词与已确认1段路径分开。本次green，red未取得。 |
| R04 | Core `SoundShapeReviewRegressionTests.R04_FourCommitsCompleteMultiSegmentPath`；`SoundShapeAutoCommitTests.Four_ChoosesRankedRegisteredWordAndNeverCommitsHalfWord` | 通过插入/移动光标构造完整多段后Four提交你好；排序词与半词边界实测。`ImeContext.SoundShape.cs`回读确认Four对complete集合FirstOrDefault合资格完整2形证据路径，不以Segments.Count限制多段。本次green，red未取得；不宣称独立覆盖所有eligible排序排列。 |
| R05 | Core `SoundShapeReviewRegressionTests.R05_XxShortcutSetsEatConfirmationToken`；`SoundShapeAutoCommitTests.H05_EatTokenConsumesOnlyFirstConfirmationSpace` | 显式xx快捷设置吞确认空格令牌；自动提交只吞首个确认空格。本次green，red未取得。 |
| R06 | Dictionary `SoundShapeLearningTests.RepeatedCanonicalSegmentInOneEventIncrementsTwice`、`InvalidLearningKeepsVersionAndExistingFrequencyUnchanged`、`SegmentsLearnCanonicalKeysWithoutInventingSentence`；`SoundShapeReviewRegressionTests.R06_InvalidPayloadIsAtomicallyRejected`（gap/overlap/reading/incomplete）、`R06_InputGapRejectsEntireLearningEvent` | 重复规范段一次事件+2；输出连续完整/输入边界/已登记reading不合法整份拒绝，无部分学习，版本频率不变。第13节旧+1测试名及结论已撤销。本次green，red未取得。 |
| R07 | Dictionary `SoundShapeReviewRegressionTests.R07_V1RecoversAllCandidatesBeyondPrefixCache`（fullPinyin/xiaoheDoublePinyin） | v1以MaxPrefixCandidatesPerKey=1编译110词，GetLexicon恢复110，不使用截断缓存当全集。本次green，red未取得；此fixture不是所有任意scheme/不可恢复缩写的穷尽证明。 |
| R08 | Dictionary `SoundShapeReviewRegressionTests.R08_InvalidLexiconIsRejectedBeforeUse`（reading/alias/frequency/dangling） | 空reading、空alias、负频率、悬空alias拒绝。回读`ImeLexiconValidator.cs`、InMemory调用、CompileLexicon经InMemory预验证、Loader将ArgumentException转DictionaryPackageException。本次green，red未取得；不把实现回读称为新增损坏package行为测试。 |
| R09 | Core `SoundShapeIndependentOracleTests.R09_SmallGrammarMatchesCanonicalPathsEvidenceAndConsumption`（Policies 30参数）；已有`SoundShapeOracleTests.FiniteAlphabetMatchesIndependentELQHSegmentsAndEvidence` | 新oracle显式终结符独立递归产生期望，不调用生产projection/fuzzy/Decode helper。3case×3placement×3optional长度+3case×PerCharacter×RequiredTwo=30组合；每组合25固定输入（750次内部对照，不额外计作750测试），词库你/ni、你/nin、你你/ni ni、张/zhang，ZhZ/AngAn交叉映射va/za/vn/zn，同文多音、多段、词后/逐字形、E/L/Q/H均比较text、Kind、CoverageEnd、ConsumedRange、remaining、Segments、Evidence。另保留niRX字母表长度0–4的341输入oracle。全量green，red未取得。不是64键所有字符串/词库/模糊规则的穷尽。 |

公开入口与人工复现：显式`ImeContext(IImeDictionary, ImeInputOptions, Action<ImeDictionaryLearning>?)`，`DetailedSnapshot`、`QueryInterpretations`、独立`QueryShape`、`SelectCandidate(ImeSelectionId)`、`UpdateFuzzyRules`、`Reset`在`src/XiaoXiIme.ImeCore/ImeContext.SoundShape.cs`；options/enums/路径/身份在`ImeInterpretation.cs`。Dictionary规范模型`ImeLexicon.cs`、`IImeDictionary.GetLexicon`、`ImeDictionaryLearning.Segments`，内存构造/`DictionaryPackageCompiler.CompileLexicon`/`DictionaryPackageLoader.Load`及`UserDictionary`为生产合同，非另造引擎。

人工稳定复现使用`InventedSoundShapeFixture.cs`的固定规范reading/shape/频率和F/X人工投影，不依赖正式词库、不称人工hk/kr/rh为真实小鹤；逐键`ImeKey.FromCharacter`输入，经QueryInterpretations按语义选择而非猜ordinal，再SelectCandidate核对规范Segments/消费/学习。R02固定ni/nihc，R03 n/niR/nih，R04按测试内光标序列，R06 niRniR得到规范你/ni计数2，R09固定25输入和30参数均可按现有测试稳定复现；这是自动测试已验证的复现fixture，不冒称本轮人工桌面操作。

legacy与package生产回归：`ImeContextTests`、`CompiledDictionaryCandidateSelectionTests`随Core全量通过；Dictionary全量包含规范CompileLexicon→Load；Integration全量包含`ProductionDictionaryKeystrokeTests`的真实全拼/小鹤package经IPC逐键，以及`ImeHostIpcIntegrationTests`的package加载、会话、学习兼容。未改变legacy默认构造、xx、SetComposition、Host持久格式或正式词库。Integration既有headless/UI模型测试随指定全量执行，不等于运行真实Windows/UI场景；本轮未改UI、SDK、包、TFM，未执行Windows沙箱/安装。

结论：本批引擎合同范围验证与两文档归档完成，822/822、fail=0、skip=0、Release error=0，R01–R09阻断按上述有限证据关闭。未批准性能/压力/无限图任务未执行，不声称性能达标；复杂混排/粘贴来源/组合中策略切换、整句LM、Windows UI/安装与完整产品验收仍在范围外。集合防御复制、所有反向登记总序及任意eligible排列不因本次green被宣称穷尽。归档后代码继续冻结，没有未同步最终文档或待读结果。

### 14.2 续轮起点：实际代码与证据（最终验收前，历史）

已回读上一轮最终 Core 日志：退出码0，340通过、0失败、0跳过；CS8620×1（Matrix tuple nullable不一致）、xUnit1026×9（Theory未用id）。当前落盘包含R02整词/分词与最佳reading、R03同文多音Q/H、R04多段Four、R05 xx吞确认键回归；生产改动及Dictionary R06–R08需本轮重新核对，不把已有代码等同验收完成。上一轮真实缺陷red原始日志尚未取得，除14.1的R01之外不虚构red计数；已有340绿不是新增测试先red的证据。Dictionary最终结果尚未回读/复跑，R02–R09仍开放。以下14.1及第13节计数为历史，重复段+1裁定已由R06明确撤销。

### 14.1 批次1：R01实际红绿记录

新增 `SoundShapeReviewRegressionTests.R01_LearnedSystemEntryRanksAheadOfUnlearnedSystemEntry`（E排序/D07）：同reading的系统词你40、泥500，学习你一次后应你优先。初次测试参数顺序编译错误及夹具不存在ND引发的异常已修正，二者不算缺陷红。有效回归实跑1失败/0通过，断言Expected你、Actual泥，证明学习未参与排序。

根因修复：`UserDictionary.GetLexicon`将实际有学习频率的规范词条提升到User来源；无学习词条保留原来源，缺少覆盖频率时保留原UserFrequency。遵循8.2来源层级及User频率，不加混合评分，也不修改legacy查询。

修复后实跑Core全量334/334、Dictionary全量152/152，失败0、跳过0，退出码0。Release解决方案构建退出码0，0错误、10警告（本轮增量构建仍报告CS8620×1、xUnit1026×9；CLI未重编译，不把增量警告数冒称干净全量19警告已消失）。Integration/IPC/Module本批未重跑，旧数仅历史依据。R02–R09及测试警告仍开放，不宣布合同关闭。

## 13. 阶段5续作即时记录（优先于阶段4历史状态）

本轮首先准确回读最后日志：Core筛选 `FullyQualifiedName~SoundShapeOracleTests`，退出码0，通过2、失败0、跳过0；含短输入完整路径枚举和外部学习后旧selection失效。日志含CS8620及xUnit1026测试警告。用户交接SoundShape253/253、Dictionary合同7/7、UserSegments3/3属此前结果，本轮全量复跑尚待执行；不将完整路径oracle误称L/Q/H/证据全覆盖。独立代码复核由主智能体另派。

### 13.1 本轮最终实测（覆盖第12节历史未运行/缺口状态）

全部代码修改先于以下最终运行；之后只更新本文与主交接。每个项目顺序执行并立即读取日志退出码和非零测试数，均为Debug/net10.0，退出码0、失败0、跳过0：

| 目标 | 筛选 | 通过/总数 |
|---|---|---|
| ImeCore.Tests 新音形 | FullyQualifiedName~SoundShape | 289/289 |
| ImeCore.Tests 全部 | 无 | 333/333 |
| Dictionary.Tests 全部 | 无 | 152/152 |
| IntegrationTests 全部 | 无 | 113/113 |
| ImeIpc.Tests 全部 | 无 | 27/27 |
| ImeModule.Tests 全部 | 无 | 148/148 |

五个全项目共773个通过；SoundShape289是Core333子集，不重复加总。Core其余44个既有测试随全项目回归，legacy生产代码及旧测试未改。合同7例、学习现5例包含在Dictionary152，不将此前用户交接7/7、3/3冒称本轮独立筛选结果。既有Off/Four/Unique/Eat1、模糊开关/更新、编辑/reset、Q/H不可选、真实全拼/小鹤、规范CompileLexicon→Load等测试随本轮全筛选实跑。

最终 `XiaoXiIme.slnx` Release构建退出码0，0错误、29警告：CS8620×1（SoundShapeMatrixTests第30行nullable元组）、xUnit1026×9（需求ID参数未使用）、CS8604×1（CLI WindowsImeInstaller第358行）、CA1416×18（CLI及其测试Windows平台调用）。构建日志共70行，输出首6274字符被工具显示限制截断，但末尾完整警告汇总及29/0计数已回读。不宣称无警告，不因本轮无关CLI警告扩修改范围；未执行Windows UI或安装场景。

### 13.2 新增测试与ID映射、红绿依据

- C09/B大小写/C长度：`SoundShapeEvidenceTests.Configuration_AllLegalDimensionsSelectAndLearn`，30合法case×placement×length组合逐键输入、完整选择、消费清空、规范reading与一次学习；不再仅construct。首跑oracle+配置33例为6通过/27失败，根因新测试`Single(SelectionId.HasValue)`把合法L与E共同计入，准确修订为选择完整E，不过滤生产合法路径。随后同筛选33/33通过，最终全SoundShape289/289。此红是测试预期错误，不是生产缺陷红。E01/D07既有行为红→绿、195/1为用户交接历史，未冒称本轮制造的红。
- A09/D局部/G01/H前缀半形：`SoundShapeOracleTests.FiniteAlphabetMatchesIndependentELQHSegmentsAndEvidence`，独立单词库你/ni/rx，字母表niRX、长度0–4共341输入，逐路径对比E/L/Q/H、CoverageEnd、ConsumedRange、remaining、规范Segments、全部phonetic/shape证据及range；预期仅由测试枚举生成，不调用生产解码求预期。已有短输入完整路径oracle仍保留；oracle共3例包含在最终289。
- D07/D08/G05规范学习：`SoundShapeLearningTests`原3例，加`InvalidLearningKeepsVersionAndExistingFrequencyUnchanged`验证错误的后段大小写reading拒绝且版本/已存频率不变；`RepeatedCanonicalSegmentInOneEventIncrementsOnlyOnce`验证同词重复段一次事件只+1。与规范键/不造整句、后段失败原子性既有测试一起包含在Dictionary152。
- E01/完备性：`SoundShapeCompletenessTests.Pagination_AllPagesMatchUntruncatedSemanticSet`明确100+1两页联合101唯一语义及HasMore；既有100高频不遮唯一低频shape、跨页身份与Unique先全体计数均实跑。
- G身份：`SoundShapeSessionTests.Selection_ForgedOrdinalPreservesCompositionAndLearning`两个伪造ordinal（-1/int.MaxValue）验证拒绝/不提交/组合保留/学习0；已有外部学习版本失效、跨上下文、编辑/reset过期身份均实跑。

本轮仅改上述5个测试文件，无生产/API签名修改、无包/SDK/TFM修改、无UI/Host持久配置修改。没有为制造红回退生产实现。

### 13.3 公开API/实现范围与明确限制

公开合同文件：Core `ImeInterpretation.cs`（options/enums/range/selection/path/rank/page/detail）、`ImeContext.SoundShape.cs`（显式构造、查询、选择、规则更新）；Dictionary `ImeLexicon.cs`（规范条目/投影/alias/confirmed segment/版本）、`IImeDictionary.cs`（GetLexicon）、`ImeDictionaryLearning.cs`（Segments），以及`InMemoryImeDictionary.cs`、`CompiledImeDictionary.cs`、`DictionaryPackageCompiler.cs`、`DictionaryPackageLoader.cs`、`UserDictionary.cs`对应能力。实现沿现有ImeContext，`ImeContext.Decoding.cs`/`ImeContext.Fuzzy.cs`实现图及模糊闭包，未新增第二引擎。此次仅补测试，不代表已独立审查所有公开API。

最终剩余测试fail=0，编译error=0；不能据此宣布72行所有细节或完备图验收全部完成。有限ELQH证据oracle仅单音词/固定单shape/UppercaseOnly/PerCharacter/OptionalTwo且最长4键，不覆盖所有case/placement/RequiredTwo/模糊碰撞/多词全证据；合法30组合测试验证一次代表性选择学习，不是各模式所有状态的笛卡尔穷尽。旧v1截断缓存与101全集对照、集合防御复制、所有反向登记稳定序及第12节其他细化缺口未逐条关闭。独立代码复核由主智能体另派，本任务未代替其结论；以上属于需求内剩余证据，不另扩UI/Host/SDK/Windows验收。所有本轮实际结果已同步，无后续结果待回读。

## 12. 阶段4：测试交付、契约编译红与需求映射

本阶段仅写测试与交接，不新增生产实现、公开签名桩、包或项目引用。完整读取第8–11节及原72行后，以现有xUnit 2.9.3、net10.0、逐键字符入口编写。`InventedSoundShapeFixture`只在测试项目内，F/X人工投影与Full/Xiaohe明确分离；X的nihkr不是实际小鹤。Z保持4词，TS保持16词（含纂人工zuan），全拼及真实小鹤分别16输入，zuang/zl仅派生别名。每参数独立词库/会话，无正式词库依赖。所有方法使用单个Assert；元组断言表达一次状态转移的结果。以下映射表示已有断言，不表示覆盖全部合同或已运行。

### 12.1 文件与实际执行

Core路径前缀：`tests/XiaoXiIme.ImeCore.Tests/`。

- `InventedSoundShapeFixture.cs`：规范条目、F/X投影、完整shape别名及固定频率，Z/TS生成器，逐键和按语义身份选择助手。
- `SoundShapeMatrixTests.cs`：A/B/C/D完整集合、H非授权边界、C09非法配置、E01低频及证据、E04–E08排序、双附着证据。
- `SoundShapeEvidenceTests.cs`：逐字符大小写标记、E02/E03不增权、30个合法三维组合构造参数（3case×3placement×3optional length，另3case×C×RequiredTwo）。
- `SoundShapeSessionTests.cs`：D07/D08、局部remaining、学习一次及规范Segments、编辑、Reset、Enter/Esc、过期/跨上下文ID。
- `SoundShapeFuzzyTests.cs`：Z每规则组合×4输入、TS两真实投影各16输入、关闭/更新/重复规则/隔离及纯形不模糊。
- `SoundShapeAutoCommitTests.cs`：Off/Four/Unique、长词/多候选/半词、Eat1、真实投影及xx显式开关/手动学习。
- `SoundShapeCompletenessTests.cs`：101候选/低频shape/分页身份/Unique、64/65边界、只读查询校验、SetComposition限制、真实CompileLexicon/Load对照、Q/H Space拒绝。
- Dictionary路径：`tests/XiaoXiIme.Dictionary.Tests/SoundShapeLexiconContractTests.cs`，规范音/shape/频率/重复规范化、F/X投影真实编译加载、LookupAliases恢复、旧构造明确拒绝规范能力。

实际运行首红筛选 `FullyQualifiedName~E01_LowFrequencyShapeSurvivesLimitOne`，目标Core测试项目，三次均编译失败，退出码1；首次诊断 `SoundShapeMatrixTests.cs(121,59): CS0246 ImeShapeCasePolicy`。最后运行日志85行，显示限制丢弃前4075字符，保留部分首条为 `InventedSoundShapeFixture.cs(45,64): CS0246 ImeInterpretationKind`；还明确缺失ImeConversionCandidate、ImeFuzzyRule、ImeLexicon等，CS0103缺ImeAutoCommitPolicy。日志首部记录退出码1。不是行为红：筛选不会跳过项目其他文件编译，没有测试方法执行结果。

实际运行Dictionary筛选 `FullyQualifiedName~SoundShapeLexiconContractTests`，退出码1，日志13行，`SoundShapeLexiconContractTests.cs(72,20): error CS0246: 未能找到类型或命名空间名“ImeLexicon”`。Foundation/Dictionary生产程序集已构建，但测试项目未编译。没有运行通过数，没有独立子智能体复验，也没有对既有回归通过的声明。后续必须在正式API实现后重新编译，检查API缺失遮蔽的测试自身诊断，不能修改冻结API去迁就测试。

### 12.2 A–H全部72设计行映射（部分覆盖明确列出）

简写M=Matrix，E=Evidence，S=Session，F=Fuzzy，A=AutoCommit，C=Completeness；均指上述对应SoundShape文件。

| ID | 已写测试方法/参数与缺口 |
|---|---|
| A01–A07 | M.Matrix_CompleteSetHasFrozenOrder逐行参数；消费/各条精确证据与Off学习0未逐行穷尽，A03独立Pick尚缺 |
| A08 | M完整集合两参数、Matrix_MergedCandidateRetainsBothAttachments；所有精确range未穷尽 |
| A09 | M完整序列；S.A09_G01_LocalSelectionRequeriesRawRemainingInput、A09_CompleteSequenceEmitsOneLearningEventWithTwoCanonicalSegments；E/L顺序及min频率60尚缺 |
| B01–B06 | M逐行大小写集合，B05两参数；E.CaseMarks覆盖B02/B03/B05小写；B04/B05全部marks尚缺 |
| B07 | S.B07_G03_EditPreservesOriginalCharacterMarks；删除后完整集合空尚缺 |
| B08 | S.B08_B09_IndependentShapeQueryIsCaseInsensitiveAndReadOnly两参数；选择保持尚缺 |
| B09 | M.RX无完整集合；S独立纯形断言运行在niR而非RX，RX后纯形副作用完整断言尚缺 |
| C01–C03 | M每行三输入及C02半形非授权；C03所有别名range未穷尽 |
| C04–C06 | M完整集合与C05半形；无自动/无学习尚缺 |
| C07 | M两个空E及合法nir/H；nirs无合法H单独断言尚缺 |
| C08 | M两输入及半形；准确全部H集合尚缺 |
| C09 | M四非法配置；E合法30组合；F.C09_UnencodableFuzzyUpdateIsRejectedAtomically目前只断言异常，原子状态保持尚缺；undefined/null/防御复制尚缺 |
| D01–D02 | M完整集合及S独立Q/L；准确证据/排序尚缺 |
| D03–D04 | M硬约束完整集合；不合法路径证据逐项排除尚缺 |
| D05 | M完整集合及双附着；精确证据range尚缺 |
| D06 | M完整集合、S两身份不合并；E先L及完整各解释尚缺 |
| D07 | S.D07_D08_SelectionConsumesOnlyChosenBoundary参数4、D07_RemainingRHasNoCandidatesOrUppercaseMark；规范ni hao/消费nihk/残留r/学习一次明确断言 |
| D08 | S同方法参数5；规范ni hao/消费nihkr/空残留/学习一次；shape精确证据尚缺 |
| D09 | M逐键nirhcc含先nirhc后c、S独立L；中间快照精确证据尚缺 |
| E01 | M两E01方法+C101低频唯一shape；无Commit/学习0单独断言尚缺 |
| E02–E03 | E别名数/重复shape不增频；E02纯查询/编辑学习0尚缺 |
| E04–E07 | M.E04_E07_ConflictUsesFrozenSort四参数；重复查询/反向登记一致性尚缺 |
| E08 | M.E08_PurePhoneticOrderDoesNotRewardAliases三策略参数 |
| E09 | 复用既有DictionaryCandidateRankingTests：Rank_WhenLayersMixThenOrdersUserExactThenSystemExactThenPrefixThenFallback、Rank_WhenSameTextAppearsInUserAndSystemThenKeepsUserCandidate等；本阶段编译红，未执行回归 |
| F01–F04 | F.ZhangCases每规则4输入、全拼/小鹤各16参数；全部f证据精确范围尚缺 |
| F05–F06 | F.TsCases两投影各16参数×16规范词、频率、实际覆盖/残留；完整路径证据尚缺 |
| F07 | F关闭恢复精确/原串光标及更新无Commit学习；开规则后的独立4集合瞬时断言、词库快照保持尚缺 |
| F08 | F重复规则频率/纯形zg不模糊；shape zc正匹配尚缺 |
| F09 | F.ResetKeepsRulesButDoesNotAffectAnotherContext；共享同一词库及package不变尚缺 |
| G01–G02 | S局部重查、第二Space规范两次学习；G02 Commit文本独立断言尚缺 |
| G03–G04 | S编辑标记/光标/原串/学习与G04重查集合；G03错误shape证据排除尚缺 |
| G05–G06 | S查询移动/合并一次规范学习与模糊规范zhang一次；真实UserDictionary无zan键尚缺 |
| G07–G08 | S.Reset清状态/旧ID失效、两上下文shape隔离；选择初态全部字段尚缺 |
| G09 | S.Enter/Escape两参数原串提交/取消/不学习 |
| H01 | A.Off等待；A.H07手动提交Eat1不吞；H01 Space文本独立断言尚缺 |
| H02–H03 | A前3键/第4键Four及Unique参数、学习一次；全部前缀学习0尚缺 |
| H04 | A真实全拼Unique/Off参数，存在更长前缀提前提交 |
| H05 | A第1/2次空格、无Commit无新增学习 |
| H06 | A字母/Left/Esc/Reset/Other解除令牌；查询/翻页解除令牌尚缺 |
| H07 | A Eat关闭/手动两参数 |
| H08 | A真实音/shape两投影4参数；F真实小鹤Z及TS4×4全输入；C TS真实编译对照；跨组件碰撞独立穷举oracle尚缺 |
| H09 | A显式xx开/关及关闭手动规范学习；既有ImeContextTests的legacy逐键/编译xx、两投影项目词/SetComposition回归未运行；启用xx与Unique/Four合计一次、LookupAlias编译后行为尚缺 |

### 12.3 新增需求与未覆盖（不能称完成72行验收）

已写新增Four高频多候选、登记短词牺牲长词、仅两字第8键/三字第12键、半词第4键拒绝；101全体分页、最后页HasMore=false、跨页稳定ID、Unique全体计数；Q/H Space拒绝；过期/跨上下文身份；查询参数/非ASCII/65键；非空replacement拒绝/相同no-op/空清；F/X及TS真实CompileLexicon→Load内存对照。编译roundtrip使用随机隔离目录不清理，测试执行后路径可由调试locals取得，尚未增加ITestOutputHelper路径日志。

尚未写：独立有限穷举图oracle（包括全部L/Q/H与完整证据）；真实UserDictionary段学习原子更新/规范键/同词多次/频率A与M各+1/未登记整句不增加；Four同频稳定平局与反向加载、插入中间/Backspace/规则更新不触发；旧v1缓存限制1与内存101全集等价；词库外部版本刷新ID、伪造ID、只读非当前输入ID、跨页移动全遍历、集合防御复制、输入非法原子保持；重复查询/反向投影登记稳定总序。三case×三placement×长度目前为构造合法性参数，不等于30组合全部行为覆盖。已有文件可供独立审查，不能声称阶段4所有行为覆盖完成。

下一步由主智能体派独立执行子智能体审查并实现正式冻结API/引擎（本阶段不实现桩）。先恢复测试可编译，重新执行E01取得真实行为红再实现该行为；未覆盖断言按上述清单补充，不能以改测试合同/过滤合法路径/默认skip逃避。所有行为结果必须重新实测并记录，当前交付止于契约编译红。

