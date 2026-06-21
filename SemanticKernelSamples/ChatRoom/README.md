# ChatRoom 桌面聊天室

多个 AI 角色在同一个聊天室中自动对话，人类可以作为其中一个角色随时插话。每个角色拥有独立的人设、记忆和模型。参考 AutoGen / CrewAI 的 GroupChat 模式。

## 技术栈

- **桌面框架**：Avalonia UI 12
- **运行时**：.NET 10
- **核心库**：[AgentLib.ChatRoom](../AgentLib/AgentLib.ChatRoom/) — 提供聊天室核心能力（角色管理、发言编排、持久化）
- **测试框架**：MSTest

## 项目结构

```
ChatRoom/
├── Code/
│   ├── ChatRoom.AvaloniaShell/      # Avalonia 桌面应用（入口、ViewModel、Views）
│   └── ChatRoom.Shell.Tests/        # 桌面应用层单元测试
├── Docs/
│   └── 需求文档.md                   # 功能需求清单
└── ../AgentLib/
    ├── AgentLib.ChatRoom/           # 聊天室核心库（ChatRoomManager / ChatRoomRole / ChatRoomService）
    └── AgentLib.ChatRoom.Tests/     # 核心库单元测试
```

## 快速上手

### 1. 编译运行

在 Visual Studio 中打开解决方案，将 `ChatRoom.AvaloniaShell` 设为启动项目，直接运行。

### 1.1 使用 dotnet run 调试

除了在 Visual Studio 中运行，也可以通过命令行 `dotnet run` 启动应用，配合控制台输出进行调试。

这种方式适合以下场景：
- 在没有 Visual Studio 的环境（如 Linux / macOS / WSL）中运行
- 需要观察程序的标准输出和 trace 日志（`LogToTrace` 已在 `Program.cs` 中启用）
- 需要通过环境变量控制运行时行为
- 需要快速验证程序是否能正常启动和退出

基本用法：

```bash
# 进入 AvaloniaShell 项目目录
cd Code/ChatRoom.AvaloniaShell

# 直接运行（默认使用 Debug 配置）
dotnet run

# 指定 Release 配置
dotnet run -c Release

# 传递运行时参数
dotnet run -- --debug
```

**控制台输出调试**：`Program.cs` 中调用了 `.LogToTrace()`，Avalonia 的诊断信息会输出到标准输出。通过 `dotnet run` 启动时，所有 trace 日志直接在终端可见，便于排查启动失败、窗口创建异常等问题。

**程序退出控制**：在终端中按 `Ctrl+C` 可以正常终止程序，Avalonia 桌面生命周期会收到取消信号并执行清理。如果程序卡死，可以按 `Ctrl+\`（发送 SIGQUIT）强制终止。

**环境变量**：

```bash
# 指定持久化目录（覆盖默认路径）
CHATROOM_PERSISTENCE_PATH=./data dotnet run

# 开启 Avalonia 开发者工具（Debug 配置下自动开启）
DOTNET_ENVIRONMENT=Development dotnet run
```

**注意事项**：
- `dotnet run` 会在当前终端前台运行，关闭终端会终止程序
- 如果需要后台运行，可以使用 `dotnet run &`（Linux/macOS）或在 Windows 上使用 `Start-Process`
- 首次运行需要 `dotnet restore` 和 `dotnet build`，后续运行会增量编译

### 2. 首次配置

首次启动后，进入设置页面配置模型提供商：

- 填写提供商名称、API 地址、密钥
- 添加至少一个模型
- 设置全局首选模型

配置保存后立即生效，无需重启。

### 3. 基本使用

1. **创建会话**：点击左侧"新建会话"，系统自动创建一个"助手"角色
2. **添加角色**：在右侧角色面板添加 AI 角色，配置角色名、系统提示词、模型
3. **开始对话**：点击发送按钮启动自动循环，AI 角色按顺序自动发言
4. **人类插话**：在输入框输入消息随时插话，AI 角色会自动回复
5. **@提及**：在消息中使用 `@角色名` 可以指定某个角色接下来回复

### 4. 会话管理

- 会话自动持久化到本地磁盘
- 可以从左侧历史列表打开、删除历史会话

## 更多信息

- [需求文档](Docs/需求文档.md) — 完整的功能需求清单
- [AgentLib.ChatRoom README](../AgentLib/AgentLib.ChatRoom/README.md) — 核心库 API 参考和使用示例
