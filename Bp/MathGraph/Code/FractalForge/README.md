# FractalForge 当前实现与开发计划

当前已实现四点显式路径模型、Neural 训练接入、可恢复配对事务和四方向短程实验入口，**不再是工程骨架，也不代表完整研究已经完成**。研究规格参见 [历史文档索引](../../../Docs/FractalForge/README.md)；Docs 保持历史原貌，当前工程状态以实际代码、本页及 [集成验收记录](集成验收记录.md) 为准。用户已授权实现，历史文档阶段的“本轮不开发”不是当前限制。

## 工程与公共能力

- Core 引用 MathGraphs；Training 引用 Core/MathGraph.Neural；四方向为独立 Exe，只引用公共工程，方向间无项目引用。六个对应 Tests 使用 MSTest。
- 保持 net9.0、Nullable、现有 Directory.Build.props 与包版本；不新增训练器、包或插件架构。独立解决方案为 `MathGraph/FractalForge.slnx`，主解决方案为 `MathGraph/MathGraph.slnx`。
- 基线 I/A/B/O 为四个计算点，不包装原五点图；展开单元的 I/O 为透明边界，A/B 为计算点。ForgeRoute 明确保留独立信号路径、参数身份、延迟及平行边。
- ForgeModel 编译为普通 Neural 根图，复用 TrainTokens、EvaluateTokens、BPTT、AdamW 与生成会话。提供 clone-widen、逐来源 Identity output-deepen、正延迟 add-feedback，适用条件和预算不满足时拒绝。
- ForgeArchive 保存逻辑路径与完整数值/优化器检查点；PairedTrial 同源派生保持/候选、相同样本共同提交、固定终点 token 加权接受，拒绝时选已训练保持分支而非回滚旧源。
- 公共 ForgeRunner 提供 train/resume/inspect/evaluate/generate，严格配置、内容身份、显式清单恢复、方向/run 隔离及单写者锁。训练/接受/最终测试的身份、规范路径和内容哈希必须不同；最终测试仅声明，当前入口不打开也不用于接受。可选 ContentRole 提供精确文本角色校验；旧无角色配置仍仅有文件级隔离，近重复/全量划分仍需审计。
- 公共及 W0、Deepening、Adaptive 方向专用路径现已复用公开 ForgeJsonlStream：完整文件哈希验证后逐行读取，不缓存整集 TokenDataset；恢复按完整样本游标顺序重读前缀，跳过样本仍计入数据预算。接受评价也支持异步流。所有训练/评价以完整样本重置循环历史，结构切换不开旧状态兼容后门。
- ForgeCorpusPreparation 按解码后的精确 text UTF-8 字节 SHA256 **首字节 % 4** 固定角色：0 训练、1 提案、2 接受、3 最终测试（不是整个摘要整数取模）。不做空白/Unicode 归一化，同文本不跨角色，但不去除角色内重复或近重复。ContentRole 声明随已消费样本校验；旧配置不自动升级为内容级隔离。

## 四方向真实状态

| 方向 | 已实现 | 限制/未完成 |
| --- | --- | --- |
| Widening | W0 保持训练与恢复；W1 等分克隆；W2 显式不等分克隆；prepare、diagnose 及训练/恢复/评价/生成 | W3 扰动分化未实现且明确拒绝；局部相关性/加权 RMS 不是因果或损失贡献；没有完整训练后适应曲线报告 |
| Deepening | D0 保持；D1 逐来源 Identity 四点加深；D2 零门、D3 有界初始门的 Tanh 四点修正路径；配对、消融、冷却、Next API | D2/D3 保留父路径，是旁路修正，不是原路径完全替换的纯串行非线性加深；透明 O 只作定位/路由；D3 仅初始化目标预激活局部界；修正单元未登记 Core Units，门/目标由方向状态保存；CLI 尚无自动多轮总事务 |
| Feedback | 正延迟局部/跨层边；F1 全零样本重置训练；同模型 F2 生成恢复；F3 已见前缀重放 API | F1 不是“不变节点保留历史”的完整映射；跨结构 F2 明确拒绝；F3 尚无 runner 持久化协调或训练迁移；同会话生成历史不等于跨样本训练历史 |
| Adaptive | A0 固定结构；A1 随机；A2 训练损失平台启发式；A3 上下文评分及探索；普通更新/多配对窗口、审计及恢复 | 不使用局部梯度/激活统计；接受集重复选择存在偏差，未统计纠偏；审计清单有界，不支持无限增长；有界流式及完整样本恢复已接入；全研究总预算和长期运行验证仍待完成 |

