# a43 UI apply 调查归档

## a45 最终实绩

关闭输入暂停/主窗关闭候选不重开已补回归，专项9/9；默认Debug Core432/Dict209/Integration215/IPC27/Module148均PASS、零失败零跳过；Release全solution 0警告0错误。新白名单共享runtime发布后冻结并实际调用full，600s/只evidence；WinRemoteShell HttpClient100s超时(-1073740791)，未回拉目录。无新report/phase/截图actual identity normal-exit或精确规模；不判业务失败、不造phase根因、不重复推相同负载，最终BLOCKED/NOT ACCEPTED。七方案source外部阻碍。

## a44 接续事实回读

a44 minimal2 报告已回读：分页10次、checkbox20次均PASS，normal-exit-6612 PASS。a44 full 报告与失败日志已回读：execution FAIL、normal-exit-2920 PASS；Capture→Click 的 First 无匹配（DebugFuzzyValidation.cs:89/292/364）。full 激活清空组合 runner 修正版据交接已发布；后续HTTP100s timeout不能归为业务失败，不能当作13项通过。当前生命周期事务锁/关闭恢复须补关闭设置输入暂停及主窗关闭候选不重开测试。本轮改用新白名单共享runtime，不复传旧目录，不复用.stop；完整13及最终测试/Release/freeze/截图身份退出待新实测。七方案source缺失仍外部阻碍。


## 修复前证据（历史回读）

- a43 minimal-final 报告已回读：10 次分页往返 + 20 次 checkbox 点击均 PASS；`normal-exit-1652` PASS。即 minimal30 PASS，另有正常退出 PASS。
- a43 full 失败日志已回读：`WaitApplied(5)` 调用 `ReadBounds`，快照超过 2 秒，抛出 `Diagnostic snapshot is stale`；调用点完整验证第 363 行。完整套件 FAIL，不能算作业务断言 PASS。
- full actual UI PID 9332；minimal actual UI PID 1652。当时两轮均保留了进程实测、身份、输入和快照记录；原件未收录在本仓库。
- full 原始包 manifest SHA256：C354BB7BC91FB97F40B422BED323CAF74A75C9FE589C2C39D9137688A8475AA5；UI dll SHA256：934A52E55A3DB583E8647F737B1AA09A529A8FCE52B2D361E6A3234D0104586B。
- a43 full 当时未生成完整报告，不能据此认定正常清理成功。normalclose timeout 仍须重现记录，禁止把强制终止当正常退出。

## 静态调用链（不是动态耗时结论）

真实应用按钮 -> DictionaryDebugSettings.SaveAsync：包 Load 在 Task.Run；恢复 lexicon / PrepareDictionaryChange / PrepareFuzzyRulesChange 在 await 返回后的 UI continuation 同步执行；随后才调用 store.SaveAsync 并 PublishSession。
PrepareFuzzyRulesChange 同步构建全词库 DecodingIndex；QueryLegacyPhonetic 再次取得 lexicon。CompiledImeDictionary 首次 GetLexicon 同步恢复全量数据。PrepareDictionaryChange 还可能重复刷新和按旧规则准备。

下一步：先加入 UTC/线程/阶段耗时诊断并真实点击复现，确认 CPU 阶段后再修复。不得放宽 2 秒 stale 门限，不截断候选，不替代真实 UI 点击。
