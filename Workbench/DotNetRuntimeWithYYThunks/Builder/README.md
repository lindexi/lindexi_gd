# Builder 命令行使用说明

Builder 用于编排外部 dotnet/runtime 源码构建，并将 YY-Thunks 接入适用的 Windows 原生 DLL/EXE 链接步骤。支持 x86、x64，当前仅接受 Win7 兼容目标；构建工具本身运行在现代 Windows 上。

**构建完成不代表 Win7 兼容验证通过。** 工具不会自动修复托管 API、动态 API 加载或预编译第三方 DLL 的不兼容问题。

## 目录与执行约定

本文所在目录是控制台项目目录，包含 `Builder.csproj`；其上一级是包含 `Builder.sln` 的开源项目根目录。**以下所有命令均从本文所在目录执行**。外部输入使用占位符，不要求源码仓库位于特定本机目录。

`<runtime-root>` 替换为 dotnet/runtime 源码目录，`<yy-root>` 为可选 YY-Thunks 源码目录，`<yy-obj>` 为可选原生对象文件路径。执行命令前应替换占位符，不要保留尖括号。

Builder 运行时会从可执行文件目录向上查找 `Builder.sln`；不要只复制 Builder 可执行文件到不含该根标记的独立目录运行。

## 环境要求

- .NET 10 SDK，用于构建和运行 Builder；项目根 `global.json` 决定 SDK 选择。
- 现代 Windows、Windows PowerShell，以及 runtime 官方要求的现有 Visual Studio C++ 工具、Windows SDK、CMake、Ninja 和 Python 等环境。
- 首次还原需要访问 NuGet 源；runtime 官方 bootstrap 也可能下载仓库本地 SDK 和构建依赖。

Builder 不安装系统组件，不要求预先打开 Developer Prompt；官方脚本负责初始化已有 VC/SDK 环境。`doctor` 只做静态输入检查，不代替实际编译验证全部环境依赖。

## 快速开始：构建完整运行时

先编译工具并自动还原 YY-Thunks：

```text
dotnet build Builder.csproj -c Release
```

检查输入：

```text
dotnet run --project Builder.csproj -c Release -- doctor --runtime "<runtime-root>" --target-os win7 --arch x64
```

分别构建、组装带 YY-Thunks 的完整运行时：

```text
dotnet run --project Builder.csproj -c Release -- build --runtime "<runtime-root>" --target-os win7 --arch x86 --full-runtime --timeout 7200
dotnet run --project Builder.csproj -c Release -- build --runtime "<runtime-root>" --target-os win7 --arch x64 --full-runtime --timeout 7200
```

两次命令应顺序执行，**不要并发操作同一个 runtime checkout**。`-c Release` 控制 Builder 自身配置；`--configuration Release` 控制目标 runtime，后者默认已是 Release。

**需要可部署的运行时目录时必须添加 `--full-runtime`。** 不加该参数只构建和收集原生组件，不包含完整托管运行时，也不是 SDK。

## 命令

基本语法：

```text
dotnet run --project Builder.csproj -c Release -- <command> --target-os win7 --arch <x86或x64> [options]
```

| 命令 | 行为 |
|---|---|
| `doctor` | 检查 Windows、官方构建入口、PowerShell 和匹配架构的 YY OBJ 等静态输入；不启动 runtime 构建。 |
| `plan` | 生成、输出构建计划，不执行构建；发现输入阻塞时返回非零。 |
| `build` | 实际接入 YY OBJ，执行官方构建；添加 `--full-runtime` 后组装完整运行时。 |
| `baseline` | 不添加 YY OBJ，构建用于对照的基线；添加 `--full-runtime` 后组装完整基线运行时。 |
| `inspect` | 检查 `--file` 指定文件的 PE 头、架构等信息；不检查完整导入兼容性，不执行该文件。 |

帮助使用独立参数形式：

```text
dotnet run --project Builder.csproj -- --help
dotnet run --project Builder.csproj -- -h
```

