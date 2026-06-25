# 文本编辑器浮动命令面板（CommandPalette）设计方案

## 背景与动机

当前 `SimpleWriteTextEditor` 通过 `OnRaisePrepareContextMenuEvent` 用右键 `ContextMenu` 实现命令菜单。这种方式限制很多：

| 限制 | 说明 |
|---|---|
| 无输入框 | `MenuItem` 只有 `Header`，无法嵌入 `TextBox` 实现搜索过滤 |
| 无快捷键拦截 | 菜单弹出期间焦点在菜单上，无法自定义键盘交互 |
| 无搜索/过滤 | 命令多了只能滚动列表，不能输入关键词过滤 |
| UI 表现力差 | 只能是扁平菜单项，无法做分组、图标、预览等 |
| 异步匹配阻塞 | `await pattern.IsMatchAsync(text)` 在菜单弹出前串行执行，命令多了会卡 |
| 布局受限 | 菜单位置由系统决定，不能精确控制 |

因此决定用自定义浮动用户控件替换 `ContextMenu`。

## 替代方案：浮动用户控件

### 整体架构

```
MainEditorView (UserControl)
└── Border
    └── Panel (新增，用于叠加)
        ├── ScrollViewer (原有的)
        │   └── EditorGrid
        │       ├── LineNumberControl (Column 0)
        │       └── TextEditor (Column 1)
        └── CommandPaletteView (新增，浮动面板，Z序在上)
            ├── DragHandle (拖拽条)
            ├── TextBox (搜索/输入)
            └── ListBox (过滤后的命令列表)
```

### 关键设计点

#### 1. 锚点定位策略

**锚点取决于触发方式**：

| 触发方式 | 锚点来源 | 说明 |
|---|---|---|
| 右键 | `e.GetPosition(TextEditor)` | 以鼠标右击位置为准 |
| `Alt+Enter` | `CaretRenderInfo.GetCaretBounds()` 底部 | 以光标位置为准 |

**三层坐标系**：

- **文档坐标系** — `TextPoint` / `TextRect`，原点在 `TextEditor` 控件左上角
- **ScrollViewer 内坐标** — 文档坐标减去 `ScrollViewer.Offset`（滚动偏移）
- **MainEditorView 坐标** — 最外层容器的坐标，用于放置浮动面板

**定位方案**：

面板放在 `Panel` 中，与 `ScrollViewer` 同级，不受滚动影响且不被裁剪。滚动时面板位置不动。

用 `Visual.TranslatePoint` 做坐标转换，避免手动计算 `ScrollViewer.Offset`、`Padding`、`Grid` 列偏移等中间变换：

```
// 1. 确定锚点：右键用鼠标位置，Alt+Enter 用光标位置
// 2. textEditor.TranslatePoint(anchorPoint, mainEditorView)
//    → 自动处理所有中间变换，得到 MainEditorView 坐标
// 3. 根据 viewPoint 计算面板最终位置（含边界翻转）
```

**边界翻转逻辑**：

- 默认在锚点下方弹出
- 水平溢出：向左偏移，保持面板在可见范围内
- 垂直溢出：翻到锚点上方

#### 2. 命令匹配保持串行

当前所有 `ICommandPattern.IsMatchAsync` 实现都是同步 CPU 工作，用 `ValueTask.FromResult` 返回，没有真正的异步 I/O：

| 实现 | IsMatchAsync 实际行为 |
|---|---|
| `DelegateCommandPattern` | 调用传入的 func 或直接返回 true |
| `OpenUrlCommandPattern` | `TryGetUrl()` 纯字符串解析 |
| `TextToBase64CommandPattern` | `CanShowSidebarConversation() && !string.IsNullOrWhiteSpace(text)` |
| `CalculatorCommandPattern` | `TryEvaluate()` 纯算术解析 |

因此保持串行匹配。用 `Task.WhenAll` 并行化不仅无意义（没有 I/O 可并行），还可能引入线程安全问题——`CalculatorCommandPattern` 会写 `_lastResult` 实例字段，如果跑在线程池上就有竞态。

匹配结果作为初始候选列表，用户在搜索框输入时再做客户端文本过滤。

#### 3. 触发方式与旧链路拦截

