# CodingChatRoom Fluent 多任务改造交接

## 1. 权威范围

项目目录：`ChatRoom/Code/CodingChatRoom.AvaloniaShell`。
测试项目：`ChatRoom/Code/CodingChatRoom.AvaloniaShell.Tests`。
工作区：`C:\lindexi\Code\lindexi_gd\SemanticKernelLib\SemanticKernelSamples`。

基础技术：.NET 10、Avalonia 12.1.0、编译绑定、MSTest、Avalonia.Headless。不升级依赖，不修改 TFM。

权威文档：

- [多工作任务并行改造需求](CodingChatRoom-多工作任务并行改造需求.md)
- [多工作任务并行改造设计](CodingChatRoom-多工作任务并行改造设计.md)

其他需求文档和待办不自动纳入多任务功能。

未列入权威需求的旧建议和旧待办均永久不做，不存在“本次不做、以后再做”的含义。禁止为永久非目标预留抽象、字段、接口或兼容逻辑。

## 2. 必须实现

- 使用 `WorkTasks.json` 持久化任务元数据。
- 应用启动时恢复任务。
- 每个任务保存全部指定信息：ID、名称、工作目录、采用的模型和思考强度。
- 恢复历史会话时，将历史会话工作路径同步到目标任务。
- 不自动恢复执行。
- 不保存密钥。
- 不考虑多个进程同时写入。
- 禁止过度防御、过度设计和补丁式实现。

## 3. 只核查，正确则不改

- 每个任务拥有完全独立的 Runtime 和运行状态。
- 停止只影响对应任务，并覆盖循环等待、压缩和最终收尾。
- 任务运行期间删除安全。
- 任务运行期间退出窗口安全。
- Runtime 不泄漏。
- 删除任务后事件订阅完整解除。
- 删除失败保留原任务。
- 异步异常有可读提示。
- 全局设置传播行为有测试。
- 任务 A、B 可以同时执行。
- 后台输出归属正确。
- 停止任务互不影响。
- 模型客户端、文件存储、沙盒和 LSP 的真实并行能力得到验证。

并行验证必须建立在每个任务的状态和 Runtime 依赖完全独立的基础上。

## 4. 只验收现有界面行为

- 长任务名显示。
- 长路径显示。
- 原位重命名。
- 确认后关闭编辑状态。
- 任务重命名不影响会话标题。
- 最小宽度下运行选项与底部按钮不冲突。
- 历史图标与思考强度选择框底部对齐。
- 任务菜单与任务名垂直居中。
- Hover、Pressed、Disabled 和键盘焦点状态。
- 图标颜色继承。
- 普通按钮不受 Danger 配色污染。
- Mica 系统合成效果。
- 窗口非激活状态。
- 不支持 Mica 时的浅色不透明回退。
- 设置页全部展开时的信息密度。
- 设置页保持单层功能分区。
- 运行相关专项测试。
- 运行最新全量测试。

禁止再次查看视觉设计稿。Headless 测试不能代替 Windows 实机材质验收。

## 5. 永久非目标

以下事项一定不做：

- 全局历史目录重构。
- 会话挂载注册表、单一可写挂载、加载预留、迟到事件协调。
- 后台历史索引增量同步体系。
- 持久化任务顺序。
- 持久化活跃任务。
- 持久化当前会话引用。
- 持久化自动压缩、DotNet Run、循环执行等运行选项。
- 持久化输入草稿、附件、运行状态或中断状态。
- 崩溃后自动续跑。
- 多进程并发写入协调、跨进程锁或跨进程隔离。
- 自动创建 Git worktree。
- 多 Agent 自动协作、任务依赖图或自动任务调度。
- 多窗口或远程多用户管理。
- 为以上事项增加任何预留设计。

## 6. 已认可界面，不得回退

- Windows 11 Fluent 风格，默认简体中文，全浅色。
- 原生窗口标题栏；申请 `Mica, None`，不支持时回退浅色不透明背景。
- 透明侧栏承接 Mica；右侧单层圆角内容表面。
- 侧栏显示品牌、新建工作任务、任务列表、全部历史会话和设置。
- 不恢复“我的工作任务”标题及数量行。
- 任务菜单与标题垂直居中。
- 原位重命名只有确认后提交；空名称保持编辑。
- 窗口标题顺序为应用名称、任务名称、工作目录、会话标题，空项省略。
- 聊天页不恢复重复任务标题栏和顶部独占操作栏。
- 工作目录、模型、思考强度保持紧凑配置布局。
- 历史按钮位于思考强度右侧并与选择框底部对齐。
- 输入面板与配置区域外边界对齐。
- 添加图片、压缩对话、结束 LSP 服务使用 Secondary 按钮外观。
- 新建会话位于结束 LSP 服务右侧。
- 停止按钮位于发送按钮旁。
- 快捷键提示保持“Ctrl + Enter 发送 · Enter 换行”。
- 正文可选择、可复制。
- Danger 样式不得覆盖 Fluent 公共按钮资源键。
- 字体优先使用微软雅黑 UI/微软雅黑并保留跨平台回退。
- 设置页保持单层功能分区。
- `Assets/Icons/CodingChatRoom_48x48.png` 必须继续作为 AvaloniaResource 打包。

## 7. 关键代码

| 文件 | 关注内容 |
| --- | --- |
| `Infrastructure/CodingChatRoomPaths.cs` | `WorkTasks.json` 路径 |
| `Services/CodingChatStartup.cs` | 每任务独立 Runtime 创建 |
| `Services/CodingChatRuntime.cs` | Runtime 所有权和释放 |
| `Services/CodingChatApplication.cs` | 停止、保存、历史恢复和路径同步 |
| `ViewModels/MainViewModel.cs` | 任务创建、恢复、保存、删除和异常展示 |
| `ViewModels/WorkTaskItemViewModel.cs` | 稳定 ID、任务订阅和状态 |
| `ViewModels/ChatViewModel.cs` | 模型、强度、路径、运行状态和事件解除 |
| `ViewModels/SessionListViewModel.cs` | 历史打开错误和事件解除 |
| `App.axaml.cs` | 启动恢复与退出释放全部 Runtime |
| `Views/MainView.axaml` / `ChatView.axaml` | 既有布局和交互验收 |
| `Styles/Controls.axaml` / `Settings.axaml` | Fluent 状态和设置层级 |

## 8. 验证规则

- 先读代码和现有测试，再决定是否修改。
- 只修改确认存在的真实问题。
- 先运行新增或受影响的专项测试，再运行全量测试。
- 对模型、沙盒、LSP、Mica 等依赖真实环境的项目，记录实际验证结果。
- 明确区分已实现、已测试和因环境限制未验证的事项。
