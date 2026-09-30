# XiaoXiIme.ImeCore.Tests

小希输入法核心逻辑测试项目。职责：测试输入状态机、组合串、候选选择、分页、提交、取消和回退策略。

`CompiledDictionaryCandidateSelectionTests` 使用同一份规范词源分别编译全拼和小鹤 package，通过逐键输入验证精确候选优先、同层稳定排序、翻页、当前页数字选择和提交。`SetComposition` 覆盖整串替换全拼 `xiaoxiaimuyi`、不自动提交 `xx`，以及非法字符保持当前组合。`QueryConversionList` 可查询全拼 `xiaoxiaimuyi` 而不改变当前组合。`QueryReverseConversionList` 可把 `XiaoXiIme` 反查为 `xiao xi ai mu yi` 而不改变当前组合。`RegisterWord`/`UnregisterWord` 写入共享用户词典且不改变当前组合。
