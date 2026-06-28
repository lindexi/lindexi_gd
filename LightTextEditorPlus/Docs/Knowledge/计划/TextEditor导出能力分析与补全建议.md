# TextEditor 导出能力分析与补全建议

> 基于 `TextEditor` → `SkiaTextEditor` → `TextEditorCore` 继承链全量公开 API，对照 `TextEditor.cs` 当前已导出的 `[UnmanagedCallersOnly]` 方法，找出缺口与后续导出方向。

---

## 1. TextEditor 继承链公开 API 全览

### 1.1 继承链

```
TextEditor (AotLayer, internal) : SkiaTextEditor
  └─ SkiaTextEditor (partial class, IRenderManager)
       └─ TextEditorCore (组合, public 属性 TextEditorCore)
            └─ DocumentManager (组合, public 属性 DocumentManager)
```

### 1.2 当前已导出方法

| EntryPoint | 签名 | 说明 |
|---|---|---|
| `CreateTextEditor` | `int ()` | 创建文本编辑器 |
| `FreeTextEditor` | `int (int textEditorId)` | 释放 |
| `SetDocumentWidth` | `int (int, double)` | 设置文档宽度 |
| `SetDocumentHeight` | `int (int, double)` | 设置文档高度 |
| `AppendText` | `int (int, IntPtr, int)` | 追加文本 |
| `SaveAsImageFile` | `int (int, IntPtr, int)` | 保存为图片 |

共 6 个方法，覆盖了创建/释放、基本文档配置、文本追加、图片导出。

### 1.3 继承链公开 API 完整清单

#### SkiaTextEditor（渲染与编辑入口）

| 方法/属性 | 签名 | 已导出 | 适合导出 | 备注 |
|---|---|---|---|---|
| `AppendText` | `void (string)` | ✅ | — | |
| `AppendRun` | `void (SkiaTextRun)` | ❌ | ✅ P0 | 带属性的文本追加 |
| `EditAndReplace` | `void (string, Selection?)` | ❌ | ✅ P0 | 替换选区文本 |
| `EditAndReplaceRun` | `void (SkiaTextRun, Selection?)` | ❌ | ✅ P1 | 带属性替换 |
| `Backspace` | `void ()` | ❌ | ✅ P0 | 退格删除 |
| `Delete` | `void ()` | ❌ | ✅ P0 | 向前删除 |
| `Remove` | `void (in Selection)` | ❌ | ✅ P0 | 按选区删除 |
| `SaveAsImageFile` | `void (string)` | ✅ | — | |
| `GetCurrentTextRender` | `ITextEditorContentSkiaRenderer ()` | ❌ | ❌ | 返回复杂对象，不适合跨边界 |
| `BuildTextEditorSkiaRender` | `…(in TextEditorSkiaRenderContext)` | ❌ | ❌ | 自定义渲染，参数复杂 |
| `WaitForRenderCompletedAsync` | `Task ()` | ❌ | ✅ P1 | 需同步化 |
| `DisableAutoFlushCaretAndSelectionRender` | `void ()` | ❌ | ❌ | UI 框架相关 |
| `CaretConfiguration` | `SkiaCaretConfiguration { get; set; }` | ❌ | ❌ | UI 配置对象 |
| `RenderConfiguration` | `SkiaTextEditorRenderConfiguration { get; set; }` | ❌ | ❌ | UI 配置对象 |
| `DebugConfiguration` | `SkiaTextEditorDebugConfiguration { get; }` | ❌ | ❌ | 调试专用 |
| `Logger` | `ITextLogger { get; }` | ❌ | ❌ | 日志基础设施 |
| `InvalidateVisualRequested` | `event` | ❌ | ❌ | 事件，需回调机制 |

#### TextEditorCore.Property（文档配置与状态）

