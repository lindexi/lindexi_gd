# MVVM 分层职责与聊天消息模板选择实践

## 背景

`CopilotChatMessage` 作为 Model 层类型，不应持有 `IBrush`、`HorizontalAlignment` 等 UI 细节。此类信息属于 View 层表现策略。

## MVVM 设计理念（简要）

MVVM 的核心目标是：

- **Model 关注数据与业务语义**，不感知具体 UI 技术。
- **View 关注呈现与交互**，负责布局、样式、模板、视觉状态。
- **ViewModel 关注状态编排与命令**，连接 View 与 Model，但不直接写具体控件样式。

### 1) Model 层应放什么

适合放在 Model：

- 领域数据（如 `Role`、`Content`、`TimeText`）
- 与业务语义相关的派生属性（如 `Author`）
- 业务状态标记（如 `IsPresetInfo`）

不应放在 Model：

- `Brush`、`Color`、`HorizontalAlignment`、`CornerRadius` 等视觉参数
- 依赖具体 UI 框架的展示策略
- 模板选择、控件行为细节

### 2) View 层应放什么

适合放在 View（XAML / 视图代码）：

- 布局与样式（颜色、间距、圆角、对齐）
- `DataTemplate` 定义
- 基于数据类型/状态选择模板（如模板选择器）

### 3) ViewModel 层应放什么

适合放在 ViewModel：

- 消息集合、发送状态、按钮文案等 UI 状态
- 事件与命令编排（发送消息、清空消息、打开设置）
- 服务调用与结果映射

不应放在 ViewModel：

- 具体控件样式、颜色、模板结构

## 本场景落地约定

- `CopilotChatMessage` 仅保留消息语义字段，不再包含气泡背景、前景色、对齐方式。
- 对于助手消息，思考链与最终正文属于不同语义，分别使用 `Reason` 与 `Content` 承载；需要整体复制或落盘时，再通过组合属性统一输出。
- 助手消息如果带有 `UsageDetails`，继续由 Model 暴露语义化统计字段；当界面需要更紧凑展示时，可由 Model 再提供一个聚合后的摘要文本，View 仅决定是否以右对齐细条显示，避免把多枚徽章布局塞回 Model。
- `CopilotSlideBar.axaml` 中定义用户/助手消息模板。
- 助手模板负责把“思考”和“正文”拆成两个可视区块，不把这类展示结构塞回 Model。
- 聊天气泡右键复制菜单直接基于 `Content` 与 `FullContent` 两个现有语义字段组合，不额外引入新的 ViewModel 包装层。
- 使用 `DataTemplateSelector`（基于 `Role`）决定 `ItemsControl` 每项采用的模板。

## 结果收益

- Model 去 UI 耦合，可复用于不同界面。
- 视图表现集中在 View，后续改皮肤/主题只改 XAML。
- 模板选择逻辑更清晰，避免通过 Model 携带布局参数。