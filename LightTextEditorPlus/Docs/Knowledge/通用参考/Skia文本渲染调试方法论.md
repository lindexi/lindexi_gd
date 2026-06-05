# Skia 文本渲染调试方法论

> 从任务日志 `2025-05-29-Skia渲染Y坐标定位与修复` 和 `领域与技术参考.md` S1 提炼。

---

## 文本渲染问题的分层诊断策略

当 Skia（或任何文本引擎）渲染出现坐标异常时，不要猜测根因，**按管线逐层输出数据**：

```
原始字体度量 -> 字符测量 -> 行布局 -> 最终绘制 -> 选择/光标范围
```

### 第 1 层：原始字体度量

先用 `SKFontMetrics`、`MeasureText`、`GetGlyphWidths` 单独对目标字体做探针输出。

关键数据：`Ascent`、`Descent`、`Top`、`Bottom`、`LineHeight`、`Leading`。

### 第 2 层：字符测量

检查 `CharInfoMeasurer` 输出的 `FrameSize`、`FaceSize`、`Baseline`、`GlyphIndex`。与第 1 层原始度量对比，确认测量层是否做了意外变换。

### 第 3 层：行布局

检查 `LineLayoutData`：`CharStartPoint`、`LineContentStartPoint`、`LineCharTextSize`、`LineContentSize`、`LineSpacingThickness`。

**关键检查项**：行高是否小于当前行中字符的最大实际高度？若 `LineContentSize.Height < 最大字符高度`，`LineSpacingThickness.Top` 为负，整行上移。

### 第 4 层：最终绘制

检查渲染器实际使用的绘制点、基线偏移是否与前几层一致。

### 第 5 层：选择/光标范围

检查 `GetSelectionBoundsList` 和 `GetCaretBounds`。

---

## 典型案例：行高小于字符高度

| 项 | 内容 |
|---|---|
| 现象 | 字符和选择范围 Y 坐标为负或偏小 |
| 根因 | 布局层 `LineContentSize.Height < 字符实际高度`，`LineSpacingThickness.Top` 为负 |
| 修复 | 行布局阶段保证 `行高 >= max(字符实际高度)` |
| 选择范围修复 | 采用几何求交（`GetLineContentBounds()` 与选择字符范围），而非仅限制高度 |

---

## 调试输出策略

- 诊断输出应**保留为可复用探针**，修复后不删除
- 为诊断输出增加统一调试开关（`TraceSwitch` 或编译条件）
- 在 Demo 项目中保留字体探针和布局探针入口

## 适用场景

同样的分层诊断方法适用于：竖排渲染异常、字体回退偏差、复杂脚本（阿拉伯文/天城文）shape 异常等。