| 方法/属性 | 签名 | 已导出 | 适合导出 | 备注 |
|---|---|---|---|---|
| `CurrentCaretOffset` | `CaretOffset { get; set; }` | ❌ | ✅ P0 | 光标位置 |
| `CurrentSelection` | `Selection { get; set; }` | ❌ | ✅ P0 | 选区 |
| `Select` | `void (in Selection)` | ❌ | ✅ P0 | 设置选区 |
| `MoveCaret` | `void (CaretMoveType)` | ❌ | ✅ P1 | 键盘导航 |
| `IsDirty` | `bool { get; }` | ❌ | ✅ P1 | 布局是否待完成 |
| `IsUpdatingLayout` | `bool { get; }` | ❌ | ✅ P2 | 布局中状态 |
| `VerticalTextAlignment` | `VerticalTextAlignment { get; set; }` | ❌ | ✅ P1 | 垂直对齐（byte 枚举） |
| `SizeToContent` | `TextSizeToContent { get; set; }` | ❌ | ✅ P1 | 自适应尺寸（byte 枚举） |
| `ArrangingType` | `ArrangingType { get; set; }` | ❌ | ✅ P2 | 横排/竖排（复杂结构体） |
| `LineSpacingStrategy` | `LineSpacingStrategy { get; set; }` | ❌ | ✅ P2 | 行距策略 |
| `LineSpacingAlgorithm` | `LineSpacingAlgorithm { get; set; }` | ❌ | ✅ P2 | 行距算法 |
| `LineSpacingConfiguration` | `DocumentLineSpacingConfiguration { get; set; }` | ❌ | ❌ | 复杂结构体 |
| `CurrentCulture` | `CultureInfo { get; set; }` | ❌ | ❌ | CultureInfo 不可直接封送 |
| `Features` | `TextFeatures { get; set; }` | ❌ | ❌ | 功能开关，内部优化 |
| `ParagraphList` | `ReadOnlyParagraphList { get; }` | ❌ | ❌ | 复杂对象图 |

#### TextEditorCore.Edit（文本编辑）

| 方法 | 签名 | 已导出 | 适合导出 | 备注 |
|---|---|---|---|---|
| `AppendText` | `void (string)` | ✅ | — | 通过 SkiaTextEditor |
| `AppendRun` | `void (IImmutableRun)` | ❌ | ✅ P0 | 带属性追加 |
| `EditAndReplace` | `void (string, Selection?)` | ❌ | ✅ P0 | 替换文本 |
| `EditAndReplaceRun` | `void (IImmutableRun?, Selection?)` | ❌ | ✅ P1 | 带属性替换 |
| `Backspace` | `void ()` | ❌ | ✅ P0 | 退格 |
| `Delete` | `void ()` | ❌ | ✅ P0 | 向前删除 |
| `Remove` | `void (in Selection)` | ❌ | ✅ P0 | 选区删除 |
| `RemoveParagraph` | `void (ITextParagraph)` | ❌ | ❌ | 段落对象不可封送 |
| `DeleteForwardWord` | `void ()` | ❌ | ✅ P2 | 删除前向词 |
| `DeleteBackwardWord` | `void ()` | ❌ | ✅ P2 | 删除后向词 |
| `Clear` | `void ()` [Obsolete] | ❌ | ✅ P1 | 清空文本 |

#### TextEditorCore.RenderInfo（布局与渲染信息）

| 方法 | 签名 | 已导出 | 适合导出 | 备注 |
|---|---|---|---|---|
| `GetDocumentLayoutBounds` | `DocumentLayoutBounds ()` | ❌ | ✅ P1 | 布局尺寸 |
| `TryGetRenderInfo` | `bool (out RenderInfoProvider?)` | ❌ | ❌ | 返回复杂对象 |
| `GetRenderInfo` | `RenderInfoProvider ()` | ❌ | ❌ | 返回复杂对象 |
| `WaitLayoutCompletedAsync` | `Task ()` | ❌ | ✅ P1 | 需同步化 |
| `TryHitTest` | `bool (in TextPoint, out TextHitTestResult)` | ❌ | ✅ P2 | 命中测试 |
| `GetCurrentGuidingLayoutInfo` | `GuidingLayoutInfo ()` | ❌ | ❌ | 高级布局 |
| `SetGuidingLayoutInfoForNextUpdateLayout` | `bool (GuidingLayoutInfo)` | ❌ | ❌ | 高级布局 |

