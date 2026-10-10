# CodingChatRoom CodingAgent 思考强度设置方案

## 1. 目标

在聊天页模型选择器右侧提供思考强度选择，并将选择完整传递到 CodingAgent 使用的每轮模型请求。

## 2. 最终设计

### 2.1 固定选项

所有模型统一提供以下选项：

- 默认
- 低
- 中
- 高

界面与运行链路直接复用 `Microsoft.Extensions.AI.ReasoningEffort`，不定义重复枚举。

“默认”使用 `null` 表示，不设置 `ChatOptions.Reasoning`，由模型服务使用默认行为。

### 2.2 不使用模型能力配置

思考强度不属于 `ModelDefinition` 或 `LlmModelCapabilities`：

- 不增加模型支持强度列表；
- 不根据模型名称推断支持情况；
- 不因模型切换改变选项集合；
- 不扩展模型配置 JSON；
- 供应商或模型不接受所选值时，保留底层服务返回的明确错误。

### 2.3 不持久化

思考强度仅是聊天界面的当前运行参数：

- 不写入聊天消息历史；
- 不写入会话存储；
- 不写入 `CodingChatShellSettings`；
- 不写回模型配置；
- 应用重启后恢复为“默认”。

### 2.4 统一运行选项

使用轻量的 `CodingChatRunOptions` 结构统一承载自动压缩、`dotnet run` 和思考强度。它只是方法参数分组，不承担持久化或运行状态管理。

选项沿现有调用链直接传递：

```text
ChatViewModel.SelectedReasoningEffort
  → CodingChatApplication.SendMessageAsync
  → ICodingChatRunner.RunAsync
  → CodingAgent.RunAsync 扩展重载
  → ChatClientAgentOptions.ChatOptions
  → ChatOptions.Reasoning.Effort
  → IChatClient
```

运行期间禁用模型和思考强度选择器，因此当前运行不会受到界面后续修改影响。人类插话进入已有运行，不重新创建 Agent 或重新设置强度。

## 3. UI

布局：

```text
[工作路径] [路径输入] [应用] [模型] [思考强度]
```

要求：

- 思考强度选择器位于模型选择器右侧；
- 宽度为 120；
- 默认选中“默认”；
- 运行期间模型和思考强度选择器均禁用；
- 用户可见提示文本位于 `Styles/Strings.axaml`。

## 4. 请求行为

### 默认

不设置：

```text
ChatOptions.Reasoning
```

因此不会主动发送思考强度字段。

### 低、中、高

设置：

```text
ChatOptions.Reasoning = new ReasoningOptions
{
    Effort = selectedEffort
}
```

由 `Microsoft.Extensions.AI` 的具体聊天客户端适配器转换为供应商协议。

## 5. 特殊场景

### 工具调用

CodingAgent 的扩展重载在内部配置 `ChatClientAgentOptions.ChatOptions`，Agent 的首次请求以及工具调用后的后续请求复用该配置。适配上下文仅作为扩展实现的私有细节，不暴露独立服务类型。

### 人类插话

插话注入当前活动运行，不读取或传递新的思考强度。

### 循环迭代

循环入口接收 `CodingChatRunOptions`，并在循环中的每次主运行调用中传递该结构。

### 自动和手动压缩

思考强度只用于 CodingAgent 主运行。压缩请求不读取聊天页的思考强度。

## 6. 测试要求

- 界面存在思考强度选择器；
- 固定提供“默认、低、中、高”；
- 默认值不包含强度；
- 指定强度可从应用层传到 Runner；
- Runner 为非默认值设置 `ChatOptions.Reasoning.Effort`；
- 现有模型切换、工作路径、自动压缩、循环迭代、插话和停止功能保持正常。
