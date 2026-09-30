# XiaoXiIme 客制化教程

本文说明 XiaoXiIme 中与名称、任务栏显示和文件属性有关的客制化入口。重点结论是：Windows 任务栏中显示的“简体”通常是当前输入语言 `中文（简体，中国）` 的语言指示，不是 XiaoXiIme 自己注册的产品名称。

## 先区分 Windows 显示的几类文本

Windows 输入法界面会同时使用多种来源不同的文本：

| 显示位置 | 当前值或示例 | 来源 | 是否适合客制化 |
| --- | --- | --- | --- |
| 任务栏输入指示 | `简体` | Windows 根据输入语言 `zh-CN` / 语言 ID `0x0804` 生成 | 不能通过修改输入法产品名直接替换 |
| `Win+Space` 输入法切换列表 | `XiaoXi IME` | 安装时传给 `ImmInstallIMEW` 的 `Layout Text` | 可以 |
| TSF 语言配置文件名称 | `XiaoXi IME (TSF)` | `TsfRegistration.DisplayName` | 可以，但当前 TSF 注册流程仍需完善 |
| 文件属性中的说明和产品名 | `XiaoXi Input Method`、`XiaoXi IME` | `XiaoXiIme.rc` 的 VERSIONINFO | 可以 |
| IME 文件名 | `XiaoXiIme.ime` | 发布和安装流程 | 可以，但会影响安装、诊断、卸载和测试，不建议只改一处 |

因此，只把安装名称从 `XiaoXi IME` 改成其他文字，通常会改变设置页面或 `Win+Space` 列表中的输入法名称，但任务栏仍可能显示“简体”。这是 Windows 按语言分组后的正常行为。

## 客制化传统 IME 的显示名称

传统 IMM 输入法通过下面的调用注册：

```text
ImmInstallIMEW(imeFileName, layoutText)
```

当前安装入口位于 `src/XiaoXiIme.Cli/Program.cs`，其中 `XiaoXi IME` 会作为 `layoutText` 传给 `WindowsImeInstaller.InstallPair`。安装器将 x64/x86 模块部署到对应系统目录，并最终在 `src/XiaoXiIme.Cli/WindowsImeInstaller.cs` 中调用 `ImmInstallIMEW` 注册 x64 布局。注册成功后，该值会成为键盘布局注册项的 `Layout Text`，通常用于 Windows 设置和输入法切换列表。

若要把产品名改为“示例输入法”，至少需要同步检查以下位置：

1. `src/XiaoXiIme.Cli/Program.cs` 中安装时传入的显示名称；
2. `src/XiaoXiIme.Cli/IntegrationTestRunner.cs` 中安装、卸载和清理使用的显示名称；
3. `src/XiaoXiIme.Cli/WindowsImeInstaller.cs` 中识别本产品布局的 `IsXiaoXiIme` 逻辑；
4. `src/XiaoXiIme.Cli/ImeInstallationDiagnostics.cs` 中诊断布局的匹配逻辑；
5. `tests/XiaoXiIme.Cli.Tests/ImeInstallationDiagnosticsTests.cs` 中相应断言。

不要只修改安装入口。当前卸载器使用显示名称和 IME 文件名识别应删除的布局；如果安装名已经变化而识别逻辑仍保留旧值，重新安装或清理旧版本时可能遗留注册项。

更稳妥的长期做法是把产品显示名称集中为一个共享常量，并让安装、卸载、诊断和测试共同引用。若需要兼容已经发布的旧名称，卸载识别逻辑应同时接受新旧名称，直到旧版本迁移完成。

## 客制化文件属性

传统 IME 的 Windows 版本资源位于 `src/XiaoXiIme.ImeModule/XiaoXiIme.rc`。可以修改以下字段：

- `CompanyName`：公司或组织名称；
- `FileDescription`：文件说明；
- `InternalName`：内部名称；
- `ProductName`：产品名称；
- `FileVersion` 和 `ProductVersion`：文件版本和产品版本；
- `OriginalFilename`：原始文件名。

这些字段主要显示在文件资源管理器的“属性 > 详细信息”中。修改 `ProductName` 或 `FileDescription` 不会修改任务栏中的“简体”，也不会自动修改 `ImmInstallIMEW` 注册的 `Layout Text`。

如果修改 `OriginalFilename` 或实际 `.ime` 文件名，需要同时更新发布重命名、安装诊断、卸载清理、集成负载清单和测试。文件名属于安装标识的一部分，不应当只作为视觉文案处理。

## 客制化 TSF 配置文件名称