#### DocumentManager（文档管理）

| 方法/属性 | 签名 | 已导出 | 适合导出 | 备注 |
|---|---|---|---|---|
| `DocumentWidth` | `double { get; set; }` | ✅ Set | ✅ P1 Get | 缺 Get |
| `DocumentHeight` | `double { get; set; }` | ✅ Set | ✅ P1 Get | 缺 Get |
| `CharCount` | `int { get; }` | ❌ | ✅ P0 | 字符数量 |
| `StyleRunProperty` | `IReadOnlyRunProperty { get; }` | ❌ | ✅ P1 | 当前样式属性（配合 RunProperty 导出） |
| `SetStyleTextRunProperty<T>` | `void (ConfigReadOnlyRunProperty<T>)` | ❌ | ✅ P1 | 设置默认样式（配合 RunPropertyId） |
| `SetRunProperty<T>` | `void (ConfigReadOnlyRunProperty<T>, Selection?)` | ❌ | ✅ P1 | 设置选区字符属性 |
| `SetStyleParagraphProperty` | `void (ParagraphProperty)` | ❌ | ❌ | 段落属性复杂 |
| `SetParagraphProperty` | `void (ParagraphIndex, ParagraphProperty)` | ❌ | ❌ | 段落属性复杂 |
| `GetParagraphProperty` | `ParagraphProperty (ParagraphIndex)` | ❌ | ❌ | 段落属性复杂 |
| `GetParagraph` | `ITextParagraph (ParagraphIndex)` | ❌ | ❌ | 段落对象不可封送 |
| `IsInitializingTextEditor` | `bool ()` | ❌ | ✅ P2 | 是否初始化中 |

#### TextEditor.Edit.Shared（文本获取，共享代码）

| 方法 | 签名 | 已导出 | 适合导出 | 备注 |
|---|---|---|---|---|
| `GetText` | `string (in Selection)` | ❌ | ✅ P0 | 获取选区文本（两段式字符串） |
| `GetSelectedText` | `string ()` | ❌ | ✅ P0 | 获取当前选中文本 |
| `GetText` | `StringBuilder (in Selection, StringBuilder)` | ❌ | ❌ | StringBuilder 不适合跨边界 |

---

## 2. 缺口分析

### 2.1 P0 — 核心编辑能力缺失

当前仅有 `AppendText`，缺少文本编辑的基本闭环。

| 缺失导出 | 对应 API | 说明 |
|---|---|---|
| `GetText` | `GetText(in Selection)` | 读取文本，采用两段式字符串（先获取长度，再读取内容） |
| `GetCharCount` | `DocumentManager.CharCount` | 获取文档字符总数 |
| `EditAndReplace` | `EditAndReplace(string, Selection?)` | 替换选区文本，无选区时在光标处插入 |
| `Backspace` | `Backspace()` | 退格删除 |
| `Delete` | `Delete()` | 向前删除 |
| `Remove` | `Remove(in Selection)` | 按选区范围删除 |
| `GetCurrentCaretOffset` | `CurrentCaretOffset` (get) | 获取光标位置 |
| `SetCurrentCaretOffset` | `CurrentCaretOffset` (set) | 设置光标位置 |
| `GetCurrentSelection` | `CurrentSelection` (get) | 获取选区（起点+终点） |
| `SetCurrentSelection` | `CurrentSelection` (set) | 设置选区 |

**Native 协议建议**：

```
// 文本读取：两段式字符串，与 GetRunPropertyFontName 模式一致
GetTextLength(int textEditorId, int startOffset, int length, int* textLength) -> int
GetText(int textEditorId, int startOffset, int length, IntPtr buffer, int charCount) -> int

// 字符数量
GetCharCount(int textEditorId, int* charCount) -> int

// 文本编辑
EditAndReplace(int textEditorId, IntPtr unicode16Text, int charCount,
               int hasSelection, int selectionStart, int selectionLength) -> int
Backspace(int textEditorId) -> int
Delete(int textEditorId) -> int
Remove(int textEditorId, int startOffset, int length) -> int

// 光标
GetCurrentCaretOffset(int textEditorId, int* caretOffset) -> int
SetCurrentCaretOffset(int textEditorId, int caretOffset) -> int

// 选区（用起点+长度表示）
GetCurrentSelection(int textEditorId, int* startOffset, int* length) -> int
SetCurrentSelection(int textEditorId, int startOffset, int length) -> int
```