除独立帮助请求外，所有命令（包括 `inspect`）均须指定 `--target-os` 和 `--arch`。命令、参数及枚举值区分大小写；不支持任意官方构建参数透传。

## 参数

| 参数 | 默认值／允许值 | 说明 |
|---|---|---|
| `--target-os` | 必填，当前仅 `win7` | `xp`、`vista` 会被明确拒绝。声明目标意图，不是兼容性认证。 |
| `--arch` | 必填，`x86` 或 `x64` | 目标运行时及 OBJ 架构；构建另一架构需单独运行。 |
| `--runtime` | 未指定时尝试项目根的同级 `runtime` 目录 | 外部 runtime checkout。可移植用法建议始终显式传入。 |
| `--configuration` | `Release`；可选 `Debug`、`Checked` | 目标构建配置。普通分子集模式中 Checked 的独立 libs.native 步骤映射为 Debug；完整模式组合构建由官方入口处理配置约束。 |
| `--full-runtime` | 关闭 | 构建 CoreCLR、CoreLib、类库和 Host，并通过官方项目组装 runtime-only 布局。 |
| `--timeout` | `3600` | 秒，范围 `1`～`86400`；实际构建命令序列共享总截止时间，各子进程也受超时约束。 |
| `--dry-run` | 关闭 | 输出计划而不执行构建，可配合 `build --full-runtime`；仍检查静态输入。不是启动或兼容性验证。 |
| `--configure-only` | 关闭 | 将配置阶段请求传给官方脚本，并跳过完整组装。Windows libs.native 不完整支持此请求，仍可能编译、安装；不是绝对不编译模式。 |
| `--obj` | 自动选择 NuGet OBJ | 显式指定架构匹配的 Win7 OBJ；优先于 `--yy`，进行 COFF 结构及架构检查。 |
| `--yy` | 不指定 | 可选旧式对象目录来源，使用该目录下 `objs/<arch>/YY_Thunks_for_Win7.obj`；不会自动编译 YY 源码。 |
| `--file` | 无 | `inspect` 必填，要检查的 PE 文件。 |

参数使用 `--name value` 形式，不支持 `--name=value`。不要重复参数。相对输入路径相对于当前工作目录解析，含空格路径须加引号。

当前路径校验允许空格、中文、单引号，但拒绝控制字符以及双引号、分号、百分号、感叹号、美元符、反引号、方括号和 `& | < > ^ ( )`。这是现有 CLI 的限制，选用路径时需避开这些字符。

## YY-Thunks 自动获取及覆盖

普通 `PackageReference` 负责 NuGet 还原，升级只需修改 `Builder.csproj` 中该引用的 `Version`。不使用包锁文件。包只作为 OBJ 获取来源，不将包内 native 链接 targets 应用到 Builder 自身的托管项目。

构建会将包内两架构 Win7 OBJ 复制到 Builder 输出的 `yy-thunks/<arch>/`，并生成 `yy-thunks/package.json` 记录实际包版本；无需手工解包或配置本机 NuGet 缓存路径。

对象选择优先级：

1. 显式 `--obj`。
2. 显式 `--yy` 下的架构对应 OBJ。
3. Builder 输出目录中的 NuGet OBJ。

示例：

```text
dotnet run --project Builder.csproj -- build --runtime "<runtime-root>" --target-os win7 --arch x64 --obj "<yy-obj>" --full-runtime
```

`baseline` 不使用 YY OBJ。普通包对象输入检查不等于对其全部回退语义的认证，也不要求消费端重新运行 FixObj。

## 常用操作

预演完整构建：

```text
dotnet run --project Builder.csproj -- build --runtime "<runtime-root>" --target-os win7 --arch x64 --full-runtime --dry-run
```

构建不带 YY-Thunks 的完整基线：

```text
dotnet run --project Builder.csproj -c Release -- baseline --runtime "<runtime-root>" --target-os win7 --arch x64 --full-runtime --timeout 7200
```

只检查某个原生文件：

```text
dotnet run --project Builder.csproj -- inspect --target-os win7 --arch x64 --file "<待检查文件路径>"
```

