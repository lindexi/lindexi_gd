# PrismCodingAgent

编程 Agent 桌面产品，当前使用 Avalonia；未来其他 UI 实现保持独立项目，不提前创建占位层。

## 结构与入口

- `Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj`：应用，输出程序集 `PrismCodingAgent`。
- `Tests/PrismCodingAgent.AvaloniaShell.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj`：测试，程序集 `PrismCodingAgent.AvaloniaShell.Tests`。
- `PrismCodingAgent.slnx`：独立解决方案。
- `Docs`：产品文档，暂保留原内部分类和历史文件名。

从 SemanticKernelSamples 根目录构建：`dotnet build PrismCodingAgent/PrismCodingAgent.slnx`。

常规测试：`dotnet test PrismCodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj --filter "TestCategory!=LiveApi"`。

## 迁移状态

目录和项目更名已执行；解决方案、项目引用、程序集、RootNamespace、友元程序集、资源 URI、manifest 和调试配置已更新。

IDE 已完成显式命名空间迁移为 `PrismCodingAgent.AvaloniaShell` 及 `.Tests`；七个 XAML 的旧视图命名空间遗漏及测试辅助类缺少 AgentLib.Coding 引用已修正。独立解决方案构建及全部非 LiveApi 测试通过。

## 兼容边界

AgentLib 仍引用 SemanticKernelSamples 根下原项目，不复制源码、不改为 NuGet。用户数据路径、配置及会话格式保持不变。旧 CodingAgent 目录可能保留缓存和空目录，不纳入新产品、不递归删除。

生产依赖不包含已撤回的 WindowsSandboxProbe。主应用输出不带 UI 框架后缀。

## 验收边界

当前产品命名空间迁移后，常规应用测试 238 项通过（0 失败）。真实服务测试不在本轮执行；构建仍存在既有分析器警告，不宣称零警告。原生 Compact 续聊仍未通过，调查见 Docs/CodingChatRoom-Responses API/14-原生压缩续聊调查与交接.md。