### 2.2 P1 — 文档配置与布局信息

| 缺失导出 | 对应 API | 说明 |
|---|---|---|
| `GetDocumentWidth` | `DocumentWidth` (get) | 获取文档宽度 |
| `GetDocumentHeight` | `DocumentHeight` (get) | 获取文档高度 |
| `GetDocumentLayoutBounds` | `GetDocumentLayoutBounds()` | 获取布局尺寸（内容范围+外接范围） |
| `GetIsDirty` | `IsDirty` | 是否需要等待布局 |
| `WaitLayoutCompleted` | `WaitLayoutCompletedAsync()` | 等待布局完成（同步阻塞） |
| `SetVerticalTextAlignment` | `VerticalTextAlignment` (set) | 垂直对齐（byte 枚举） |
| `SetSizeToContent` | `SizeToContent` (set) | 自适应尺寸（byte 枚举） |
| `AppendRun` | `AppendRun(SkiaTextRun)` | 追加带属性的文本 |
| `Clear` | `Clear()` | 清空全部文本 |
| `SetStyleRunProperty` | `SetStyleTextRunProperty<T>` | 设置默认样式（配合 RunPropertyId） |
| `SetRunProperty` | `SetRunProperty<T>` | 设置选区字符属性（配合 RunPropertyId） |

**Native 协议建议**：

```
// 文档尺寸 Get
GetDocumentWidth(int textEditorId, double* width) -> int
GetDocumentHeight(int textEditorId, double* height) -> int

// 布局尺寸（内容范围 X/Y/Width/Height + 外接范围 X/Y/Width/Height）
GetDocumentLayoutBounds(int textEditorId,
    double* contentX, double* contentY, double* contentW, double* contentH,
    double* outlineX, double* outlineY, double* outlineW, double* outlineH) -> int

// 状态
GetIsDirty(int textEditorId, int* isDirty) -> int

// 等待布局完成（同步阻塞，内部轮询 IsDirty）
WaitLayoutCompleted(int textEditorId) -> int

// 垂直对齐（0=Top, 1=Center, 2=Bottom）
SetVerticalTextAlignment(int textEditorId, byte alignment) -> int

// 自适应尺寸（0=Manual, 1=Width, 2=Height, 3=WidthAndHeight）
SetSizeToContent(int textEditorId, byte sizeToContent) -> int

// 追加带属性的文本（使用已创建的 RunPropertyId）
AppendRun(int textEditorId, int runPropertyId, IntPtr unicode16Text, int charCount) -> int

// 清空文本
ClearText(int textEditorId) -> int

// 设置默认样式字符属性（将 RunPropertyId 关联为文本编辑器的默认样式）
SetStyleRunProperty(int textEditorId, int runPropertyId) -> int

// 设置选区字符属性
SetRunProperty(int textEditorId, int runPropertyId,
               int hasSelection, int selectionStart, int selectionLength) -> int
```

### 2.3 P2 — 高级能力

| 缺失导出 | 对应 API | 说明 |
|---|---|---|
| `MoveCaret` | `MoveCaret(CaretMoveType)` | 键盘导航移光标 |
| `DeleteForwardWord` | `DeleteForwardWord()` | 删除前向词（Ctrl+Backspace） |
| `DeleteBackwardWord` | `DeleteBackwardWord()` | 删除后向词（Ctrl+Delete） |
| `TryHitTest` | `TryHitTest(in TextPoint, out TextHitTestResult)` | 坐标命中测试 |
| `GetIsUpdatingLayout` | `IsUpdatingLayout` | 是否正在布局 |
| `SetArrangingType` | `ArrangingType` (set) | 横排/竖排切换 |
| `SetLineSpacingStrategy` | `LineSpacingStrategy` (set) | 行距策略 |
| `SetLineSpacingAlgorithm` | `LineSpacingAlgorithm` (set) | 行距算法 |
| `IsInitializingTextEditor` | `IsInitializingTextEditor()` | 是否初始化中 |
| `WaitForRenderCompleted` | `WaitForRenderCompletedAsync()` | 等待渲染完成 |

