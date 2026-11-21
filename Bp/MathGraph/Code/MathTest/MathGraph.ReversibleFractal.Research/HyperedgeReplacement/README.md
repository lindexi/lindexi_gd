# 超边替换

独立的 .NET 9 实验，参照 Frank Drewes、Hans-Jörg Kreowski 和 Annegret Habel 的 *Hyperedge Replacement Graph Grammars*（*Handbook of Graph Grammars and Computing by Graph Transformation*, Vol. 1, 1997）。这里只实现对指定边应用一条规则的操作；顶点只表示拓扑及端口关系，不赋予求值语义。

- 超边的附着列表有序，长度等于标签的 rank，可重复附着同一顶点。标签按名称、rank 和终结属性匹配，规则左部必须是非终结标签。
- 规则右部是独立的超图，外部顶点列表有序、无重复，长度等于左部 rank。应用规则时，仅移除宿主中选定的边；右部内部顶点和所有右部边均新建，第 `i` 个外部顶点映射到被替换边的第 `i` 个附着顶点。宿主及规则模板不变。

## 3 → 5 顶点示例

初始图有 `A(s,j)` 和 `A(j,t)`，共享顶点 `j`。规则把 `A(x,y)` 替换为终结边 `link(x,m)` 和 `link(m,y)`，其中 `[x,y]` 是外部顶点列表，`m` 每次新建。先左后右或先右后左，结果都可写成 `s → u → j → v → t`。两次结果的内部顶点实例不同；固定 `s,j,t` 并按位置对应 `u,v` 后，标签和附着顺序相同。这里比较的是对两条指定边应用已选规则的次序，不比较不同规则的选择。

## WPF 可视化

在安装了 .NET 9 SDK 的 Windows 上，从本目录运行 `dotnet run --project Wpf/MathGraph.HyperedgeReplacement.Wpf.csproj`。

- 点击「展开左侧 A」或「展开右侧 A」，左图保留本步操作前的图，右图显示核心替换后的图。两侧可按任意次序展开；紫色顶点 `j` 始终是同一个共享顶点。
- 展开前可勾选交换外部顶点顺序。用同一条 `A` 对比默认和交换后的展开结果：连线上的端口 `1`、`2` 会对调，而右侧模板的边定义不变。
- 点击「重置」恢复初始图及默认顺序。

## 测试

在本目录运行 `dotnet test HyperedgeReplacement.Tests/HyperedgeReplacement.Tests.csproj`。
