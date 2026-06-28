# RunProperty 导出能力分析与补全建议

> 基于 `SkiaTextRunProperty` 继承链全量属性，对照 `NativeTextEditorRunPropertyExporter` 当前已导出方法，找出缺口与后续导出方向。

---

## 1. SkiaTextRunProperty 属性全览

### 1.1 继承链

```
IReadOnlyRunProperty (接口)
  └─ LayoutOnlyRunProperty (record)
       └─ SkiaTextRunProperty (record)
```

### 1.2 属性清单

| 属性 | 类型 | 来源 | 可写性 | 公开性 |
|---|---|---|---|---|
| `FontSize` | `double` | IReadOnlyRunProperty / LayoutOnlyRunProperty | `init` | public |
| `FontName` | `FontName` | IReadOnlyRunProperty / LayoutOnlyRunProperty → SkiaTextRunProperty (override) | `init` | public |
| `FontVariant` | `TextFontVariant` | IReadOnlyRunProperty / LayoutOnlyRunProperty | `init` | public |
| `IsInvalidRunProperty` | `bool` | IReadOnlyRunProperty / SkiaTextRunProperty (override) | get-only | public |
| `Opacity` | `double` | SkiaTextRunProperty | `init` | public |
| `Foreground` | `SkiaTextBrush` | SkiaTextRunProperty | `init` | public |
| `Background` | `SKColor` | SkiaTextRunProperty | `init` | public |
| `Stretch` | `SKFontStyleWidth` | SkiaTextRunProperty | `init` | public |
| `FontWeight` | `SKFontStyleWeight` | SkiaTextRunProperty | `init` | public |
| `FontStyle` | `SKFontStyleSlant` | SkiaTextRunProperty | `init` | public |
| `IsBold` | `bool` | SkiaTextRunProperty | `init` | public (Obsolete) |
| `IsItalic` | `bool` | SkiaTextRunProperty | `init` | public (Obsolete) |
| `DecorationCollection` | `TextEditorImmutableDecorationCollection` | SkiaTextRunProperty | `init` | public |
| `IsMissRenderFont` | `bool` | SkiaTextRunProperty | `init` | internal |
| `RenderFontName` | `string` | SkiaTextRunProperty | `init` | internal |
| `ResourceManager` | `SkiaPlatformResourceManager` | SkiaTextRunProperty | `init` | internal |

### 1.3 FontVariant 结构

`TextFontVariant` 是 `readonly record struct`，包含：

| 字段 | 类型 | 说明 |
|---|---|---|
| `FontVariants` | `TextFontVariants` (byte 枚举) | Normal=0, Superscript=1, Subscript=2 |
| `BaselineProportion` | `double` | 上下标基线比例，默认 0.3 |

### 1.4 Foreground 画刷类型体系

```
SkiaTextBrush (abstract record)
  ├─ SolidColorSkiaTextBrush (sealed record)
  │    └─ Color: SKColor
  └─ LinearGradientSkiaTextBrush (sealed record)
       ├─ StartPoint: GradientSkiaTextBrushRelativePoint (X, Y, Unit)
       ├─ EndPoint: GradientSkiaTextBrushRelativePoint
       ├─ Opacity: double
       └─ GradientStops: SkiaTextGradientStopCollection
            └─ SkiaTextGradientStop (Color: SKColor, Offset: float)
```

### 1.5 装饰类型

`TextEditorDecorations` 静态类预设装饰（Skia 平台可用）：

| 装饰 | 类型 | Native 位标志 |
|---|---|---|
| `Underline` | `UnderlineTextEditorDecoration` | `1 << 0` |
| `Strikethrough` | `StrikethroughTextEditorDecoration` | `1 << 1` |
| `EmphasisDots` | `EmphasisDotsTextEditorDecoration` | `1 << 2` |

> `WaveLine` 仅在 `USE_WPF` 条件下可用，Skia 平台不涉及。

---

## 2. 当前已导出方法清单

### 2.1 生命周期

