# SlideML 主题化后续思考

## 背景

在讨论 SlideML 多页面课件、多作者或多 AI 实例并行制作的问题时，我们发现绝对字号、绝对颜色、绝对间距等写法会带来视觉不统一的问题。

当前已决定先在 V2 / V3 规范中加入 **字号等级机制**：

```xml
<TextElement Text="Unit 3 Summary" FontSize="L2" />
<TextElement Text="Do you have a boat?" FontSize="L3" />
```

本文件记录讨论过但暂未加入正式规范的主题化方向，作为后续演进参考。

---

## 当前已加入规范的内容

### 字号等级

已加入：

```text
L1 / L2 / L3 / L4 / L5
```

默认定义：

| 等级 | 倍数 | 默认用途 |
|------|------:|----------|
| `L1` | `1.67` | 封面主标题、章节大标题 |
| `L2` | `1.17` | 页面标题、重点标题 |
| `L3` | `1.00` | 正文、对话、句型 |
| `L4` | `0.83` | 辅助文字、词汇、注释 |
| `L5` | `0.67` | 微文字、页码、角标 |

基准值：

```text
BaseCanvas = 1280 × 720
BaseLevel = L3
BaseFontSize = 48px
```

---

## 暂未加入规范的内容

### 1. 主题色 Token

曾讨论是否允许颜色属性使用主题色 token，例如：

```xml
<Page Background="Background">
<Rect Fill="Card" Stroke="BorderLight" />
<TextElement Foreground="TextPrimary" />
```

可能的主题色包括：

| Token | 用途 |
|-------|------|
| `Background` | 页面背景 |
| `Surface` | 浅色内容区域背景 |
| `Card` | 卡片背景 |
| `Primary` | 主色 |
| `PrimaryLight` | 主色浅色背景 |
| `Accent` | 强调色 |
| `TextPrimary` | 主文本 |
| `TextSecondary` | 次级文本 |
| `TextMuted` | 弱化文本 |
| `BorderLight` | 浅描边 |
| `BorderStrong` | 强描边 |
| `OnPrimary` | 主色背景上的文字 |

示例：

```xml
<Rect Id="card" Fill="Card" Stroke="BorderLight" />
<TextElement Id="title" Foreground="TextPrimary" />
```

暂不加入原因：

1. 需要定义较多默认颜色 token，容易让规范膨胀。
2. 颜色 token 的命名需要和具体业务、课件风格绑定，通用规范很难一次定准。
3. 颜色属性分布较广，包括 `Background`、`Fill`、`Stroke`、`Foreground`、`Stop.Color`、`Shadow.Color` 等，加入后会影响范围较大。
4. 对 LLM 来说，过多颜色 token 可能增加选择负担。

后续若加入，建议单独设计轻量主题色规范，并明确：

- 是否提供内置默认主题色表；
- 未知颜色 token 是 Warning 还是 Error；
- 主题色定义放在课件级元数据、业务层配置，还是 SlideML 文档内部；
- 渐变和阴影中的颜色 token 如何解析。

---

### 2. 字体名 / 字体族 Token

曾讨论是否让字体也支持类似 token：

```xml
<TextElement FontFamily="Title" />
<TextElement FontFamily="Body" />
```

主题中再定义：

```xml
<FontFamily Name="Title" Value="Microsoft YaHei, PingFang SC, Arial" />
<FontFamily Name="Body" Value="Microsoft YaHei, PingFang SC, Arial" />
```

暂不加入原因：

1. `FontName="Body"` 容易和真实字体名混淆。
2. 不同操作系统和渲染后端的字体可用性不同。
3. 当前 SlideML 已有字体回退机制，字体 token 的收益不如字号明显。
4. 需要额外设计 `FontName`、`FontFamily`、真实字体列表、fallback 的优先级关系。

后续若加入，建议不要复用 `FontName` 表达 token，而是考虑新增独立属性：

```xml
<TextElement FontFamily="Body" />
```

并规定：

```text
FontFamily 命中主题字体族时使用主题字体族；
FontName 保持真实字体名语义；
FontFamily 与 FontName 同时存在时，需要定义优先级。
```

---

### 3. 间距等级

间距是后续很值得考虑的方向。

可用于：

```xml
<Panel Padding="S3" Gap="S2" />
```

可能定义：

| Token | 基准值 |
|-------|-------:|
| `S1` | `8` |
| `S2` | `16` |
| `S3` | `24` |
| `S4` | `32` |
| `S5` | `48` |

适用属性：

- `Padding`
- `Gap`
- `Margin`

暂不加入原因：

1. 本次目标是先降低规范变更范围。
2. `Margin` 支持 1~4 个逗号分隔值，若支持 token，需要定义混合写法，如 `Margin="0,0,0,S2"`。
3. 间距是否跟随画布缩放，需要单独讨论。

---

### 4. 圆角等级

可用于统一卡片、按钮、标签等元素的圆角。

示例：

```xml
<Rect CornerRadius="R3" />
```

