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

## 查找选项扩展

- 查找栏支持“忽略大小写”和“正则”两个选项，直接由 `FindReplaceViewModel` 持有状态。
- 文档内查找、替换、文件夹搜索统一复用同一套匹配规则，避免不同入口行为不一致。
- 开启“正则”后，如果表达式无效，导航按钮与文件夹搜索按钮会自动禁用，状态栏显示错误提示。
- 开启“正则”后的替换沿用 .NET 正则替换语义，可使用捕获组替换文本。
- 正则输入需要先做轻量结构校验，再决定是否构造 `Regex`，避免用户逐字输入时持续触发异常。
- 正则匹配必须放到后台线程执行，并统一使用默认 `3s` 超时，超时后只回写状态，不阻塞 UI 线程。
- 新增查找选项属于交互控件扩展，不能依赖 Avalonia 默认浅色外观；需要显式接入 `MainStyles.axaml` 聚合的深色共享样式。
