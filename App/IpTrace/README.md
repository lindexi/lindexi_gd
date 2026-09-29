# IpTrace

基于 .NET 10 与 Avalonia 12 的轻量级网络诊断桌面工具，提供 DNS 解析、Ping 检测、TCP 探测和路由追踪。界面默认使用简体中文，采用 Fluent 风格，在支持的环境中使用 Mica 背景，否则回退为不透明背景。

当前单 EXE 发布方案面向 **Windows x64**，使用 Native AOT，目标机器无需安装 .NET 运行时。

## 功能

| 工具 | 功能 |
| --- | --- |
| DNS 解析 | 使用系统名称解析服务获取 IPv4 / IPv6 地址，去重后通过 ip.sb 查询归属地及运营商信息 |
| Ping 检测 | 执行 4 次 ICMP 请求，显示回复及统计信息，单次超时为 3 秒 |
| TCP 探测 | 对指定端口执行 4 次连接测试，显示连接耗时及成功、失败统计，单次超时为 3 秒 |
| 路由追踪 | 使用递增 TTL 的 ICMP 请求逐跳探测，最多 100 个跃点，单跳探测超时为 2 秒，并查询节点归属地 |

支持取消当前任务、查看执行耗时、清空结果及选择结果文本复制。路由追踪逐行显示结果；DNS、Ping 和 TCP 探测在任务结束后显示完整结果。

## 使用方法

1. 在顶部选择诊断工具。
2. 输入域名、IP 地址或完整网址，例如 `example.com`、`1.1.1.1` 或 `https://example.com/path`。
3. 使用 TCP 探测时设置端口，默认值为 `443`，有效范围为 `1–65535`。
4. 点击“开始诊断”，或在目标地址输入框中按 Enter。
5. 需要中止时点击“取消”；选择结果文本后可按 Ctrl+C 复制。

输入网址时只保留主机名，协议、端口、路径、查询参数和片段不会用于诊断。**TCP 端口需要单独设置**，不会自动沿用网址中的端口。IPv6 地址建议使用方括号形式输入，例如 `[2606:4700:4700::1111]`。

DNS 解析并非完整的 DNS 记录查询工具，目前不提供 MX、TXT、NS 等记录查询或自定义 DNS 服务器选项。

## 归属地查询与隐私

DNS 解析和路由追踪复用 `https://api.ip.sb/geoip/{IP}` 查询归属地，接口请求超时为 8 秒。程序识别出的本地或私有地址会显示“本地/私有网络”，不进行归属地请求。

- 查询会将相应 IP 地址发送给第三方服务 ip.sb；该服务也能看到请求来源的公网 IP。
- 归属地与运营商信息取决于第三方数据库，不代表设备的精确物理位置。
- 接口不可达、限流或超时可能导致归属地显示失败，也会增加诊断耗时。
- DNS 返回多个地址时，当前实现依次查询归属地，可随时取消任务。

请仅在获得授权的网络和目标上使用诊断功能。

## 开发环境

- .NET 10 SDK。
- 可使用支持 .NET 10 的 IDE，或直接使用 .NET CLI。
- Windows x64 Native AOT 发布还需要兼容的 Visual Studio / Build Tools C++ 工具链及 Windows SDK，安装时选择“使用 C++ 的桌面开发”工作负载。
- 首次还原需要能够访问所配置的 NuGet 源。

以下命令均在包含 `IpTrace.slnx` 的项目根目录执行。

### 构建与调试

```shell
dotnet restore IpTrace.slnx
dotnet build IpTrace.slnx -c Release
dotnet run --project Code/IpTrace/IpTrace.csproj
```

项目默认启用 `PublishAot`；普通构建或 `dotnet run` 不等同于生成 Native AOT 发布产物。

## Windows x64 单 EXE 发布

```shell
dotnet publish Code/IpTrace/IpTrace.csproj -c Release -r win-x64 -o artifacts/publish/win-x64-single
```

发布入口：

`artifacts/publish/win-x64-single/IpTrace.exe`

发布到一个空目录时，当前配置仅输出用于分发的 EXE。若复用旧发布目录，历史遗留的 DLL 或 PDB 不一定会被自动删除，建议发布前使用空目录。

### 原生依赖如何处理

程序将以下原生库嵌入程序集资源：

- `libHarfBuzzSharp.dll`
- `libSkiaSharp.dll`
- `av_libglesv2.dll`

在 Avalonia 初始化前，程序会将资源释放到临时目录，并通过绝对路径加载：

`%TEMP%\IpTrace\native\<DLL 内容的 SHA-256 哈希>\`

临时目录实际由 `Path.GetTempPath()` 决定。已有缓存通过 SHA-256 校验后复用；不存在或校验失败时重新释放。加载器使用互斥锁和临时文件替换，协调多实例启动。

**这是“单文件分发”，不是“不落盘运行”**。DLL 以嵌入资源保存，没有额外进行压缩；运行时仍需要允许写入临时目录并加载其中的原生库。退出程序不会自动清理缓存。关闭所有 IpTrace 实例后，可以删除对应缓存目录，下次启动会重新生成。

单 EXE 嵌入和发布过滤配置目前仅对 `win-x64` 启用，不代表其他平台或架构已完成适配。不要额外启用 `PublishSingleFile` 来替代本项目的 AOT 原生库嵌入流程。

## 项目结构

| 路径 | 说明 |
| --- | --- |
| `IpTrace.slnx` | 解决方案入口 |
| `Code/IpTrace/IpTrace.csproj` | 依赖、AOT、图标及原生资源发布配置 |
| `Code/IpTrace/MainWindow.axaml` | 窗口布局、样式与界面文案 |
| `Code/IpTrace/MainWindow.axaml.cs` | 工具切换、任务执行、取消及状态管理 |
| `Code/IpTrace/NetworkDiagnosticService.cs` | 网络诊断与归属地查询逻辑，包含 JSON 源生成上下文 |
| `Code/IpTrace/NativeLibraryBootstrap.cs` | 原生库释放、校验及预加载 |
| `Code/IpTrace/Program.cs` | 程序启动入口 |
| `Code/IpTrace/Assets/IpTrace.ico` | EXE 与窗口图标 |

## 常见问题

### Ping 或部分跃点超时，是否表示目标不可用？

不一定。目标主机、防火墙或中间路由器可能禁止 ICMP，或不回应 TTL 超时请求。可以结合 TCP 探测判断指定服务端口是否可达。TCP 连接成功仅表示该端口可建立连接，不代表应用层业务正常。

### 路由追踪为什么比较慢？

每个跃点都可能等待 ICMP 超时，并对返回地址进行归属地查询。最大 100 跃点不是固定总时长；网络超时和第三方接口延迟都会影响耗时。

### 单 EXE 启动时提示资源缺失或无法加载 DLL？

确认运行的是最新发布目录中的 EXE，而不是普通构建产物或历史版本。检查临时目录写入权限、安全软件及组织策略是否允许加载临时目录中的 DLL。资源缺失属于构建或发布问题，应保留完整异常信息排查，不应通过从不可信来源下载 DLL 解决。

### 修改图标后仍显示旧图标？

重新发布后确认打开的是新 EXE。Windows 可能缓存旧图标，可将发布文件放到新位置后检查。

## 验证范围

仓库目前没有独立的自动化测试项目。构建与发布成功不能替代实际运行验证；修改后建议在 Windows x64 环境中检查首次启动、缓存复用、四种诊断工具、取消操作及归属地请求失败的表现。