- **右键**：`SimpleWriteTextEditorHandler.OnPointerReleased` 中检测右键，触发 overlay（不调用 base，截断旧链路）
- **快捷键 `Alt+Enter`**：通过 `ShortcutManagerHelper.AddDefaultShortcut` 注册
- **删除 `OnRaisePrepareContextMenuEvent`**：`SimpleWriteTextEditor` 中直接删除 override。`OnRaisePrepareContextMenuEvent` 只在 `TextEditorHandler.OnPointerReleased` 检测到右键时被调用，而 `SimpleWriteTextEditorHandler` 已在 override 中截断右键链路，不会走到 base。键盘 Context Menu 键走的是 `OnKeyDown`，不经过此链路

#### 4. 关闭行为

- 按 `Esc` 关闭
- 点击面板外部关闭
- 滚动 `ScrollViewer` **不**关闭面板，面板位置不动（因为面板在 `ScrollViewer` 外的 `Panel` 层）

#### 5. 面板可拖动

面板顶部有一个 `DragHandle`（拖拽条），鼠标拖拽时用 `RenderTransform`（`TranslateTransform`）实现面板偏移。

关键实现细节：
- 拖拽时用 `GetVisualRoot()` 获取根窗口坐标，避免坐标空间混乱
- `DragHandle` 设置 `Cursor="SizeAll"`

#### 6. 焦点与快捷键

面板打开时 `TextBox` 自动获取焦点：
- `↑` / `↓`：切换列表选中项
- `Enter`：执行选中命令
- `Esc`：关闭面板

## 文件改动清单

| 操作 | 文件 | 说明 |
|---|---|---|
| 新增 | `SimpleWrite/ViewModels/CommandPaletteItem.cs` | 数据模型，记录模式与匹配结果 |
| 新增 | `SimpleWrite/ViewModels/CommandPaletteViewModel.cs` | 面板 ViewModel，管理搜索过滤、选中项、可见性 |
| 新增 | `SimpleWrite/Views/Components/CommandPaletteView.axaml` | 面板界面：拖拽条 + 搜索框 + 命令列表 |
| 新增 | `SimpleWrite/Views/Components/CommandPaletteView.axaml.cs` | 面板 code-behind：定位、边界翻转、拖拽、键盘导航、焦点管理 |
| 修改 | `SimpleWrite/Views/Components/MainEditorView.axaml` | 用 `Panel` 包裹 `ScrollViewer`，叠加 `CommandPaletteView` |
| 修改 | `SimpleWrite/Views/Components/MainEditorView.axaml.cs` | 收集选区文本、计算锚点、调用 `CommandPaletteView.Show`、点击外部关闭 |
| 修改 | `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs` | 删除 `OnRaisePrepareContextMenuEvent`、`ContextMenu` 相关代码 |
| 修改 | `SimpleWrite/Business/TextEditors/SimpleWriteTextEditorHandler.cs` | 右键拦截，触发 `CommandPaletteRequested` 事件（带鼠标位置） |
| 修改 | `SimpleWrite/Business/ShortcutManagers/ShortcutManagerHelper.cs` | 注册 `Alt+Enter` 快捷键 |
| 修改 | `SimpleWrite/ViewModels/EditorViewModel.cs` | 持有 `CommandPaletteViewModel` |

## 职责拆分

| 层/类型 | 职责 | 不负责什么 |
|---|---|---|
| `SimpleWriteTextEditor` | 暴露选区文本、光标坐标 | 不直接决定面板 UI |
| `SimpleWriteTextEditorHandler` | 拦截右键事件、抛出带鼠标位置的 `CommandPaletteRequested` 事件 | 不处理定位计算 |
| `CommandPaletteViewModel` | 管理候选列表、搜索过滤、选中命令、执行回调 | 不处理定位坐标 |
| `CommandPaletteView` | 定位（`TranslatePoint` + 边界翻转）、焦点管理、拖拽、键盘导航 | 不处理命令匹配逻辑 |
| `MainEditorView` | 收集选区文本、计算锚点（右键/光标）、调用 `CommandPaletteView.Show`、点击外部关闭 | 不处理面板内部布局 |
| `EditorViewModel` | 持有 `CommandPaletteViewModel` 实例 | 不决定面板何时打开 |
| `ShortcutManager` | 提供 `Alt+Enter` 快捷键绑定 | 不直接触发面板逻辑 |