| EntryPoint | 签名 | 说明 |
|---|---|---|
| `CreateRunPropertyFromStyle` | `int (int textEditorId)` | 从文本编辑器样式创建 |
| `FreeRunProperty` | `int (int runPropertyId)` | 释放 |

### 2.2 Set 方法

| EntryPoint | 参数 | 对应属性 |
|---|---|---|
| `SetRunPropertyFontName` | `int, IntPtr, int` (ptr+count) | `FontName` |
| `SetRunPropertyFontSize` | `int, double` | `FontSize` |
| `SetRunPropertyOpacity` | `int, double` | `Opacity` |
| `SetRunPropertyForegroundColor` | `int, byte×4` (a,r,g,b) | `Foreground` (仅纯色) |
| `SetRunPropertyBackgroundColor` | `int, byte×4` | `Background` |
| `SetRunPropertyStretch` | `int, int` | `Stretch` |
| `SetRunPropertyFontWeight` | `int, int` | `FontWeight` |
| `SetRunPropertyFontStyle` | `int, int` | `FontStyle` |
| `SetRunPropertyIsBold` | `int, int` | `IsBold` (Obsolete) |
| `SetRunPropertyIsItalic` | `int, int` | `IsItalic` (Obsolete) |
| `SetRunPropertyFontVariant` | `int, byte, double` | `FontVariant` |
| `SetRunPropertyDecorationFlags` | `int, int` (位标志) | `DecorationCollection` |

### 2.3 Get 方法

| EntryPoint | 输出参数 | 对应属性 |
|---|---|---|
| `GetRunPropertyFontName` | `IntPtr, int, int*` (两段式) | `FontName` |
| `GetRunPropertyFontSize` | `double*` | `FontSize` |
| `GetRunPropertyOpacity` | `double*` | `Opacity` |
| `GetRunPropertyForegroundColor` | `byte×4 *` | `Foreground` (仅纯色) |
| `GetRunPropertyBackgroundColor` | `byte×4 *` | `Background` |
| `GetRunPropertyStretch` | `int*` | `Stretch` |
| `GetRunPropertyFontWeight` | `int*` | `FontWeight` |
| `GetRunPropertyFontStyle` | `int*` | `FontStyle` |
| `GetRunPropertyIsBold` | `int*` | `IsBold` (Obsolete) |
| `GetRunPropertyIsItalic` | `int*` | `IsItalic` (Obsolete) |
| `GetRunPropertyFontVariant` | `byte*, double*` | `FontVariant` |
| `GetRunPropertyDecorationFlags` | `int*` | `DecorationCollection` |

---

## 3. 缺口分析

### 3.1 未导出的公开属性

| 属性 | 类型 | 缺失方向 | 优先级 | 原因 |
|---|---|---|---|---|
| `IsInvalidRunProperty` | `bool` | Get | 中 | 公开属性，表示字符属性是否无效（如字体回退失败），调用方可能需要据此做容错 |

### 3.2 已导出但能力不完整

#### 3.2.1 Foreground 仅支持纯色（高优先级）

**现状**：`SetRunPropertyForegroundColor` 只能设置 `SolidColorSkiaTextBrush`；`GetRunPropertyForegroundColor` 通过 `AsSolidColor()` 读取，渐变色会丢失信息。

**缺失**：

| 建议导出 | 说明 |
|---|---|
| `GetRunPropertyForegroundBrushType` | 返回画刷类型（0=Solid, 1=LinearGradient），让调用方知道应使用哪种读取方式 |
| `SetRunPropertyForegroundToLinearGradient` | 设置线性渐变前景色，参数包括起止点坐标、刻度点数量、各刻度点的颜色和偏移 |
| `GetRunPropertyLinearGradientForeground` | 获取线性渐变前景色详情（起止点、刻度点） |

**Native 协议建议**：

