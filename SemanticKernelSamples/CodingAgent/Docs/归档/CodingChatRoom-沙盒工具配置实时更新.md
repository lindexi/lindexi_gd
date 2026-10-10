# CodingChatRoom 沙盒工具配置实时更新

## 生效边界

保存设置后不修改当前 Agent 运行。下一次新对话运行开始时，使用最新的 Windows 沙盒配置重新装配附加工具。

## 工具生命周期

- `CodingWorkspaceCache` 只缓存内置工作区工具和跨对话状态。
- `CodingAgent` 每次新运行时调用 `AdditionalToolSources` 创建本轮附加工具。
- `CodingRunWorkspaceContext` 合并缓存工具和本轮附加工具，形成不可变工具及展示注册表快照。
- 当前运行已经取得的快照不会因设置保存而变化。
- 工作区缓存不会因沙盒配置变化而释放，因此 LSP、文件读取状态和 CLI 最后日志不受影响。

## 沙盒配置

`WindowsSandboxToolSource` 保存一个不可变配置快照：

- 是否启用；
- WinRemoteShell 工具路径；
- Server 地址。

`UpdateConfiguration` 使用单次引用替换更新完整配置。下一轮调用 `CreateToolRegistrations` 时读取一次快照：

- 禁用时返回空工具集合；
- 启用时使用当前路径和地址创建本轮 `WindowsSandboxTools`。

## 设置保存链路

```text
SettingsViewModel.SaveAsync
  -> CodingChatSettingsService.SaveAsync
      -> 保存模型和 Shell 配置文件
      -> WindowsSandboxToolSource.UpdateConfiguration

下一次 CodingAgent.RunAsync
  -> 为当前工作区取得 CodingWorkspaceCache
  -> WindowsSandboxToolSource.CreateToolRegistrations
  -> CodingWorkspaceCache.CreateRunContext
```

`CodingChatStartup` 创建唯一的 `WindowsSandboxToolSource` 和 `CodingChatSettingsService`。同一个工具源实例同时交给设置服务和 `CodingAgent`。设置页面通过 `CodingChatRuntime.SettingsService` 复用该设置服务。

## 用户提示

保存成功后显示：

> 设置已保存。沙箱配置将在下一次对话运行时生效；模型和系统提示词将在下次启动时生效。