## 旧代码清理

直接删除以下内容（不需要 `[Obsolete]` 兼容）：

1. `SimpleWriteTextEditor` 构造函数的 `ContextMenu = new ContextMenu();` 和 `ContextMenu.Closed += ...`
2. `SimpleWriteTextEditor.OnRaisePrepareContextMenuEvent` override 方法直接删除
3. 打开的 `ContextMenu.Items.Clear();` 回调

## 开发者排查顺序

1. `SimpleWrite/Views/Components/MainEditorView.axaml` — 了解布局结构
2. `SimpleWrite/Views/Components/MainEditorView.axaml.cs` — 了解 overlay 生命周期
3. `SimpleWrite/Views/Components/CommandPaletteView.axaml(.cs)` — 面板 UI
4. `SimpleWrite/ViewModels/CommandPaletteViewModel.cs` — 面板数据层
5. `SimpleWrite/Business/TextEditors/EditorOverlayService.cs` — 坐标计算
6. `SimpleWrite/Business/TextEditors/SimpleWriteTextEditorHandler.cs` — 右键拦截
7. `SimpleWrite/Business/ShortcutManagers/ShortcutManagerHelper.cs` — 快捷键

## 坐标系分析

### 三层坐标空间

```
文档坐标系 (TextPoint/TextRect)
    ← 原点: TextEditor 控件左上角
    ← RenderInfoProvider.GetSelectionBoundsList() 返回此坐标
    ← CaretRenderInfo.GetCaretBounds() 返回此坐标
    ↓ TranslatePoint 自动转换，无需手动计算

MainEditorView 坐标系 (Avalonia.Point)
    ← 原点: MainEditorView 左上角
    ← 用于设置 Panel 中子控件的 Margin/位置

屏幕坐标系 (Avalonia.Point in Root)
    ← 原点: 窗口左上角
    ← 用于拖拽时的坐标跟踪
```

### 为什么不用手动计算

手动计算需要考虑的变换链：

```
文档坐标
  + TextEditor 在 EditorGrid 中的偏移 (Column 1，即 LineNumberControl 宽度)
  + EditorGrid 在 ScrollViewer 中的偏移 (Padding.Left)
  - ScrollViewer.Offset (滚动偏移)
  + ScrollViewer 在 Panel 中的偏移
  + Panel 在 Border 中的偏移
  + Border 在 MainEditorView 中的偏移
= MainEditorView 坐标
```

其中 `LineNumberControl` 的可见性动态变化（`IsVisible="{Binding IsLineNumberVisible}"`），手动计算极容易出错。用 `TranslatePoint` 一次性处理全部。

## 关键代码片段

### 坐标转换 + 边界翻转 (EditorOverlayService)

```csharp
private async Task OpenAsync(SimpleWriteTextEditor textEditor, CommandPatternManager? manager,
    MainEditorView mainEditorView, Point? rightClickPosition = null)
{
    // 1. 收集上下文文本
    string selectedText = GetContextText(textEditor, rightClickPosition);
    if (string.IsNullOrEmpty(selectedText))
    {
        return;
    }

    // 2. 计算锚点坐标
    Point anchorPoint;
    if (textEditor.TextEditorCore.TryGetRenderInfo(out var renderInfo, autoLayoutEmptyTextEditor: false))
    {
        var selection = textEditor.CurrentSelection;

        if (!selection.IsEmpty)
        {
            var boundsList = renderInfo.GetSelectionBoundsList(in selection);
            if (boundsList.Count > 0)
            {
                var firstBounds = boundsList[0];
                anchorPoint = new Point(firstBounds.X, firstBounds.Bottom);
            }
            else
            {
                anchorPoint = default;
            }
        }
        else
        {
            var caretInfo = renderInfo.GetCaretRenderInfo(textEditor.CurrentCaretOffset);
            var caretBounds = caretInfo.GetCaretBounds(textEditor.CaretConfiguration.CaretThickness);
            anchorPoint = new Point(caretBounds.X, caretBounds.Bottom);
        }
    }
    else
    {
        anchorPoint = default;
    }

    // 3. 转换为 MainEditorView 坐标
    if (textEditor.TranslatePoint(anchorPoint, mainEditorView) is { } viewPoint)
    {
        _currentPosition = CalculatePanelPosition(viewPoint, mainEditorView.Bounds.Size);
    }
    else
    {
        _currentPosition = null;
    }

    // 4. 触发 ViewModel 打开
    await _viewModel.OpenAsync(selectedText, manager);
    _isOpen = true;
}

private Point? CalculatePanelPosition(Point anchorPoint, Size hostSize)
{
    const double PanelWidth = 320;
    const double PanelMaxHeight = 300;
    const double Margin = 4;

    double x = anchorPoint.X;
    double y = anchorPoint.Y + Margin;

    // 水平：右侧溢出时向左偏移
    if (x + PanelWidth > hostSize.Width)
    {
        x = Math.Max(0, hostSize.Width - PanelWidth - Margin);
    }

    // 垂直：下方空间不够则翻到上方
    double spaceBelow = hostSize.Height - y;
    if (spaceBelow < PanelMaxHeight)
    {
        y = anchorPoint.Y - PanelMaxHeight - Margin;
        if (y < 0)
        {
            y = Margin;
        }
    }

    return new Point(x, y);
}
```

