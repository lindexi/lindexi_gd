# 正反五点有限拓扑研究

独立项目：`MathGraph/Code/MathTest/MathGraph.ReversibleFractal.Wpf`，测试在相邻的 `MathGraph.ReversibleFractal.Wpf.Tests`。两项目加入 `MathGraph/MathGraph.slnx` 的 `/Code/MathTest/` 分组。旧 FiveNodeFractal、Diamond、GdIfs 项目及 Docs 研究总路线不修改。

## 两类模板与端口语义

基础图是正向 FullCross：`A→C,A→D,B→C,B→D,C→E,D→E`，外部连接 `S0→A,S1→B,E→T`。初始5真实点、3外部边界点、9边。

- 2入1出叶：替换成正向五点，输入0/1分别到A/B，E输出。
- 1入2出叶：替换成六条内部边全部反向的五点，即 `C→A,D→A,C→B,D→B,E→C,E→D`。输入到E，A/B分别提供输出0/1。
- 正向模块几何列为 `A,B / C,D / E`；反向为 `E / C,D / A,B`。字母不重命名。嵌套测量和放置各自读取模块方向，内部箭头仍向右。
- 每个模板严格只有ABCDE五个真实子点。没有I/O适配点，容器没有计算行为，也不计入顶点。每次替换净增4真实点、6边。
- 正向子点A/B为12，C/D/E为21；反向子点A/B为21，C/D/E为12。实际叶度数始终闭合于12和21，按实际保留度数自动选下一次模板。

端口编号由固定模板边顺序确定，与叶ID排序、展开先后无关。`GetBoundary`递归返回有序真实叶端口，模块外部边直接连接这些叶端口，不连接容器中心；原连接的两端都展开时也保留输入/输出身份。`GetDegree`读取当前实际度数，`GetSignature`读取叶或容器的保留度数，`GetDirection`读取对应替换方向。模型仅使用.NET类型，不依赖WPF类型；测试仅使用公开API，没有InternalsVisibleTo。

全叶同步替换：第1轮5次展开，25真实点39边；第2轮累计30次展开，125真实点189边；第3轮累计155次展开，625真实点939边。一般第n轮真实点为5^(n+1)，边为9+6×累计展开数。容器路径稳定，收起移除其展开子树、恢复原叶与原连接，兄弟模块保持不变。

展开后的真实叶、外部边界点和有向边由 `MathGraphs` 存储，边数据保留起点和终点的端口号。`ReversibleGraph` 继续管理展开路径、模板和有序模块边界；`Vertices` 与 `Edges` 是从底图生成的只读视图，WPF 不直接访问可变底图。

## 交互与资源约束

右侧栏250像素，无窗口内大标题、长规则正文或底部说明。必要统计显示真实点、外部点、边、模块数、深度；选择模块可查看方向和端口数。

- 左键叶展开；左键模块空白或侧栏列表选择模块。
- 右键模块空白收起命中的最深模块；右键叶收起其最深已展开父模块。基础图叶无父模块时不操作。
- 拖动空白平移；滚轮以鼠标为锚缩放，按钮以视图中心缩放；可适应视图。
- 提供重置后展开C的正向示例、展开A的反向示例、收起全部和收起所选模块。
- 批量层数输入非负整数，0恢复基础图。负值、非整数拒绝。

手动展开没有业务深度、次数、节点限制；缩放没有业务上下限，仅拒绝非有限、非正、浮点溢出/下溢的状态，平移也进行数值安全检查。实际仍受内存、浮点精度、递归调用栈及绘图资源限制，不承诺无限规模可运行。

仅批量 `Generate(depth, expansionBudget)` 有独立预算，默认 `DefaultExpansionBudget = 1000` 次展开。生成前按每轮叶数5、25、125……累计预检，超预算拒绝，不静默截断。默认可生成4轮（累计780次展开、3125真实点、4689边）；5轮需显式预算至少3905。可通过公开API指定更大预算，不影响手动操作。生成和手动编辑均先构建候选状态，成功后整体提交；参数错误和预算拒绝保留原图。

## 研究边界

这是正反双类型的**有限拓扑替换研究**，不是数值计算模型或收益证明。不证明数值函数保持，不证明训练效果、收敛性、严格自相似或任何维数，也不提供维数估计。线段交叉不产生额外节点。没有引入随机连接或额外第三方库。

## 运行与验证

沿用旧项目 `Microsoft.NET.Sdk`、`net9.0-windows`、WPF、nullable和implicit usings配置，并继承 `MathGraph/Directory.Build.props`；未调整SDK、TFM或语言版本。测试沿用 Microsoft.NET.Test.Sdk 17.12.0、MSTest 3.6.4。

在支持.NET 9 WPF的Windows环境中打开 `MathGraph/MathGraph.slnx`，设置 `MathGraph.ReversibleFractal.Wpf` 为启动项目。

- 运行：`dotnet run --project MathGraph/Code/MathTest/MathGraph.ReversibleFractal.Wpf/MathGraph.ReversibleFractal.Wpf.csproj`
- 构建：`dotnet build MathGraph/Code/MathTest/MathGraph.ReversibleFractal.Wpf/MathGraph.ReversibleFractal.Wpf.csproj`
- 测试：`dotnet test MathGraph/Code/MathTest/MathGraph.ReversibleFractal.Wpf.Tests/MathGraph.ReversibleFractal.Wpf.Tests.csproj`

MSTest覆盖基础边和实际度数、正向C/反向A完整内部外部端口边、B/E重接、ABCDE模板、净增长、混合双端递归与展开顺序等价、有序叶边界、方向闭合、端口唯一连续、端点存在、双向可达、无环、整轮计数、收起恢复、超旧深度链、预算接受及拒绝原子性。模型测试不替代WPF视觉与鼠标交互人工验收。
