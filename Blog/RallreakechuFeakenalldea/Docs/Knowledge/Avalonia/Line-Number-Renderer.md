# 编辑器行号渲染

## 场景

为 `MainEditorView` 中的 `TextEditor` 增加行号显示。要求：

- 性能高：滚动时帧率不掉，只绘制可见行号。
- 内存低：不为全文档行号预创建对象，只缓存当前可见范围的绘制资源。
- 与文本渲染解耦：行号不参与文本布局，不影响文本渲染管线。
- 正确处理跨行：一个段落因自动换行产生多行时，只在段落首行显示行号。

## 核心决策

行号与文本内容相互独立。两者之间唯一的依赖是：

```
文本布局完成 → 行号读取布局结果（每行的 Y 坐标和高度）→ 行号独立绘制
```

不修改 LightTextEditorPlus。`TextEditor` 已经开放了 `protected virtual TextRect? GetViewport()`，子类重写即可解决 `ScrollViewer` 查找问题。行号控件放在 `ScrollViewer` 内部，与 `TextEditor` 同级在 `Grid` 中，享受天然滚动同步。

## 架构

行号控件与 `TextEditor` 放在同一个 `ScrollViewer` 内的 `Grid` 中，垂直滚动天然同步。`SimpleWriteTextEditor` 继承 `TextEditor` 并重写 `GetViewport()`，用 `FindAncestor<ScrollViewer>()` 查找祖先 `ScrollViewer`，绕过 `UpdateInScrollViewer` 中 `Parent as ScrollViewer` 返回 null 的问题。

```
MainEditorView.axaml
  └─ Border (TextEditorBorder)
       └─ ScrollViewer (TextEditorScrollViewer)
            └─ Grid
                 ├─ Column 0: LineNumberControl (固定宽度)
                 └─ Column 1: SimpleWriteTextEditor (填充剩余空间)
```

### 为什么不改 UpdateInScrollViewer

`UpdateInScrollViewer` 是 `private` 方法，改为 `protected virtual` 还需要把 `_containerScrollViewer` 字段也改为 `protected`，改动面较大。而 `GetViewport()` 已经是 `protected virtual`，且是 `_containerScrollViewer` 的唯一消费方——重写 `GetViewport()` 后，`_containerScrollViewer` 是否为 null 就不再重要。

### SimpleWriteTextEditor 重写

```csharp
protected override TextRect? GetViewport()
{
    // Parent 是 Grid，需要往上找 ScrollViewer
    ScrollViewer? scrollViewer = this.FindAncestor<ScrollViewer>();
    if (scrollViewer is null)
    {
        return null;
    }

    double offsetX = scrollViewer.Offset.X;
    double offsetY = scrollViewer.Offset.Y;
    double viewportWidth = scrollViewer.Viewport.Width;
    double viewportHeight = scrollViewer.Viewport.Height;
    return new TextRect(offsetX, offsetY, viewportWidth, viewportHeight);
}
```

不改动 LightTextEditorPlus 任何代码。

### 数据流

```
TextEditor.LayoutCompleted (事件)
  → LineNumberControl.InvalidateVisual()

LineNumberControl.Render(DrawingContext)
  → TextEditor.TextEditorCore.TryGetRenderInfo(out renderInfo) 获取 RenderInfoProvider
  → 遍历 ParagraphRenderInfoList，取每段首行的 OutlineBounds
  → 只绘制可见范围（行号与文本在同一个 ScrollViewer 内，天然同步滚动）
```

### 可选增强：渲染变更通知

当前 `LayoutCompleted` 事件已经足够驱动行号更新。如果后续遇到 `LayoutCompleted` 触发频率与 `Render` 不对齐的问题（如纯光标闪烁导致的重绘不触发 `LayoutCompleted`），可选配合路线 A：

- 继承 `TextEditor` 重写 `protected virtual void Render(in AvaloniaTextEditorDrawingContext context)`。
- 在 `base.Render` 之后调用行号控件的 `InvalidateVisual`。

这是可选项，当前阶段不需要。

## 跨行处理

`ParagraphLineRenderInfo` 提供以下关键属性：

| 属性 | 说明 |
|---|---|
| `IsFirstLine` | 是否为段落首行（`LineIndex == 0`） |
| `ParagraphIndex` | 段落序号，从 0 开始 |
| `OutlineBounds` | 行的外接范围（`TextRect`，含 Y 坐标和高度） |
| `ContentBounds` | 行的内容范围 |

