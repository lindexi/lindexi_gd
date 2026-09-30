# XiaoXiIme.Dictionary.Tests

小希输入法词库测试项目。当前覆盖词库合同、源解析、package 编译与加载、查询排序、前缀和短语、按文本反查 reading、用户学习与遗忘、持久化，以及词典性能预算。

`DictionaryPerformanceTests` 使用确定性合成代表集，默认包含 25,000 个音码条目，并固定以下初始护栏：

- package 启动加载与校验耗时；
- 加载后的托管内存增量；
- 精确查询 P50/P95；
- 精确加前缀查询 P50/P95；
- 10,000 条用户词典快照的原子保存耗时。

预算可通过环境变量调整：

- `XIAOXIIME_PERF_ENTRY_COUNT`
- `XIAOXIIME_PERF_LOAD_MS`
- `XIAOXIIME_PERF_MEMORY_MIB`
- `XIAOXIIME_PERF_EXACT_P50_US`
- `XIAOXIIME_PERF_EXACT_P95_US`
- `XIAOXIIME_PERF_PREFIX_P50_US`
- `XIAOXIIME_PERF_PREFIX_P95_US`
- `XIAOXIIME_PERF_USER_SAVE_MS`

这些数值用于在正式词库入库前防止明显回退，不代表最终产品预算。正式 package 和目标测试机确定后，应使用真实数据重新校准默认值。