---

## 3. 不适合导出的 API

| API | 原因 |
|---|---|
| `GetCurrentTextRender` / `BuildTextEditorSkiaRender` | 返回 `ITextEditorContentSkiaRenderer` 复杂对象，无法跨边界封送 |
| `TryGetRenderInfo` / `GetRenderInfo` | 返回 `RenderInfoProvider` 复杂对象 |
| `ParagraphList` | `ReadOnlyParagraphList` 对象图，无法封送 |
| `GetParagraph` / `SetParagraphProperty` | `ITextParagraph` / `ParagraphProperty` 不可封送 |
| `CaretConfiguration` / `RenderConfiguration` | UI 框架配置对象，属性复杂 |
| `DebugConfiguration` | 调试专用 |
| `Logger` | 日志基础设施接口 |
| `CurrentCulture` | `CultureInfo` 不可直接封送 |
| `LineSpacingConfiguration` | 复杂结构体 |
| `Features` / `EnableFeatures` / `DisableFeatures` | 功能开关，内部优化用 |
| 所有事件（`InvalidateVisualRequested`、`LayoutCompleted` 等） | 需回调机制，当前 Native 协议不支持 |
| `RemoveParagraph` | 依赖 `ITextParagraph` 对象 |
| `SetGuidingLayoutInfo` / `GetCurrentGuidingLayoutInfo` | `GuidingLayoutInfo` 复杂结构体 |

---

## 4. 总结对照表

| 能力分类 | 已导出 | 待补全（P0） | 待补全（P1） | 待补全（P2） |
|---|---|---|---|---|
| 生命周期 | Create / Free | — | — | — |
| 文本编辑 | AppendText | EditAndReplace / Backspace / Delete / Remove | AppendRun / Clear | DeleteForwardWord / DeleteBackwardWord |
| 文本读取 | — | GetText / GetCharCount | — | — |
| 光标选区 | — | Get/SetCaretOffset / Get/SetSelection | — | MoveCaret |
| 文档配置 | SetWidth / SetHeight | — | GetWidth / GetHeight / SetVerticalTextAlignment / SetSizeToContent | SetArrangingType / SetLineSpacing* |
| 布局信息 | — | — | GetDocumentLayoutBounds / GetIsDirty / WaitLayoutCompleted | GetIsUpdatingLayout / TryHitTest |
| 样式属性 | — | — | SetStyleRunProperty / SetRunProperty | — |
| 渲染导出 | SaveAsImageFile | — | — | WaitForRenderCompleted |

---

## 5. 后续导出优先级

| 优先级 | 导出项 | 理由 |
|---|---|---|
| **P0** | `GetText` + `GetCharCount` | 无读取能力的文本编辑器无法使用 |
| **P0** | `EditAndReplace` + `Backspace` + `Delete` + `Remove` | 编辑基本闭环 |
| **P0** | `Get/SetCurrentCaretOffset` + `Get/SetCurrentSelection` | 光标和选区是编辑基础 |
| **P1** | `GetDocumentWidth/Height` | 补全 Get 侧 |
| **P1** | `GetDocumentLayoutBounds` + `GetIsDirty` + `WaitLayoutCompleted` | 布局状态查询 |
| **P1** | `SetVerticalTextAlignment` + `SetSizeToContent` | 文档排版配置 |
| **P1** | `AppendRun` + `Clear` | 带属性追加、清空 |
| **P1** | `SetStyleRunProperty` + `SetRunProperty` | 与 RunProperty 导出器联动 |
| **P2** | `MoveCaret` + `TryHitTest` | 高级交互 |
| **P2** | `SetArrangingType` + `SetLineSpacing*` | 竖排与行距 |