行号 = 段落序号 + 1。遍历 `GetParagraphRenderInfoList()` 时，每个段落取 `IsFirstLine == true` 的行，用其 `OutlineBounds.Y` 作为行号绘制位置。续行（`IsFirstLine == false`）不显示行号，可留空或显示 `·` 等续行符号。

## 实现约定

### LineNumberControl

继承 `Control`，职责：

1. 持有对 `TextEditor` 的引用。
2. 监听 `TextEditor.LayoutCompleted` 事件，触发 `InvalidateVisual`。
3. 在 `Render` 中通过 `RenderInfoProvider` 获取行布局信息，只绘制可见范围。不需要监听滚动事件——行号控件与 `TextEditor` 在同一个 `ScrollViewer` 内，`ScrollViewer` 滚动时 `Grid` 的内容整体移动，行号控件作为 `Grid` 的子级自动跟随。

```csharp
public class LineNumberControl : Control
{
    private TextEditor? _textEditor;

    // 行号区域固定宽度
    public static readonly StyledProperty<double> LineNumberWidthProperty =
        AvaloniaProperty.Register<LineNumberControl, double>(nameof(LineNumberWidth), 48);

    // 续行符号，null 表示不显示
    public static readonly StyledProperty<string?> ContinuationSymbolProperty =
        AvaloniaProperty.Register<LineNumberControl, string?>(nameof(ContinuationSymbol), "·");

    // 行号字体大小，默认跟随文本
    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<LineNumberControl, double>(nameof(FontSize), 14);

    // 行号颜色
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<LineNumberControl, IBrush?>(nameof(Foreground));

    // 行号背景色
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<LineNumberControl, IBrush?>(nameof(Background));

    internal void Attach(TextEditor textEditor)
    {
        _textEditor = textEditor;
        textEditor.LayoutCompleted += OnLayoutCompleted;
    }

    internal void Detach()
    {
        if (_textEditor is not null)
        {
            _textEditor.LayoutCompleted -= OnLayoutCompleted;
            _textEditor = null;
        }
    }

    private void OnLayoutCompleted(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        // 1. 画背景
        // 2. 获取 RenderInfoProvider，遍历段落
        // 3. 只画可见范围
        // 4. 段落首行画行号，续行画 ContinuationSymbol
    }
}
```

### 绘制逻辑

`Render` 方法核心步骤：

1. **画背景**：用 `Background` 填充控件整个区域。
2. **获取布局信息**：`_textEditor.TextEditorCore.TryGetRenderInfo(out var renderInfo)`。如果获取失败（文本是脏的），直接返回，等 `LayoutCompleted` 后自动重绘。
3. **计算可见范围**：行号控件与 `TextEditor` 在同一个 `ScrollViewer` 内，`ScrollViewer` 滚动时行号控件的内容区域也整体偏移。可见范围由控件自身的 `Bounds` 决定，`Avalonia` 渲染管线会自动裁剪超出范围的内容。但由于行号控件高度等于 `TextEditor` 的内容高度（可能远大于可见区域），需要在绘制时做手动裁剪优化：从 `ScrollViewer` 读取 `Offset.Y` 和 `Viewport.Height`，得到 `[viewportTop, viewportBottom]`。
4. **遍历段落**：`renderInfo.GetParagraphRenderInfoList()` 逐段遍历。对每个段落，取首行 `OutlineBounds`：
   - 若 `OutlineBounds.Bottom < viewportTop`，跳过（在可见区域上方）。
   - 若 `OutlineBounds.Top > viewportBottom`，终止遍历（已超出可见区域下方）。
   - 否则在 `OutlineBounds` 的垂直中心位置绘制行号文本。
5. **行号文本**：段落序号 + 1，右对齐到行号区域右边缘，留 8px 右间距。续行在相同位置绘制 `ContinuationSymbol`。
6. **文本缓存**：使用 `FormattedText` 绘制行号。由于可见行数通常 30-50 行，不缓存 `FormattedText` 对象，每次创建——开销可忽略。如果后续需要优化，可以按行号做 `Dictionary<int, FormattedText>` 缓存，容量限制为 64。

### 布局约定

