# FunctionalGrid 中的查找替换面板

## 场景

为 `SimpleWriteMainView` 增加查找与替换功能时，需要让界面出现在 `FunctionalGrid` 中，并且保持当前项目的 MVVM 分层方式。

## 实现约定

1. 新增 `FindReplaceViewModel` 负责：
   - 面板显示与隐藏；
   - 当前文档查找结果统计；
   - 查找上一个、下一个、替换当前项、全部替换。
2. `Ctrl+F` / `Ctrl+H` 仍然统一在 `ShortcutManagerHelper.AddDefaultShortcut` 注册。
3. `SimpleWriteMainViewModel` 负责桥接 `FindReplaceViewModel.SearchStatusText` 到 `StatusViewModel`，避免视图直接修改状态栏文本。
4. `FindReplaceBar` 视图只处理界面交互细节：
   - 面板显示后聚焦输入框；
   - `Esc` 关闭面板；
   - 关闭后焦点返回当前 `TextEditor`。

## 状态栏规则

- 当面板打开且输入了查找文本时，`StatusText` 追加查找统计信息。
- 格式为：`[查找: N 项，第 M 项]`。
- 没有命中时显示：`[查找: 0 项]`。

## 交互细节

- 若当前编辑器存在非空单行选区，打开查找或替换面板时会优先把选中文本带入查找框。
- 查找结果通过设置 `TextEditor.CurrentSelection` 定位，滚动由现有 `MainEditorView` 光标可见逻辑自动处理。
