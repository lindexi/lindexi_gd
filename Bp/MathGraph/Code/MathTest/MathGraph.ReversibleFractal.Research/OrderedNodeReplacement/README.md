# 固定有序端口的节点替换

独立的 .NET 9 研究项目，受 Engelfriet 和 Rozenberg 的节点替换图文法启发，但不是完整的 edNCE。此处只实现固定签名的确定性重接，不实现基于标签和邻点上下文的连接指令、规则选择或文法推导。

## 模型与替换

- `OrderedGraph` 保存有向图快照。每个节点分别声明输入、输出端口数；两组端口独立从 0 编号，每个方向的每个端口最多占用一次。允许未连接端口、不同端口间的平行边和自环。
- `ReplacementTemplate` 接收模板节点、内部边，以及有序的输入和输出边界。边界列表第 `i` 项指定旧节点同方向第 `i` 个端口对应的真实模板端口；映射在同方向内不能重复或占用已接内部边的端口。
- `Apply` 要求目标与模板签名完全相同。它移除目标节点，加入模板节点与内部边，并逐端重接原有边；自环的两端分别重接，不插入适配点，也不修改输入图。新节点 ID 由原节点 ID 与模板局部名组成，和应用顺序无关。图等价按节点和边比较，不依赖列表顺序。

签名和拓扑由调用方提供的数据决定；没有写死三点或五点规则，不提供逆向折叠操作，也不依赖其他研究项目。

## WPF 可视化

`Wpf/MathGraph.OrderedNodeReplacement.Wpf.csproj` 是独立的 `net9.0-windows` WPF 项目，直接引用核心库，不加入解决方案。在 Windows 上使用 .NET 9 SDK，于本目录执行：

- 构建：`dotnet build Wpf/MathGraph.OrderedNodeReplacement.Wpf.csproj`
- 启动窗口：`dotnet run --project Wpf/MathGraph.OrderedNodeReplacement.Wpf.csproj`

在下拉框切换三点链（1 入 1 出）和五点分支（2 入 2 出），窗口会调用 `ReplacementTemplate.Apply` 并重绘展开前后的节点和有向边。节点左侧的 `inN` 和右侧的 `outN` 分别表示输入、输出端口；悬停在边上可查看完整的源、目标端口。五点模板的边界顺序与局部节点的声明顺序不同，例如目标的 `in0` 重接到 `target/b:in0`。

图下方显示展开前后的节点和边数：三点示例为 3 → 5 个节点、2 → 4 条边；五点示例为 3 → 7 个节点、4 → 8 条边。两个示例的适配点数均为 0，原有边直接重接到模板端口。

## 测试

`OrderedNodeReplacement.Core/` 是核心库；`OrderedNodeReplacement.Tests/` 使用 Microsoft.NET.Test.Sdk 17.12.0 和 MSTest 3.6.4。测试覆盖三点链、2 入 2 出的五点模板及非自然边界顺序、相邻节点的两种替换顺序、自环、非法端口和签名不匹配。

在本目录使用 .NET 9 SDK 运行：

`dotnet test OrderedNodeReplacement.Tests/OrderedNodeReplacement.Tests.csproj`