## 构建内容与输出

普通模式顺序编排 `clr.native`、`libs.native`、`host.native`。完整模式使用 `clr.runtime+clr.corelib+libs.native+libs.sfx+host.native+host.tools`，然后调用官方运行时文件收集与目录发布目标。当前不做 ReadyToRun 发布优化，不生成 SDK、安装器或 NuGet 发行包。

每次运行均在项目根下创建独立目录：

```text
artifacts/<命令>-win7-<架构>-<UTC时间>-<GUID>/
```

其中可能包含：

| 路径 | 内容 |
|---|---|
| `plan.json` | 命令、解码的官方脚本和静态输入阻塞项。 |
| `manifest.json` | 阶段状态、退出码、进程结果、输入及输出哈希、对象来源和实际包版本。 |
| `0.log`、`1.log` 等 | 各实际构建或组装步骤的日志。 |
| `YYThunks.cmake`、`Toolchain.cmake` | 本次生成的原生构建注入配置。 |
| `runtime/` | 完整模式构建并组装成功后的运行时目录。 |
| `bin/` | 普通模式收集的原生组件，不是完整运行时。 |

成功的 `runtime/` 布局为 `dotnet.exe`、`host/fxr/<version>/`、`shared/Microsoft.NETCore.App/<version>/` 以及许可证文件，不包含 `sdk`、`packs`、`templates`。

CLI 不会自动把每次输出发布到固定的 `runtime-yythunks-win-*` 路径；独立验收、审计和稳定候选发布流程见后文链接。实际中间文件和官方日志仍可能写入外部 runtime checkout 的 `artifacts`。切换 baseline/build 时工具清理所选架构配置的 CMake 缓存，不清空全部中间输出，因此不是 clean 全量构建。

日志和清单可能记录本机环境路径，不应未经脱敏直接作为开源文档提交。

## 退出码与故障排查

| 退出码 | 含义 |
|---|---|
| `0` | 对应命令阶段完成；不代表 Win7 实际运行通过。 |
| `2` | 参数无效、目标不支持、静态检查阻塞、文件或进程启动等错误。 |
| `3` | PE 头部检查不符合目标要求。 |
| `124` | 超时。 |
| `130` | 用户取消，例如 Ctrl+C。 |
| 其他非零 | 官方构建子进程退出码原样传播；可能与上述值重合，以日志为准。 |

失败后先查看最新运行目录的 `manifest.json` 和对应步骤日志。构建失败立即停止后续步骤；超时或取消会终止子进程树。不要只根据 `runtime/` 目录存在判断成功，失败过程可能留下部分文件。

OBJ 缺失时先重新构建 Builder 触发还原和复制；工具链问题按官方日志处理已有环境。不要通过关闭文件版本验证、忽略未解析符号或复制其他版本运行时文件来伪造成功。

## 当前兼容范围

YY OBJ 主要覆盖自编译的适用原生最终链接目标，不对静态库统一注入；无入口且无导入、无可执行节的纯资源 DLL 可豁免。NuGet 等渠道提供的预编译第三方 DLL 不会因调用方加上 OBJ 自动被修复。

当前候选的第三方未覆盖项包括 `msquic.dll` 和 `Microsoft.DiaSymReader.Native.*.dll`；其中 MsQuic 存在已识别的 Win7 API 风险。现代 Windows 启动回归通过不能替代 Win7 SP1 实测。

完整组装成功、PE 检查通过、原生链接证据、导入审计、现代系统回归、Win7 实测是不同阶段。此 CLI 的 `inspect` 不提供普通／延迟导入白名单或依赖闭包认证。运行 manifest 中的 `ImportValidation=NotImplemented` 指本 CLI；独立审计结果另行保存。

## 更多文档

- [整体使用与交付说明](../Docs/README.md)
- [YY-Thunks 构建与验证](../Docs/YY-Thunks构建与验证.md)
- [运行时完整性审计](../Docs/运行时完整性审计.md)

这些链接均位于包含 `Builder.sln` 的项目根目录内，不引用项目根之外的本机文件。
