# 有向菱形三类型递归实验

## 真实拓扑与端口

初始真实节点 `root/L`（Split）、`root/U`、`root/D`（Transfer）、`root/R`（Merge）；内部边 L→U、L→D、U→R、D→R，加两个不可展开的外部虚点及 S→L、R→T，共4实点、2虚点、6边。默认仍只展开 `root/U`，白底、蓝/黄交替层级。

所有非虚叶节点都可展开，每次删除被替换的真实节点，容器仅用于显示，不是真节点：

- **Transfer（1入1出）** → 四点菱形 L（Split）、U/D（Transfer）、R（Merge）。外入接 L，外出从 R 接出。
- **Split（1入2出）** → A（Split）+ 上 B、下 C 两个完整四点菱形，共9个真实节点（3 Split、4 Transfer、2 Merge）。外入接 A，A 出0→B/L、出1→C/L，B/R→原外出0、C/R→原外出1。
- **Merge（2入1出）** → 上 B、下 C 两个完整四点菱形 + A（Merge），共9个真实节点（2 Split、4 Transfer、3 Merge）。原外入0→B/L、外入1→C/L，B/R→A 入0、C/R→A 入1，A→原外出。

每个固定菱形均为 L→U、L→D、U→R、D→R。B/C 只是模板分组，不是真节点、不是额外 Transfer 替换或可单独收起的 expanded 模块，不消耗额外展开次数和递归深度。9个内部节点包括 A 均可继续展开。

每个真实节点严格保持相应类型度数；模块边界的聚合入/出度也保持原类型。构图递归返回**有序输入、输出端口集合**，按模板指定的端口索引连边，不排序当前端点 ID，也不依赖边的遍历顺序。即使两端和同类 A、异类 B/C 都被递归替换，上下支身份仍保持。稳定路径按父类型验证，例如 `root/L/A/B/R`、`root/L/B/U/L` 合法，`root/L/U` 和仅分组 `root/L/B` 非法。深度按真实节点替换次数定义，不按斜杠数量：展开 `root/L` 为深度1，随后展开 `root/L/B/L` 为深度2。

展开次数按类型记为 t、s、m：真实 V=4+3t+8(s+m)，含外部 V 再加2，总 E=6+4t+10(s+m)。Transfer 每次增加3点4边；Split/Merge 删除旧点、一次产生9点10条内部边，每次净增8点10边。收起删除整棵后代子树，恢复原节点及两支端口连接。

## 操作与几何

点击任意非虚小节点展开；小节点命中优先于容器。点击大圆或在侧栏选择模块，按“收起所选子树”。侧栏显示类型及真实/聚合入出度。T/S/M 标签分别表示传递/分流/汇流，蓝黄表示层级，侧栏有图例。

- “重置附图”：恢复仅 `root/U` 展开。
- “重置三类型示例”：根 L/U/R 各展开一次，展示三种真实有向内部结构。
- “全部收起”：恢复初始图。
- 保留仅上分支递归、上下中间分支递归；增加“全部类型递归”，按各类型合法子节点完整递归。
- 生成深度0–6，展开次数预算0–126，实际真实节点硬上限382；完整校验后才提交。预算不足或超硬上限明确报错并保留原图，**不悄悄截断**。

全类型深度0/1/2分别展开0/4/30次，真实点4/26/174，边6/34/222。深度1类型数为7 Split、12 Transfer、7 Merge；深度2为47 Split、80 Transfer、47 Merge。深度3需要204次展开、1166真实点和1482边，超过硬上限，必须原子拒绝。旧双中间分支深度6仍为126次展开、382个真实节点。

三类型示例只需展开 root/L、root/U、root/R：L 蓝色大圆内左侧黄色 A 与上下两个黄色固定小圆，小圆内为蓝色完整菱形；R 为镜像布局，U 仍为蓝圆内黄色菱形，D 保持实点。固定小圆空白不会选中父模块，节点命中优先；固定小圆不参与单独收起。菱形、分流和汇流分别采用局部几何模板；递归子图及子容器圆完整位于父圆内部。滚轮以指针为中心缩放，拖空白平移，支持按钮缩放及适应画布。深层隐藏标签，命中半径至少10设备无关像素；极深层固定命中区域可能重叠，应先缩放再选择。

## API 与测试

模型在 `Model/DiamondGraph.cs`，不依赖 WPF。保留原 `Vertex(Id, IsBoundary, CanExpand)` 构造与解构签名、`Generate(depth, bothBranches, budget)`、`ExpandedPaths`、Expand/Collapse/GetDegree/GetModuleDegree 等 API。增加 `NodeKind`、`Vertex.Kind`、`GetKind(path)`、`GenerationMode` 和枚举生成重载；L/R 现在允许展开，因此旧的“L/R非法”假设不再成立。

相邻 `.Tests` 沿用 MSTest 3.6.4、Microsoft.NET.Test.Sdk 17.12.0；覆盖旧 Transfer 行为、三类型计数与端口、嵌套接线、收起恢复、可达无环和失败原子性。不新增项目、第三方包，不修改 net9.0-windows、SDK 或 Directory.Build.props。所有界面用户文案集中在 Strings.xaml。

Windows 安装 .NET 9 SDK 和桌面运行时后，在 Visual Studio 打开 `MathGraph/MathGraph.slnx`，将本项目设为启动项目。仓库根目录构建和测试目标：

- `dotnet build MathGraph/Code/MathTest/MathGraph.DiamondFractal.Wpf/MathGraph.DiamondFractal.Wpf.csproj`
- `dotnet test MathGraph/Code/MathTest/MathGraph.DiamondFractal.Wpf.Tests/MathGraph.DiamondFractal.Wpf.Tests.csproj`

## 数学范围

三类型递归闭合，但程序始终只构造有限有向无环图。无限深度仅是数学极限，不表示已生成真正无限节点；未证明严格 GDIFS 的分离条件、测度或维数。几何缩放不能直接当成拓扑替换的维数结论。
