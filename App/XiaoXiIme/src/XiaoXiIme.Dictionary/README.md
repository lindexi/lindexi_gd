# XiaoXiIme.Dictionary 小希输入法词库与候选服务项目。职责：承载词库算法适配，提供候选查询、排序和反馈学习能力，通过接口接入核心流程。

音码源 reading 会规范化为小写全拼音节，并用单个空格分隔。粘连全拼（如 `junding`）在可唯一编码为音节时会被切开。无法投影为小鹤音节的单 token 缩写（如 `xx`）会保留原 lookup key，因此全拼与小鹤 package 都可用 `xx` 命中“小希”。规范 5.4 术语保存在独立的 `phonetic/xiaoxiime-project-terms.phonetic.tsv`，不并入 SeWZC 转换产物。候选 shard 仍保留规范全拼 reading；全拼 package 用去掉空格的 lookup key 查询，小鹤 package 用编译期投影键查询，例如 `xiao xi ai mu yi` 对应全拼 `xiaoxiaimuyi`、小鹤 `xnxiaimuyi`。

查询先构造内部 `DictionaryCandidate`（来源、匹配类型、系统频率、用户频率），按用户精确、系统精确、系统前缀、回退分层排序后再映射为 `ImeCandidate`。同层再按频率降序、更短 lookup key、更短文本，最后用 `Ordinal` 文本决胜。`QueryByText` 按精确文本反查 reading，优先更长规范读音，再映射为 `ImeCandidate`。宿主按输入方案解析安装目录：默认 `XiaoXiIme.DictionaryPackage` 为全拼，`XiaoXiIme.DictionaryPackages/xiaoheDoublePinyin` 为小鹤；可用显式路径、`hostBaseDirectory` 或 `XIAOXIIME_INPUT_SCHEME` 选择。`dictionary-inspect` 报告版本、路径、计数和 SeWZC 署名，不进入按键查询路径。本机 `dictionary-update` 在格式版本、编译器版本、源路径/长度/最后修改时间和编译参数全部一致时复用现有 package。