入口命令、严格配置、退出码和协议细节见各方向交接及实际 Tests 夹具：

- [公共交接](公共实现交接.md)
- [Widening](FractalForge.Research.Widening/交接说明.md)
- [Deepening](FractalForge.Research.Deepening/交接说明.md)（含 `Experiments/v1/D2.template.json`）
- [Feedback](FractalForge.Research.Feedback/交接.md)
- [Adaptive](FractalForge.Research.Adaptive/交接.md)（含 `adaptive-policy.example.json`）

示例预算仅供短程验证，不是推荐超参数。相对路径按进程工作目录解析；恢复必须明确指定清单，不搜索最新文件。首次配置需登记真实 tokenizer、三个划分、哈希、动作、优化器及预算。

## 真实语料短程验证与环境依赖

源数据位于工作区 `minimind_dataset/pretrain_t2t_mini.jsonl`；`minimind_model/tokenizer.json`、`tokenizer_config.json` 为实际加载资产，目录也包含模型 Python 源码，但烟测不加载预训练模型权重，而是新建四点基线。RealCorpusIntegrationTests 依赖这些本地资产、可写工作区及 90 秒取消预算，不是无外部数据的可移植全自动测试。

本轮独立解决方案测试实际生成 `.coding-agent/fractalforge-real/a7e4eb3dfb7f4415961a6092e0bfb793/`。准备 32 条源记录、22814 字节，ReachedEnd=false：指纹仅是该前缀，不是全量语料。W0、D2、公共 Feedback add-feedback（配置标 F1）、A1 各 train 1 步再 resume 1 步，最多 8 token/样本，每方向游标 2、监督 token 14；W1 只保存配置未训练。提案/接受/最终测试角色文件被准备，但未用于提案选择、接受评价或最终测试；不能据此声称完成研究或取得收益。精确产物和测试结果见验收记录。

## 开发计划

- [x] 四点显式路径、真实 Neural 编译及公共结构动作。
- [x] 参数/矩保存、同源配对、固定终点接受和显式恢复。
- [x] 四方向真实入口、取消接线、短程 tokenizer/数据/恢复集成测试。
- [x] 最小集成修复及六个新测试项目；本轮重跑独立解决方案 Release 构建和新旧测试。主解决方案构建仅属此前记录，本轮未重跑。
- [x] W0、Deepening、Adaptive 接入有界逐行读取、异步接受评价及完整样本游标恢复；精确文本稳定角色和真实前缀短程烟测。
- [ ] 全量语料运行验证、跨平台不可变数据约束与大规模运行成本测量；短程流式接入不等于正式研究完成。
- [ ] 实现受验证的跨结构历史/时钟映射、完整 F1/F2，及 F3 持久化协调。
- [ ] 完成 W3、纯串行非线性加深/局部递归语法与公共单元元数据契约，再独立验证，不重命名现有旁路冒充完成。
- [ ] 连续多轮持久化调度、跨轮总 token/计算预算、审计分段和统一研究报告。
- [ ] 样本级去重/划分审计、多种子对照、长依赖与扰动恢复曲线、统计选择控制，以及隔离最终测试发布流程。

本轮没有运行正式长程训练，没有得到研究收益或假设成立的结论。精确验证数量与剩余阻塞见集成验收记录。