- `LineNumberControl` 宽度固定为 `LineNumberWidth`（默认 48px），放在 `ScrollViewer` 内部 `Grid` 的 `Column="0"`。
- `SimpleWriteTextEditor` 放在 `Grid` 的 `Column="1"`。
- `Grid` 的高度由 `SimpleWriteTextEditor` 的 `DesiredSize` 决定（`SizeToContent = Height`），`LineNumberControl` 的高度与 `TextEditor` 一致，因此垂直滚动范围一致。
- 行号控件和 `TextEditor` 都在 `ScrollViewer` 内部，`ScrollViewer` 滚动时两者同步移动。

### MainEditorView 改造

当前 XAML：

```xml
<Border x:Name="TextEditorBorder">
    <ScrollViewer x:Name="TextEditorScrollViewer"
                  HorizontalScrollBarVisibility="Disabled"
                  VerticalScrollBarVisibility="Visible"
                  Padding="10,0,20,0">
    </ScrollViewer>
</Border>
```

改造后：

```xml
<Border x:Name="TextEditorBorder">
    <ScrollViewer x:Name="TextEditorScrollViewer"
                  HorizontalScrollBarVisibility="Disabled"
                  VerticalScrollBarVisibility="Visible"
                  Padding="10,0,20,0">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <controls:LineNumberControl x:Name="LineNumberControl"
                                        Grid.Column="0"
                                        LineNumberWidth="48"/>
            <!-- TextEditor 在代码中动态设置到 Grid.Column="1" -->
        </Grid>
    </ScrollViewer>
</Border>
```

`MainEditorView.axaml.cs` 中 `CurrentTextEditor` 的 setter 需要调整：

- `TextEditorScrollViewer.Content` 改为设置到 `Grid` 的 `Column="1"`（或先取到 `Grid` 再 `Grid.Children.Add`）。
- `LineNumberControl.Attach(textEditor)` 建立关联。

切换编辑器时需要先 `Detach` 旧的关联（取消事件订阅），再 `Attach` 新的。

### 行号开关

`EditorViewModel` 新增 `IsLineNumberVisible` 属性，绑定到 `LineNumberControl.IsVisible`。切换时无需重建编辑器。

## 性能分析

| 场景 | 行为 | 开销 |
|---|---|---|
| 打开 10 万行文档 | `LayoutCompleted` 触发一次行号重绘 | 遍历段落直到超出可见范围，约 30-50 次迭代 |
| 滚动 | `ScrollViewer` 帧驱动行号重绘 | 只绘制可见行号，约 30-50 次 `DrawText` |
| 输入文字 | `LayoutCompleted` 触发行号重绘 | 同上 |
| 光标闪烁 | 不触发 `LayoutCompleted`，行号不重绘 | 零开销 |

### 内存

- `LineNumberControl` 本身不缓存行号列表。每次 `Render` 从 `RenderInfoProvider` 实时遍历。
- 如需缓存，按行号缓存 `FormattedText`，容量 64 个，每个约 200 字节，总计约 12KB。
- 不存储全文档行号位置列表，内存占用与文档行数无关。

## 关键文件

| 文件 | 职责 |
|---|---|
| `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs` | 重写 `GetViewport()`，用 `FindAncestor<ScrollViewer>` 查找祖先 |
| `SimpleWrite/Views/Components/LineNumberControl.cs` | 行号控件，继承 `Control`，负责绘制行号 |
| `SimpleWrite/Views/Components/MainEditorView.axaml` | 将 `ScrollViewer` 内容改为 `Grid`，行号在 `Column="0"` |
| `SimpleWrite/Views/Components/MainEditorView.axaml.cs` | 在 `CurrentTextEditor` setter 中调用 `LineNumberControl.Attach/Detach` |
| `SimpleWrite/ViewModels/EditorViewModel.cs` | 新增 `IsLineNumberVisible` 属性 |

## 扩展指南

- 行号宽度自适应：监听段落数量变化，动态计算最大行号位数，调整 `LineNumberWidth`。
- 当前行高亮：在 `Render` 中根据 `TextEditor.CurrentCaretOffset` 获取当前段落序号，高亮对应行号。
- 行号右键菜单：在 `LineNumberControl` 上注册 `ContextMenu`，通过坐标反查段落序号。
- 多编辑器实例：`LineNumberControl` 通过 `Attach`/`Detach` 方法与 `TextEditor` 一一绑定，切换标签页时先 `Detach` 旧的，再 `Attach` 新的。

## 适用场景

- 代码编辑器需要显示行号
- 长文档编辑需要快速定位行位置
- 需要与 LightTextEditorPlus 渲染管线保持独立的行号显示
