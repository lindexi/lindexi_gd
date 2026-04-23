# 标签关闭确认与临时快照自动保存

## 场景

`SimpleWrite` 的标签页关闭动作需要先判断当前内容是否已经落到本地文件。

这次改动同时覆盖两条链路：

1. 关闭标签页时，对“非空且有未保存内容”的文档给出窗口内确认面板。
2. 文本编辑停止 3 秒后，把当前内容写入 `AppPathManager.TempDirectory` 下的临时快照，并只保留最近 10 个版本。

## 当前实现

### 1. 关闭确认入口

- 标签上的关闭按钮在 `TabBar.axaml.cs` 中转发到 `EditorViewModel.RequestCloseDocumentAsync`。
- `Ctrl+W` 在 `ShortcutManagerHelper` 中也统一走 `RequestCloseCurrentDocumentAsync`。
- 真正删除标签的逻辑仍然留在 `EditorViewModel` 内部的关闭核心方法，避免 View 直接处理业务判断。

### 2. 何时弹出确认

`EditorViewModel.ShouldConfirmClose` 的规则是：

- 文档不是空白标签；
- 并且 `SaveStatus != SaveStatus.Saved`。

因此会覆盖两类文档：

- 已关联本地文件，但还有未保存修改；
- 还没有正式保存到本地的草稿标签。

### 3. 窗口内确认面板

- 确认 UI 放在 `SimpleWriteMainView.axaml` 顶层 `Grid` 的覆盖层里，不新开窗口。
- 面板状态由 `SimpleWriteMainViewModel` 持有。
- View 只负责按钮点击事件转发：
  - 保存并关闭
  - 直接关闭
  - 继续编辑

### 4. 深色主题样式

- 新增的对话框样式放在 `SimpleWrite/Styles/DialogStyles.axaml`。
- 颜色统一复用 `Brushes.axaml` 中的共享画刷，并补充：
  - 遮罩背景
  - 对话框背景
  - 徽标背景
- `MainStyles.axaml` 负责聚合新样式，避免在单个视图里硬编码。

### 5. 临时快照自动保存

- 自动保存实现放在 `TempDocumentAutoSaveService`。
- 每个 `EditorModel` 使用 `DocumentId` 作为稳定标识，单独维护一组快照。
- 文本变化后会重置延时计时器：
  - 3 秒内继续编辑，则取消前一次等待；
  - 停止编辑满 3 秒，才真正落盘。
- 关闭标签前也会执行一次 `FlushAsync`，尽量把最后一次修改写进临时目录。

### 6. 快照文件组织

- 根目录：`AppPathManager.TempDirectory`
- 子目录：按 `EditorModel.DocumentId` 分组
- 文件名：`时间戳__文档名.扩展名`
- 版本清理：每组只保留最近 10 个文件，超出的最旧文件自动删除

## 注意事项

- `SaveDocument` / `SaveDocumentAs` 改为返回 `Task<bool>`，用于关闭流程判断“是否真的保存成功”。
- `另存为` 被用户取消时，应恢复原先的保存状态，而不是把文档误标成错误。
- 新增 UI 交互后，要优先运行一次构建，及时发现 XAML 样式或绑定问题。

## 适用场景

- 调整标签页关闭确认文案或按钮策略。
- 排查草稿文档误关闭的问题。
- 修改临时恢复目录结构、快照保留数量或自动保存节流时间。
