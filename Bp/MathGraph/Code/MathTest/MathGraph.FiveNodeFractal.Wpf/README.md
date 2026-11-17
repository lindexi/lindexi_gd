# 五点有向图递归 WPF 实验

位置：`MathGraph/Code/MathTest/MathGraph.FiveNodeFractal.Wpf`；测试在相邻 `.Tests` 项目。两个项目注册于 `MathGraph/MathGraph.slnx` 的 `/Code/MathTest/` 分组。旧 Diamond/GdIfs 实验不作修改或复用包装。

## 三种非穷尽原型

均有独立外部输入 `S0→A`、`S1→B` 和输出 `E→T`。整体为2入1出；仅S0、S1、T是虚点。初始5真实点、3虚点。A左上、B左下、C中上、D中下、E右中；线段交叉处不产生节点。

| 原型 | 内部有向边 | 总边数 | A/B/C/D/E真实度数（入,出） |
|---|---|---|---|
| FullCross | A→C,A→D,B→C,B→D,C→E,D→E | 9 | 12 / 12 / 21 / 21 / 21 |
| SingleCross | A→C,A→D,B→D,C→E,D→E | 8 | 12 / 11 / 11 / 21 / 21 |
| Parallel | A→C,B→D,C→E,D→E | 7 | 11 / 11 / 11 / 11 / 21 |

这只是研究用的三种连接方式，非穷尽枚举，更不代表唯一正确的五点分形。尤其SingleCross的C与D类型不同，不能硬编码为同型。

## 有限端口替换

所有非虚叶点（含I/O适配点）均可展开，删除原点并用选定原型的完整核心及必要适配点替换，原外部边重接到真实叶点，而不是装饰容器中心。

- p=2：原输入0/1分别接核心A/B；两独立输入不先汇流再分流。
- p=1：显式增加真实split点I，原入接I，I的0/1输出分别接A/B。
- q=1：核心E直接接原输出。
- q=2：显式增加真实split点O，E→O，O按0/1连接原输出。

| p,q | 模板真实顶点 | 净增真实点 | 内部边（也是总边净增） |
|---|---:|---:|---|
| 2,1 | 5 | 4 | k |
| 1,1 | 6 | 5 | k+2 |
| 1,2 | 7 | 6 | k+3 |
| 2,2 | 6 | 5 | k+1 |

k为原型内部边数。内部算法按p/q接线，可组合22规则，但本次三个原型均不自然产生22，未暴露无用的任意类型注入API，也不宣称UI可操作22。

FullCross展开C（21）：原A→C、B→C改为A→C/A、B→C/B，C/E→E；只有五个模板点。初始5点9边变为9点15边。

FullCross展开A（12）：S0→A/I，I→A/A、A/B；核心E→A/O，O的0/1输出分别接原C/D，共七个模板真实点（含I/O），变为11点18边。适配点、核心各点深度相同，全部可再次展开，没有隐藏分组层级。

`root/A`等路径稳定。模板边顺序确定端口身份，与展开后的ID字典排序无关。公开`GetBoundary`递归返回有序输入/输出实际叶端口；`GetSignature`返回原点/模块保留的度数，`GetDegree`读取当前真实叶度数。容器不是顶点，收起恢复原点及原接线并移除其展开子树。

## 操作与预算

默认FullCross基础图。切换原型重置展开。提供C与A示例（示例类型取决于当前原型）及全收起。左键真实叶点（含I/O）展开；右键模块空白收起命中的最深模块子树，右键叶点收起包含它的最深已展开模块，基础图叶点无模块可收起时不操作。仍支持左键容器/列表选择后用按钮收起。滚轮以鼠标位置为锚缩放，按钮以视图中心缩放，空白拖动平移，并可适应视图。窗口标题保留，移除窗口内重复标题、底部提示与右下规则长文，右侧栏宽250。

手动展开不设固定深度、次数或节点预算；缩放不设业务上下限，仅拒绝非有限、非正或数值溢出/下溢的缩放状态。实际规模仍受double精度、内存及递归调用栈等机器资源约束，不保证无限规模可计算。蓝黄交替深度，方形I/O区别于圆形核心，边端标注0/1；深层标签隐藏，放大可查看。布局递归计算子树尺寸、分列分行留白；跨容器长边可能交叉。

批量层数改为非负整数文本框（0恢复基础图），无0–3滑条限制，负值和非整数拒绝。为避免全图指数增长，只有批量`Generate(depth, expansionBudget)`默认使用`DefaultExpansionBudget = 1000`次展开预算；没有固定深度或节点上限，显式预算可以大于1000，且不影响手动展开。生成前按每轮叶数递推（初始5，下一轮6n−1）预检所需总展开数，超预算立即拒绝并保留原图，不静默截断；通过后从基础图按轮重建，最后原子替换。默认允许3轮（207次展开、1037真实点），4轮需显式预算至少1244。支持全体和手动路径递归，不提供另一个指定路径链批量编辑器。

独立计数校验：第1轮5次展开、29真实点；第2轮34次展开、173真实点。FullCross全叶第1/2轮为45/261边；SingleCross为40/232边；Parallel为35/203边。FullCross第1轮14个12及15个21，第2轮增14×6+15×4=144点。SingleCross第1轮9个12、10个11、10个21；Parallel第1轮24个11、5个21，也各净增144点。

这是有限拓扑替换近似，只有全图批量生成有默认资源预算，不是严格GDIFS定义、收敛性或维数证明。没有添加随机连接、穷举枚举、维数估计或其他第三方库。

## 启动与验证

使用支持net9.0-windows WPF的Windows/.NET SDK。在Visual Studio打开`MathGraph/MathGraph.slnx`，将`MathGraph.FiveNodeFractal.Wpf`设为启动项目运行。也可使用 `dotnet run --project MathGraph/Code/MathTest/MathGraph.FiveNodeFractal.Wpf`。

构建：`dotnet build MathGraph/Code/MathTest/MathGraph.FiveNodeFractal.Wpf/MathGraph.FiveNodeFractal.Wpf.csproj`。
测试：`dotnet test MathGraph/Code/MathTest/MathGraph.FiveNodeFractal.Wpf.Tests/MathGraph.FiveNodeFractal.Wpf.Tests.csproj`。

沿用目录内`Directory.Build.props`的语言配置；未新增global.json。MSTest使用Microsoft.NET.Test.Sdk 17.12.0、MSTest 3.6.4。模型不依赖WPF类型，测试走公开API；WPF绘制与交互不包含在模型单元测试的视觉验收范围。
