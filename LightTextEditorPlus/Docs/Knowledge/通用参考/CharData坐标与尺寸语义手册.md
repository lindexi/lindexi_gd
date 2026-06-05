# CharData 坐标与尺寸语义手册

> 从任务日志 `2026-03-24-竖排光标范围与行字符范围说明` 提炼。

---

## 核心概念

文本引擎中存在两类尺寸概念，不可混用：

| 概念 | 属性 | 含义 | 使用场景 |
|---|---|---|---|
| **FrameSize** (`CharData.Size`) | FrameSize | 字符布局框尺寸（外框） | 排版推进、光标、选择、换行 |
| **FaceSize** | FaceSize | 字面实际着墨区域（字墨） | 文本装饰、精细渲染修正 |

`FaceSize <= FrameSize` 始终成立。

---

## `GetStartPoint()` 的方向语义

### 横排

- 直接表示字符框左上角
- `GetStartPoint() + Size` = 字符框右下角

### 右到左竖排

- `GetStartPoint()` 更接近"列锚点"，不能当作可视矩形左上角
- 实际可视宽高需交换 `Size.Width` 和 `Size.Height` 语义
- 可视宽 = `Size.Height`，可视高 = `Size.Width`

### 左到右竖排（蒙古文）

- 列锚点方向与右到左竖排相反

---

## 行范围的两个层次

| 方法 | 含义 | 使用场景 |
|---|---|---|
| `GetLineContentBounds()` | 行内容带（字符实际区域） | 光标、选择、字符绘制 |
| `OutlineBounds` | 行外接带（含行距边距） | 空白点击命中、段落边界判断 |

两者不一致时，内容带可能小于外接带（行距为负时）或大于（行距为正时）。

---

## 竖排测量宽高交换

横排下 `FrameSize = (宽度, 高度)`。竖排测量时，需交换语义：

- 竖排 `Size.Height` = 横排的推进宽度
- 竖排 `Size.Width` = 横排的渲染高度近似值

这是为了在 Core 层保持统一的横排坐标语义存储，避免竖排分支重写所有布局逻辑。

---

## 常见误用

| 误用 | 正确做法 |
|---|---|
| 竖排直接用 `GetStartPoint() + Size` 作为可视矩形 | 需交换宽高语义后使用 |
| 用 `OutlineBounds` 做字符级判断 | 使用 `GetLineContentBounds()` |
| 用 `FaceSize` 做布局推进 | 使用 `FrameSize` |