TSF 相关标识定义在 `src/XiaoXiIme.TsfModule/TsfRegistration.cs`：

- `ClassId`：TSF COM 类标识；
- `ProfileId`：语言配置文件标识；
- `SimplifiedChineseLanguageId`：简体中文语言 ID `0x0804`；
- `DisplayName`：TSF 配置文件显示名称。

修改 `DisplayName` 可以改变未来 TSF 语言配置文件注册时使用的名称，但当前项目的 TSF 注册流程仍在建设中，不能把该常量的变化等同于系统中已注册名称已经更新。完成 TSF 注册后，还需要卸载或注销旧配置文件，再注册新配置文件并重新登录验证。

`ClassId` 和 `ProfileId` 是持久身份，不是普通显示文本。品牌改名通常不需要更换 GUID；随意更换会让 Windows 把它识别为另一套组件，并可能留下旧注册信息。

## 为什么不能直接把任务栏“简体”改成品牌名

当前传统 IME 和 TSF 配置都归属简体中文：

- 传统 IME 注册后生成的键盘布局 ID 以简体中文语言 ID 为基础；
- TSF 使用 `SimplifiedChineseLanguageId = 0x0804`；
- Windows 任务栏会优先显示当前输入语言的短标签，而不是每个输入法的完整产品名。

项目没有一个可把“简体”替换为任意品牌文字的普通配置项。修改 `Layout Text`、VERSIONINFO 或 TSF `DisplayName` 都不能保证改变该标签。

不建议为了改变这两个字而把输入法挂到其他语言或修改系统语言资源。这会导致语言分组、候选行为、键盘布局、用户词典或系统兼容性出现语义错误，而且不属于输入法应用应控制的稳定接口。

如果希望用户在任务栏附近识别产品，建议采用以下方式：

1. 确保 `Win+Space` 和 Windows 设置中显示清晰的产品名称；
2. 在 TSF 实现完善后，为语言配置文件注册产品名称和图标；
3. 实现 TSF Language Bar Item，用图标或状态按钮表达中英文模式、全半角等输入法内部状态；
4. 使用候选窗口和设置界面的品牌视觉，但不要覆盖 Windows 自己的语言指示。

当前仓库尚未实现 `ITfLangBarItem` 一类的语言栏按钮，因此“自定义任务栏图标/状态按钮”属于后续功能开发，而不是修改一处资源即可完成的客制化。

## 修改后的安装验证

输入法注册信息会被 Windows 缓存。验证名称变更时，应在可还原 VM 或专用测试机中执行，不要在日常开发机上反复安装实验版本。

建议按以下顺序验证：

1. 卸载旧版本，并确认旧键盘布局注册项已清理；
2. 重新构建并发布 `.ime` 与 CLI；
3. 使用管理员权限安装新版本；
4. 注销并重新登录；必要时重启 Windows；
5. 在“设置 > 时间和语言 > 语言和区域”中检查输入法名称；
6. 使用 `Win+Space` 检查切换列表中的名称；
7. 检查任务栏是否仍显示“简体”，并按本文前述说明区分语言标签与产品名；
8. 打开 `.ime` 文件属性，检查 VERSIONINFO 是否符合预期；
9. 执行 CLI 和集成测试，确认卸载、诊断与清理仍能识别改名后的布局。

可用管理员 PowerShell 查看实际注册结果：

```powershell
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Control\Keyboard Layouts' |
	ForEach-Object {
		$item = Get-ItemProperty $_.PSPath
		[pscustomobject]@{
			LayoutId = $_.PSChildName
			LayoutText = $item.'Layout Text'
			ImeFile = $item.'Ime File'
		}
	} |
	Where-Object { $_.ImeFile -like '*XiaoXi*' -or $_.LayoutText -like '*XiaoXi*' }
```

重点检查 `Layout Text` 是否为新的产品名称、`Ime File` 是否指向预期文件。注册表只用于诊断；正式修改应继续通过安装和卸载流程完成，不建议手工编辑注册表作为发布方案。

## 推荐的客制化检查清单

- 产品名是否同时更新了传统 IME 安装、卸载、诊断与测试；
- TSF 名称是否与传统 IME 名称保持一致；
- VERSIONINFO 的公司名、说明、产品名和版本是否一致；
- 是否保留了对旧产品名和旧文件名的卸载兼容；
- 是否避免把 Windows 的“简体”语言标签误当成产品名称；
- 是否在 VM 中完成卸载、重装、注销登录和 `Win+Space` 验证；
- 是否执行相关自动化测试和解决方案构建。