```
// 画刷类型枚举
// 0 = Solid (纯色)
// 1 = LinearGradient (线性渐变)

GetRunPropertyForegroundBrushType(int runPropertyId, int* brushType) -> int

// 设置线性渐变前景色
// startStopCount: 渐变刻度点数量
// 后续通过多次调用 SetRunPropertyGradientStop 逐个设置刻度点
SetRunPropertyForegroundToLinearGradient
    (int runPropertyId,
     float startX, float startY, byte startUnit,
     float endX, float endY, byte endUnit,
     double opacity,
     int stopCount) -> int

// 设置渐变刻度点（需先设置 LinearGradient 类型）
SetRunPropertyGradientStop
    (int runPropertyId, int index,
     byte alpha, byte red, byte green, byte blue, float offset) -> int

// 获取线性渐变前景色信息
GetRunPropertyLinearGradientForegroundInfo
    (int runPropertyId,
     float* startX, float* startY, byte* startUnit,
     float* endX, float* endY, byte* endUnit,
     double* opacity, int* stopCount) -> int

// 获取渐变刻度点
GetRunPropertyGradientStop
    (int runPropertyId, int index,
     byte* alpha, byte* red, byte* green, byte* blue, float* offset) -> int
```

#### 3.2.2 缺少 Clone 能力（中优先级）

**现状**：调用方若需基于一个已有 RunProperty 创建变体，必须逐个属性读取再设置到新创建的对象上，效率低且易遗漏。

**建议导出**：

| 方法 | 说明 |
|---|---|
| `CloneRunProperty` | 从已有 RunProperty 克隆出新的独立副本，返回新 Id |

**Native 协议建议**：

```
CloneRunProperty(int sourceRunPropertyId) -> int  // 返回新的 runPropertyId 或负数错误码
```

### 3.3 不需要导出的 internal 属性

| 属性 | 原因 |
|---|---|
| `IsMissRenderFont` | internal，属于渲染层内部状态 |
| `RenderFontName` | internal，字体回退后的实际渲染字体名，非用户设置 |
| `ResourceManager` | internal，平台资源管理器，不可跨边界传递 |

### 3.4 装饰集合能力评估

当前 3 个位标志（Underline / Strikethrough / EmphasisDots）已覆盖 Skia 平台全部预设装饰。无需扩展。

`TextEditorImmutableDecorationCollection` 还支持 `Remove` 和自定义 `TextEditorDecoration` 子类，但当前 Native 协议采用位标志模式，不支持自定义装饰类型。如未来需要，可考虑扩展为"装饰列表"协议（传入装饰类型枚举 + 参数），但当前无实际需求。

---

## 4. 总结对照表

| 属性 | Set | Get | 备注 |
|---|---|---|---|
| `FontName` | ✅ | ✅ | 两段式字符串 |
| `FontSize` | ✅ | ✅ | |
| `FontVariant` | ✅ | ✅ | byte + double |
| `IsInvalidRunProperty` | — | ❌ **待补** | get-only，无法 Set |
| `Opacity` | ✅ | ✅ | |
| `Foreground` (Solid) | ✅ | ✅ | 仅纯色 |
| `Foreground` (Gradient) | ❌ **待补** | ❌ **待补** | 渐变画刷 |
| `Background` | ✅ | ✅ | |
| `Stretch` | ✅ | ✅ | |
| `FontWeight` | ✅ | ✅ | |
| `FontStyle` | ✅ | ✅ | |
| `IsBold` | ✅ | ✅ | Obsolete |
| `IsItalic` | ✅ | ✅ | Obsolete |
| `DecorationCollection` | ✅ | ✅ | 位标志 |
| Clone | ❌ **待补** | — | 便捷复制 |

---

## 5. 后续导出优先级

| 优先级 | 导出项 | 理由 |
|---|---|---|
| P0 | `GetRunPropertyIsInvalidRunProperty` | 简单 get-only 属性，导出成本低 |
| P0 | `CloneRunProperty` | 单方法，显著提升调用方体验 |
| P1 | `GetRunPropertyForegroundBrushType` | 区分纯色/渐变，是渐变支持的前置 |
| P1 | `SetRunPropertyForegroundToLinearGradient` + `SetRunPropertyGradientStop` | 渐变前景色设置 |
| P1 | `GetRunPropertyLinearGradientForegroundInfo` + `GetRunPropertyGradientStop` | 渐变前景色读取 |
