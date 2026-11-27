# 本地化最终回归

## 修复

- Core 迁移后诊断语言由官方生成的 `SR.SetCurrent` 选择，不再依赖系统 UI Culture。容器诊断辅助方法显式选择英文，并在 finally 恢复原语言。
- LRC、MicroDVD、SubViewer、MPL2 精确诊断断言恢复调用前语言，不再硬编码恢复中文。移除 MP4 测试入口泄漏的中文选择；字幕套件入口也保存并恢复 Core 语言。
- LocalizationTest 保留 Demo 中英、格式参数及语言回退测试，增加 Core 中英实际解析异常、WPF Text 中英、TextBlock 的真实 BindingExpression 自动更新（中→英→中，未手动 UpdateTarget）、Generic.xaml 与合并样式资源加载。
- 不再使用旧 ResourceManager 字符串断言；检查三个业务程序集没有旧字符串 .resources（允许 WPF .g.resources）。
- 三个 csproj 的本地化包均为 PrivateAssets=all、ExcludeAssets=runtime。实际运行检查三个程序集 AssemblyRef、发布目录所有 DLL、deps.json 的 libraries/targets 均无 DotNetCampus 本地化运行库依赖。

## 实际发布与执行

均使用 Release、自包含发布、IncludeSelfTestMedia=true；未更改 TFM、SDK、包版本、平台定义或平台校验。

| Platform | RID | 发布目录 | Windows 沙盒参数 | 结果日志 |
| --- | --- | --- | --- | --- |
| x64 | win-x64 | .coding-agent/localization-final-x64 | --subtitle-test | .coding-agent/localization-final-x64-results/subtitle-test.log |
| x86 | win-x86 | .coding-agent/localization-final-x86 | --subtitle-test | .coding-agent/localization-final-x86-results/subtitle-test.log |

发布均显式传入 `-p:Platform=x64 -r win-x64` 或 `-p:Platform=x86 -r win-x86`，以及 `--self-contained true -p:IncludeSelfTestMedia=true`。

两份最终日志均 331 行，以 RESULT PASS 结束。MkvInvalidUtf8、其余 Matroska/MP4 边界及格式测试、本地化依赖审计、D3DImage/HWND 字幕显示、暂停、seek、延迟、关闭、重开、Dispose 验证全部通过。

额外执行播放器解决方案默认 x64 Release 构建成功，0 错误、0 警告（增量构建）。曾尝试在解决方案级指定 RID，被 SDK 以 NETSDK1134 拒绝；随后按解决方案不传 RID 构建成功。双架构验证以各自显式 Platform/RID 的项目发布及真实沙盒运行结果为准，未额外执行 x86 解决方案级构建。

## 清理及边界

- 旧 resx 内容已清空，仍由 csproj 排除；空文件仅为无法删除情况下的残留，不是资源源文件。
- LocalizationProbe 的代码、配置及资源内容已清空，两个项目保留为无包依赖的空项目；旧验证说明明确停用，不作为通过证据。
- WPF 模板绑定已实测支持语言通知。Demo 菜单创建时直接赋值的 Header/ToolTip 是快照，不能承诺运行中所有文本实时切换；需重建对应菜单。Lang、SR、Text 语言分别选择。
- x86 发布可见 CS1591，来源为 DotNetCampus.SourceLocalizations 0.1.1-alpha.9 生成的公开成员缺少 XML 文档。保留警告，未全局 NoWarn、关闭文档生成或编辑生成文件。增量构建无警告不表示生成器问题已修复。
- 本轮证明上述完整字幕/本地化套件通过，不等同于全部播放器测试、硬件解码、音画同步或所有 Demo 交互均经过验证。
