# 实战范例

两个真实改造案例：一个"提问讨论页"重排成三行横向对照，一个"知识点"重排成条目式拆解。范例给出可直接复用的整页 XML，`Source` 中的资源 ID 需换成当前课件里的真实 ID。

## 目录

1. [案例：三行横向对照](#1-案例三行横向对照)
2. [完整 XML](#2-完整-xml)
3. [案例：知识点拆解](#3-案例知识点拆解)
4. [两类案例的共同点](#4-两类案例的共同点)

## 1 案例：三行横向对照

原页是一道讨论题：三个运动物体的速度变化比较。原始状态：

- 文字被拆成十几个零散文本框，`200m/s`、`0`、`速度变化量为0，` 各自独立，无法整行调整。
- 画布外残留三个不可见组合元素（X 超出 1280），需要清除。
- 上下两行结论没有共同基准线，换行后三行基线参差。
- 标题为宋体 42px 纯红，同时用居中定位和固定 X 坐标，整体偏右。
- 背景是带斜切的浅蓝底图，与全册其它页不统一。

改造决策：

| 问题 | 处理 |
| --- | --- |
| 零散文本框 | 每行合并成固定三栏：图 ／ 过程轴 ／ 结论 |
| 残留元素 | 整页替换时一并清除 |
| 基准线不齐 | 三行共用同一份坐标模板，只换文字 |
| 大面积红 | 收敛成灰阶 + 砖红，仅染每行最终比值 |
| 背景图 | 换纯白，靠细线与留白分层 |
| 过程说不清（只有一根横线） | 改成两端带刻度的区间轴，标"匀速飞行／加速 20 s／减速 5 s" |
| 数字排版 | 去 Times 斜体，数值与单位留空格 |

数据核对（改造前后逐字一致）：飞机 `200 m/s` → `200 m/s`，`Δv = 0`，比值 `0`；汽车 `0` → `20 m/s`（20 s），`Δv = 20 m/s`，比值 `1 m/s²`；运动员 `10 m/s` → `0`（5 s），`Δv = 10 m/s`，比值 `2 m/s²`。

## 2 完整 XML

```
<?xml version="1.0" encoding="UTF-8"?>
<Page Background="#FFFFFF">
  <TextElement Id="hdr-kicker" X="120" Y="34" Width="420" Text="讨论与交流" FontSize="13" FontName="微软雅黑" Foreground="#8A8A8A" />
  <TextElement Id="hdr-title" X="120" Y="54" Width="980" Text="比较运动物体速度变化的快慢" FontSize="34" IsBold="True" FontName="SimSun" Foreground="#1F2328" />
  <TextElement Id="hdr-meta" X="800" Y="34" Width="400" Text="第一章 运动的描述 · 第五节 加速度" FontSize="12" FontName="微软雅黑" Foreground="#9A9A9A" TextAlignment="Right" />
  <Rect Id="hdr-rule" X="120" Y="108" Width="1040" Height="1" Fill="#D8D8D4" />

  <Image Id="r1-photo" X="120" Y="156" Width="150" Height="112" Source="img_3" Stretch="UniformToFill" />
  <TextElement Id="r1-plabel" X="300" Y="176" Width="260" Text="匀速飞行" FontSize="15" FontName="微软雅黑" Foreground="#6B7280" TextAlignment="Center" />
  <TextElement Id="r1-v0" X="180" Y="216" Width="116" Text="200 m/s" FontSize="17" FontName="SimSun" Foreground="#1F2328" TextAlignment="Right" />
  <TextElement Id="r1-v1" X="564" Y="216" Width="116" Text="200 m/s" FontSize="17" FontName="SimSun" Foreground="#1F2328" />
  <Rect Id="r1-axis" X="300" Y="248" Width="260" Height="1" Fill="#B9B9B4" />
  <Rect Id="r1-tick-l" X="300" Y="242" Width="1" Height="13" Fill="#B9B9B4" />
  <Rect Id="r1-tick-r" X="559" Y="242" Width="1" Height="13" Fill="#B9B9B4" />
  <Rect Id="r1-div" X="700" Y="148" Width="1" Height="128" Fill="#EDEDEA" />
  <TextElement Id="r1-dv" X="724" Y="180" Width="440" Text="速度变化量 Δv = 0" FontSize="15" FontName="微软雅黑" Foreground="#6B7280" />
  <TextElement Id="r1-rl" X="724" Y="220" Width="340" Text="速度变化快慢 Δv/Δt =" FontSize="22" FontName="SimSun" Foreground="#1F2328" />
  <TextElement Id="r1-rv" X="1080" Y="216" Width="120" Text="0" FontSize="26" IsBold="True" FontName="SimSun" Foreground="#A34A31" TextAlignment="Right" />

  <Rect Id="sep-1" X="120" Y="304" Width="1040" Height="1" Fill="#EFEFEB" />

  <Image Id="r2-photo" X="120" Y="340" Width="150" Height="112" Source="img_4" Stretch="UniformToFill" />
  <TextElement Id="r2-plabel" X="300" Y="360" Width="260" Text="加速 20 s" FontSize="15" FontName="微软雅黑" Foreground="#6B7280" TextAlignment="Center" />
  <TextElement Id="r2-v0" X="180" Y="400" Width="116" Text="0" FontSize="17" FontName="SimSun" Foreground="#1F2328" TextAlignment="Right" />
  <TextElement Id="r2-v1" X="564" Y="400" Width="116" Text="20 m/s" FontSize="17" FontName="SimSun" Foreground="#1F2328" />
  <Rect Id="r2-axis" X="300" Y="432" Width="260" Height="1" Fill="#B9B9B4" />
  <Rect Id="r2-tick-l" X="300" Y="426" Width="1" Height="13" Fill="#B9B9B4" />
  <Rect Id="r2-tick-r" X="559" Y="426" Width="1" Height="13" Fill="#B9B9B4" />
  <Rect Id="r2-div" X="700" Y="332" Width="1" Height="128" Fill="#EDEDEA" />
  <TextElement Id="r2-dv" X="724" Y="364" Width="440" Text="速度变化量 Δv = 20 m/s" FontSize="15" FontName="微软雅黑" Foreground="#6B7280" />
  <TextElement Id="r2-rl" X="724" Y="404" Width="340" Text="速度变化快慢 Δv/Δt =" FontSize="22" FontName="SimSun" Foreground="#1F2328" />
  <TextElement Id="r2-rv" X="1080" Y="400" Width="120" Text="1 m/s²" FontSize="26" IsBold="True" FontName="SimSun" Foreground="#A34A31" TextAlignment="Right" />

  <Rect Id="sep-2" X="120" Y="488" Width="1040" Height="1" Fill="#EFEFEB" />

  <Image Id="r3-photo" X="120" Y="524" Width="150" Height="112" Source="img_5" Stretch="UniformToFill" />
  <TextElement Id="r3-plabel" X="300" Y="544" Width="260" Text="减速 5 s" FontSize="15" FontName="微软雅黑" Foreground="#6B7280" TextAlignment="Center" />
  <TextElement Id="r3-v0" X="180" Y="584" Width="116" Text="10 m/s" FontSize="17" FontName="SimSun" Foreground="#1F2328" TextAlignment="Right" />
  <TextElement Id="r3-v1" X="564" Y="584" Width="116" Text="0" FontSize="17" FontName="SimSun" Foreground="#1F2328" />
  <Rect Id="r3-axis" X="300" Y="616" Width="260" Height="1" Fill="#B9B9B4" />
  <Rect Id="r3-tick-l" X="300" Y="610" Width="1" Height="13" Fill="#B9B9B4" />
  <Rect Id="r3-tick-r" X="559" Y="610" Width="1" Height="13" Fill="#B9B9B4" />
  <Rect Id="r3-div" X="700" Y="516" Width="1" Height="128" Fill="#EDEDEA" />
  <TextElement Id="r3-dv" X="724" Y="548" Width="440" Text="速度变化量 Δv = 10 m/s" FontSize="15" FontName="微软雅黑" Foreground="#6B7280" />
  <TextElement Id="r3-rl" X="724" Y="588" Width="340" Text="速度变化快慢 Δv/Δt =" FontSize="22" FontName="SimSun" Foreground="#1F2328" />
  <TextElement Id="r3-rv" X="1080" Y="584" Width="120" Text="2 m/s²" FontSize="26" IsBold="True" FontName="SimSun" Foreground="#A34A31" TextAlignment="Right" />

  <TextElement Id="pageno" X="1140" Y="690" Width="60" Text="2" FontSize="12" FontName="微软雅黑" Foreground="#9A9A9A" TextAlignment="Right" />
</Page>
```

三行用同一份坐标模板，行首 Y 分别为 124／308／492（`r1-*` 元素在此基础上加偏移）。复制时只改文字，不要改偏移量。

## 3 案例：知识点拆解

原页是概念讲解，一条条填空加红色答案，标题被挤成两行。改造为条目式拆解，不做卡片：

- 标题带同上；正文从 `Y=132` 开始，四条内容，条目间距 16。
- 每条左侧固定宽 64 的标签（右对齐），右侧正文；条目间用 `#EFEFEB` 细线分隔。
- 公式行字号比正文大 6–10px 并加粗；单位那一行用较大字号但同一位置对齐。
- 顶部说明与底部注释用 14–15px 灰字，形成"大—小—大"的节奏。

单条目的结构（其余条目复制并改 Y 与文字）：

```
<Rect Id="it1-tag" X="120" Y="140" Width="96" Height="26" Fill="#F2F1ED" />
<TextElement Id="it1-tagtext" X="120" Y="143" Width="96" Text="定义" FontSize="15" FontName="微软雅黑" Foreground="#5B5348" TextAlignment="Center" />
<TextElement Id="it1-body" X="240" Y="136" Width="960" Text="速度的变化量与发生这一变化所用时间的比值，叫做加速度。" FontSize="22" FontName="SimSun" Foreground="#1F2328" />
<Rect Id="it1-sep" X="120" Y="196" Width="1040" Height="1" Fill="#EFEFEB" />
```

若原页是"填空 + 答案"形式，且课上要逐条揭示，就把答案文字单独放一个 `TextElement`，底下垫一块浅色 `Rect`（如 `#F2F1ED`），后续可直接遮挡或隐藏。

## 4 两类案例的共同点

- 先清残留元素，再重排：整页替换天然完成这一步，只需确认没有把有用内容一起删掉。
- 用同一份坐标模板复制多份，是保证对齐最省事的方式。
- 一段信息一个 `TextElement`；不要把一整行拆成多个碎片，也不要一条长文本里塞完所有层级。
- 强调只给"这一页最该被看见的那个数值"，其余一律灰阶。
- 交付前截图逐条核对数值与符号，再说明哪条警告是误报。
