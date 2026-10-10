# CodingAgent

CodingAgent 桌面产品目录。当前 UI 实现为 Avalonia，未来其他 UI 实现应放在 Code 下的独立项目，不预先建立占位项目。

## 结构

- Code/CodingAgent.Avalonia：当前应用，输出程序集 CodingAgent。
- Tests/CodingAgent.Avalonia.Tests：应用测试，输出程序集 CodingAgent.Avalonia.Tests。
- Docs：产品文档，内部分类暂时保留。
- CodingAgent.slnx：独立构建入口。

## 依赖

继续引用仓库根目录的 AgentLib/AgentLib.Coding，不复制源码、不改为 NuGet。以 SemanticKernelSamples 为根，产品目录适合后续 git subtree 分离；单独检出仍需要约定的外部源码依赖布局。

## 构建与测试

从 SemanticKernelSamples 根目录构建：`dotnet build CodingAgent/CodingAgent.slnx`。

当前项目入口：

- Code/CodingAgent.Avalonia/CodingAgent.Avalonia.csproj
- Tests/CodingAgent.Avalonia.Tests/CodingAgent.Avalonia.Tests.csproj

从仓库根目录运行测试：`dotnet test CodingAgent/Tests/CodingAgent.Avalonia.Tests/CodingAgent.Avalonia.Tests.csproj`。真实服务测试的执行约定及已知失败边界见产品文档。

目录、项目文件、输出程序集与资源 URI 已迁移；源码命名空间及调试配置名称暂时保留旧名称。现有用户数据目录、配置和会话格式保持不变，不因产品目录迁移而自动改变。

构建产物位于产品的 artifacts 目录，由 Directory.Build.props 控制。没有迁入旧构建目标中的示例输出消息，也没有迁入已撤回的沙箱探针。

## 验证边界

独立解决方案构建、任务协调/ViewModel/Runtime 定向测试，以及资源/结构/Responses 开关测试已通过。未执行全量回归或真实服务测试；原生 Compact 续聊仍未验收，详见 Docs/CodingChatRoom-Responses API/14-原生压缩续聊调查与交接.md。
