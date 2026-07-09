# Avalonia 列表保持滚动到底部探索

> 本文记录对 Avalonia 列表滚动到底部行为的源码探索结论，重点讨论 `ListBox` / `ItemsControl` 在新增项、最后一项高度增长、虚拟化和滚动锚点机制下，如何正确实现“保持滚动到底部”。

---

## 目录

1. [核心结论](#1-核心结论)
2. [`ScrollToEnd()` 的语义](#2-scrolltoend-的语义)
3. [Avalonia 的滚动锚点机制](#3-avalonia-的滚动锚点机制)
4. [`ListBox` / `ItemsControl` 是否有内建贴底配置](#4-listbox--itemscontrol-是否有内建贴底配置)
5. [新增 item 时为什么不会自动贴底](#5-新增-item-时为什么不会自动贴底)
6. [最后一项内容变高时为什么不会自动贴底](#6-最后一项内容变高时为什么不会自动贴底)
7. [`ScrollIntoView(lastItem)` 的作用与限制](#7-scrollintoviewlastitem-的作用与限制)
8. [正确实现方式：封装 KeepAtBottom 行为](#8-正确实现方式封装-keepatbottom-行为)
9. [实验建议](#9-实验建议)
10. [总结对照表](#10-总结对照表)

---

## 1. 核心结论

Avalonia 当前没有一个 `ListBox` 内建属性可以表示“持续保持滚动到底部”。没有发现类似以下状态型配置：

- `KeepAtEnd`
- `StickToBottom`
- `AutoScrollToEnd`
- `FollowOutput`
- `AnchorToBottom`

`ScrollViewer.ScrollToEnd()` 只是一次性滚动到底部，不会持续记住“以后内容增长时继续贴底”。

Avalonia 的滚动锚点机制也不是贴底机制。它的目标是保持当前视口中某个可见元素的位置稳定，避免用户正在看的内容因上方内容变化而跳动。

因此，对于聊天窗口、日志窗口、AI 流式回复这类场景，正确做法是封装一个应用层行为或自定义控件，例如 `KeepAtBottom`，由它统一监听集合变化、最后项尺寸变化和布局完成事件，并在用户原本处于底部时执行滚动校正。

---

## 2. `ScrollToEnd()` 的语义

Avalonia 中 `ScrollViewer.ScrollToEnd()` 的语义是：

```csharp
Offset = new Vector(double.NegativeInfinity, double.PositiveInfinity);
```

随后 `Offset` 会被约束到合法范围：

```text
0 <= Offset.Y <= Extent.Height - Viewport.Height
```

因此 `double.PositiveInfinity` 会在当次调用中被约束为当前最大滚动值。

关键点在于：

> `ScrollToEnd()` 保存下来的不是“贴底意图”，而是一个具体的 `Offset` 数值。

例如：

```text
调用 ScrollToEnd 时：
Extent.Height = 1200
Viewport.Height = 400
Offset.Y = 800

之后内容增长：
Extent.Height = 1400
Viewport.Height = 400
新的最大 Offset.Y = 1000
旧 Offset.Y = 800 仍然合法
```

此时 Avalonia 不会自动把 `Offset.Y` 从 `800` 推到 `1000`。

`Offset` 约束逻辑只保证不越界。它会在 `Extent` 缩小时把超出范围的 `Offset` clamp 回来，但不会在 `Extent` 增大时自动贴到新的最大值。

---

## 3. Avalonia 的滚动锚点机制

Avalonia 有滚动锚点相关接口和实现：

| 文件 | 类型 / 成员 | 作用 |
|------|-------------|------|
| `src/Avalonia.Controls/IScrollAnchorProvider.cs` | `IScrollAnchorProvider` | 定义滚动锚点提供者 |
| `src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs` | `RegisterAnchorCandidate` / `CurrentAnchor` | 管理锚点候选和当前锚点 |
| `src/Avalonia.Controls/ScrollViewer.cs` | `IScrollAnchorProvider` 转发实现 | 将锚点相关调用转发给 presenter |
| `src/Avalonia.Controls/VirtualizingStackPanel.cs` | 注册可见元素为锚点候选 | 虚拟化面板把可见 item 注册为候选 |

滚动锚点机制的大致流程：

1. 可见元素被注册为 anchor candidate。
2. `ScrollContentPresenter` 从候选中选择一个锚点。
3. 选择规则倾向于选择距离视口左上角最近的可见元素。
4. 布局后比较锚点元素在内容坐标系中的位置是否变化。
5. 如果锚点位置变化，则调整 `Offset`，让这个锚点在视口中的视觉位置保持稳定。

这个机制主要解决的是：

> 当前视口上方插入、删除或改变高度时，用户正在看的内容不要跳动。

它不是“保持列表底部可见”的机制。

### 为什么锚点不等价于贴底

锚点算法比较的是元素的 `Bounds.Position`，也就是元素顶部位置，而不是元素底部或尺寸。

如果最后一项自己变高，通常变化是：

```text
Top 不变
Height 变大
Bottom 下移
```

锚点算法看到的是：

```text
Position 没变
anchorShift = 0
不调整 Offset
```

即使最后一项刚好被选为锚点，它保持的也是最后一项顶部位置，不是底部位置。

---

## 4. `ListBox` / `ItemsControl` 是否有内建贴底配置

源码探索中未发现 `ListBox` / `ItemsControl` 暴露持续贴底配置。

相关结论：

- `ListBox.Scroll` 只是模板中 `PART_ScrollViewer` 暴露出来的只读 `IScrollable?`。
- `ListBox` 模板只配置滚动条可见性、滚动链、惯性、延迟滚动、`BringIntoViewOnFocusChange` 等。
- `ItemsControl.ScrollIntoView(...)` 是显式调用 API，不会自动监听集合变化或 item 尺寸变化。
- `AutoScrollToSelectedItem` 只在选择项变化时滚动到选中项，不是贴底功能。

`AutoScrollToSelectedItem` 可以间接做到“新增并选中新项后滚过去”，但这会改变选择语义，不适合作为日志窗口或聊天窗口的通用贴底方案。

---

## 5. 新增 item 时为什么不会自动贴底

新增 item 会触发集合变化，列表和虚拟化面板会更新 item 数量、容器和布局。

但 Avalonia 源码中没有这样的逻辑：

```text
如果新增 item 前位于底部：
    新增 item 后自动 Offset = Extent - Viewport
```

新增 item 后：

- `Extent.Height` 可能变大；
- `ScrollBarMaximum` 可能变大；
- 旧 `Offset.Y` 仍然合法；
- `Offset.Y` 不会自动等于新的最大值。

所以如果需要“用户在底部时自动跟随新增内容”，需要应用层或自定义行为监听集合变化并执行贴底校正。

---

## 6. 最后一项内容变高时为什么不会自动贴底

这个场景比新增 item 更容易遗漏。

例如：

- 最后一条日志持续追加文本；
- AI 回复逐字生成；
- Markdown 渲染后高度变大；
- 图片异步加载完成；
- 最后一项展开更多内容。

这些变化通常不会触发 `CollectionChanged`，因为集合没有新增、删除或替换 item。

它们触发的是：

- item ViewModel 属性变化；
- 绑定更新；
- 子控件重新 Measure；
- item 容器 Bounds / DesiredSize 变化；
- ScrollViewer 的 `Extent` 变化。

但如前所述，`Extent` 增大不会自动把 `Offset` 推到新的最大值。

因此只监听集合变化是不够的。最后一项高度增长时，需要额外监听最后项相关的尺寸或布局变化。

---

## 7. `ScrollIntoView(lastItem)` 的作用与限制

`ItemsControl.ScrollIntoView(...)` 的调用链大致是：

```text
ListBox.ScrollIntoView(lastItem)
=> ItemsControl.ScrollIntoView(object)
=> ItemsControl.ScrollIntoView(index)
=> ItemsPresenter.ScrollIntoView(index)
=> VirtualizingStackPanel.ScrollIntoView(index)
=> element.BringIntoView()
=> ScrollContentPresenter.BringDescendantIntoView(...)
```

对于已实现的最后一项，`VirtualizingStackPanel.ScrollIntoView(index)` 通常直接调用：

```text
element.BringIntoView()
```

`BringIntoView` 的语义是 minimal scroll：用最小滚动量让目标进入视口。

因此：

- 如果最后一项高度小于视口，且底部在视口下方，`ScrollIntoView(lastItem)` 通常能让底部重新可见。
- 如果最后一项高度大于视口，它不保证底部贴住视口底部，只保证目标某部分可见。
- 如果内容刚变高但布局尚未完成，调用时可能仍基于旧 Bounds，导致滚动不到新增底部。

所以 `ScrollIntoView(lastItem)` 有帮助，但最好在布局完成后调用。

---

## 8. 正确实现方式：封装 KeepAtBottom 行为

推荐不要在每个添加项或修改内容的位置手写滚动代码，而是封装一个统一行为。

这个行为可以是：

- attached property；
- behavior；
- 自定义 `ListBox`；
- 自定义 `AutoScrollListBox`；
- 项目内的 `KeepAtBottom` 辅助类。

它内部需要维护两个核心状态：

```text
IsUserAtBottom
PendingKeepBottom
```

推荐流程：

```text
当 ScrollViewer 滚动状态变化：
    根据 Offset / Extent / Viewport 更新 IsUserAtBottom

当集合变化：
    如果 IsUserAtBottom：
        PendingKeepBottom = true
        请求布局后贴底

当最后一项属性变化或尺寸变化：
    如果 IsUserAtBottom：
        PendingKeepBottom = true
        请求布局后贴底

布局完成后：
    如果 PendingKeepBottom：
        滚到底部
        PendingKeepBottom = false
```

判断是否接近底部可以使用阈值：

```csharp
private static bool IsNearBottom(IScrollable scroll)
{
    const double threshold = 2;
    var maxOffset = Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height);
    return scroll.Offset.Y >= maxOffset - threshold;
}
```

贴底校正可以组合使用：

1. 对最后项调用 `ScrollIntoView(lastItem)`；
2. 如果可以安全拿到内部 `ScrollViewer` 或 `IScrollable`，设置到最大偏移：

```csharp
var maxOffset = Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height);
scroll.Offset = scroll.Offset.WithY(maxOffset);
```

对于最后项高度增长，尤其需要在布局完成后再执行一次：

```text
内容变化时：
    标记 PendingKeepBottom

下一次 LayoutUpdated 或 Dispatcher Loaded 优先级：
    根据新的 Extent / Bounds 校正到底部
```

---

## 9. 实验建议

建议在实验控件中观察以下场景：

### 场景一：新增 item

1. 滚动到底部。
2. 添加一条新 item。
3. 观察 `Extent` 变大。
4. 观察旧 `Offset` 是否自动变为新最大值。

预期：不会自动贴底，需要行为主动校正。

### 场景二：最后一项内容增长

1. 滚动到底部。
2. 不新增 item，只追加最后一项文本。
3. 观察最后一项高度变大。
4. 观察 `Extent` 变大。
5. 观察底部新增内容是否仍可见。

预期：不会稳定自动贴底，需要监听最后项尺寸变化并布局后校正。

### 场景三：用户上翻历史内容

1. 用户滚动到列表中间。
2. 新增 item 或增长最后项。
3. 行为不应该强制把用户拉回底部。

预期：只有用户原本接近底部时才自动贴底。

### 场景四：最后一项高度大于视口

1. 让最后一项内容非常高。
2. 调用 `ScrollIntoView(lastItem)`。
3. 观察它是否保证底部贴底。

预期：`ScrollIntoView` 是 minimal scroll，不一定等价于底部贴底。

---

## 10. 总结对照表

| 问题 | Avalonia 源码结论 |
|------|------------------|
| `ScrollToEnd()` 是否持续贴底？ | 否，只是一次性设置 Offset |
| `Extent` 增大时 Offset 是否自动变成新最大值？ | 否，旧 Offset 仍合法则保持不变 |
| 滚动锚点是否能保持底部？ | 否，它保持锚点顶部位置稳定 |
| `ListBox` 是否有 `KeepAtBottom` 类属性？ | 未发现 |
| `AutoScrollToSelectedItem` 是否等价于贴底？ | 否，只跟随选择变化 |
| 新增 item 是否自动贴底？ | 否，需要应用层行为 |
| 最后一项变高是否触发集合变化？ | 通常不会 |
| `ScrollIntoView(lastItem)` 是否有帮助？ | 有帮助，但不是完整贴底语义 |
| 最后一项变高时最可靠做法 | 监听尺寸/布局变化，布局完成后校正 Offset |
| 推荐封装方式 | Attached Property / Behavior / 自定义 ListBox |

---

## 最终结论

Avalonia 中实现“保持滚动到底部”的关键，不是依赖 `ScrollToEnd()` 的持久性，也不是依赖滚动锚点机制，而是封装一个明确的 `KeepAtBottom` 行为。

这个行为需要同时处理两类变化：

1. 集合变化：新增、删除、重置 item；
2. 尺寸变化：最后一项内容增长、图片加载、展开折叠、文本换行等。

正确语义应该是：

> 如果用户原本就在底部，则内容变化后继续保持底部；如果用户已经上翻查看历史内容，则不要强行拉回底部。

对于末尾项内容自动撑大的场景，最重要的是：

> 在最后项高度变化并完成布局后，重新把 `Offset.Y` 校正到新的 `Extent.Height - Viewport.Height`。