### 拖拽实现 (CommandPaletteView.axaml.cs)

```csharp
private bool _isDragging;
private Point _dragStartPoint;
private Point _dragStartOffset;

private void DragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    _isDragging = true;
    _dragStartPoint = e.GetPosition(this.GetVisualRoot() as Visual);
    _dragStartOffset = new Point(
        RenderTransform is TranslateTransform t ? t.X : 0,
        RenderTransform is TranslateTransform t2 ? t2.Y : 0);
    e.Handled = true;
}

private void DragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
{
    if (!_isDragging) return;
    var currentPoint = e.GetPosition(this.GetVisualRoot() as Visual);
    RenderTransform = new TranslateTransform(
        _dragStartOffset.X + currentPoint.X - _dragStartPoint.X,
        _dragStartOffset.Y + _dragStartPoint.Y);
}

private void DragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    _isDragging = false;
}
```

### 右键拦截 (SimpleWriteTextEditorHandler)

```csharp
protected override void OnPointerReleased(PointerReleasedEventArgs e)
{
    // ... 保留 Ctrl+Click 超链接逻辑 ...

    if (e.InitialPressMouseButton == MouseButton.Right && !e.Handled)
    {
        e.Handled = true;
        CommandPaletteRequested?.Invoke(this, EventArgs.Empty);
        return;
    }

    base.OnPointerReleased(e);
}

/// <summary>请求打开命令浮动面板</summary>
public event EventHandler? CommandPaletteRequested;
```

## 问题与答案

### Q: 是否也需要在 `OnRaisePrepareContextMenuEvent` 里做拦截？

**不需要**，直接删除。`OnRaisePrepareContextMenuEvent` 只在 `TextEditorHandler.OnPointerReleased` 检测到右键时被 `RaisePrepareContextMenuEvent` 调用。`SimpleWriteTextEditorHandler` override 了 `OnPointerReleased` 并在右键时 `return`（不调用 base），所以 `RaisePrepareContextMenuEvent` 根本不会执行。键盘 Context Menu 键走的是 `OnKeyDown`，不经过此链路。因此 `SimpleWriteTextEditor.OnRaisePrepareContextMenuEvent` override 连同构造函数中的 `ContextMenu` 相关代码一起删除即可。

### Q: ScrollViewer 滚动时是否关闭面板？

**不关闭**。面板放在 `ScrollViewer` 外的 `Panel` 层，不受滚动影响。滚动时保持面板可见更符合直觉——用户可能边看文档边选命令。

### Q: 是否考虑面板可拖动？

**是**。面板顶部有拖拽条，用 `TranslateTransform` 实现。拖拽后位置持续到面板关闭，下次打开时重置到自动计算的位置。

## 关键约束

- 浮动面板完全替代右键 `ContextMenu`，不保留旧链路
- `ICommandPattern` 接口不变，命令注册逻辑不变
- 模型相关命令与本地工具型命令继续分开注册
- 编辑器派生命令默认新建会话
- 面板关闭时清理 ViewModel 状态，避免内存泄漏