可能定义：

| Token | 基准值 |
|-------|-------:|
| `R0` | `0` |
| `R1` | `4` |
| `R2` | `8` |
| `R3` | `12` |
| `R4` | `16` |
| `R5` | `24` |

暂不加入原因：

1. `CornerRadius` 当前还支持四角独立值，如 `12,0,12,0`，token 化会增加解析复杂度。
2. 是否允许 `CornerRadius="R3,0,R3,0"` 需要额外定义。
3. 圆角是否随画布缩放也需要讨论。

---

### 5. 阴影 / Elevation 等级

阴影参数较复杂，适合后续主题化。

当前写法：

```xml
<Rect Shadow="0 4 12 #00000033" />
```

可能写法：

```xml
<Rect Shadow="E2" />
```

可能定义：

| Token | 值 |
|-------|----|
| `E0` | `None` |
| `E1` | `0 2 6 #0000001F` |
| `E2` | `0 4 12 #00000026` |
| `E3` | `0 8 24 #0000002E` |

暂不加入原因：

1. 阴影和主题色关系紧密，若颜色 token 暂不加入，阴影 token 也应延后。
2. `Shadow` 既有字符串属性，也有 `<Shadow>` 子元素，需要统一两种表达的 token 规则。

---

### 6. 行高等级

当前行高为数字倍数：

```xml
<TextElement LineHeight="1.4" />
```

后续可考虑：

```xml
<TextElement LineHeight="LH3" />
```

可能定义：

| Token | 值 |
|-------|---:|
| `LH1` | `1.1` |
| `LH2` | `1.2` |
| `LH3` | `1.4` |
| `LH4` | `1.6` |

暂不加入原因：

1. 行高通常和 TextStyle 绑定，而不是单独频繁指定。
2. 当前 `LineHeight` 是倍数，不涉及画布缩放，复杂度不高，直接写数字仍可接受。

---

### 7. 动画节奏 Token

V3 有动画系统，后续可考虑统一动画时长与延迟。

示例：

```xml
<Appear Target="title" Duration="M2" Delay="M1" />
```

可能定义：

| Token | 值 |
|-------|---:|
| `M0` | `0` |
| `M1` | `0.15` |
| `M2` | `0.3` |
| `M3` | `0.5` |
| `M4` | `0.8` |

也可以使用语义命名：

```text
Fast / Normal / Slow
```

暂不加入原因：

1. 动画系统本身仍在演进。
2. 课件中的动画节奏和教学叙事强相关，不一定适合过早固定。

---

### 8. 描边粗细等级

可考虑：

```xml
<Rect StrokeThickness="B1" />
```

可能定义：

| Token | 值 |
|-------|---:|
| `B0` | `0` |
| `B1` | `1` |
| `B2` | `2` |
| `B3` | `4` |

暂不加入原因：

1. 描边粗细通常只需要 `0`、`1`、`2`，直接写数字已经足够简单。
2. 收益低于字号、颜色、间距、圆角和阴影。

---

### 9. 布局安全区 / 栅格

后续可考虑统一页面布局，例如：

```text
PageMargin
ContentWidth
TitleY
GridColumn
SafeArea
```

可能用法：

```xml
<TextElement X="PageMargin" Y="TitleY" Width="ContentWidth" />
```

暂不加入原因：

1. 这会把 SlideML 推向更复杂的布局系统。
2. 当前 `X`、`Y`、`Width`、`Height` 更像页面具体排版，不适合过早 token 化。
3. 如果要做，应作为单独的布局系统设计，而不是简单 token 扩展。

---

## 后续设计原则

如果未来继续引入主题化能力，建议遵守以下原则：

1. **只主题化高频、容易不统一、全局修改收益大的属性。**
2. **优先保证 LLM 易生成，不引入过多候选 token。**
3. **直接值仍然保留，作为兼容旧文档和特殊视觉需求的逃生口。**
4. **主题定义应尽量放在课件级元数据或业务层配置中，不建议每个 Page 单独定义。**
5. **新增 token 前要明确未知 token 的错误处理策略。**
6. **不要一次性把 SlideML 变成完整 CSS，保持面向幻灯片生成的轻量语义。**

---

## 建议优先级

如果未来继续推进，建议优先级如下：

| 优先级 | 能力 | 说明 |
|--------|------|------|
| P0 | 字号等级 | 已加入规范 |
| P1 | 主题色 | 收益高，但需要控制 token 数量 |
| P1 | 间距等级 | 对页面节奏统一帮助大 |
| P2 | 圆角等级 | 对卡片、按钮、标签统一有帮助 |
| P2 | 阴影等级 | 对卡片和浮层统一有帮助 |
| P3 | 行高等级 | 可与未来 TextStyle 一起考虑 |
| P3 | 动画节奏 | 适合动画系统稳定后再考虑 |
| P3 | 描边粗细 | 收益较低，可延后 |
| P4 | 布局安全区 / 栅格 | 需要单独设计布局系统 |
