# GD-IFS 五点环研究台

独立的 .NET 9 / WPF 中文研究示意程序，只支持 Windows，需要 .NET 9 SDK 与 Windows Desktop 运行时。未修改已有项目、SDK、TFM 或公共语言配置。

## 启动与测试

在仓库根目录使用 Visual Studio 打开 `MathGraph/MathGraph.slnx`，将 `MathGraph.GdIfs.Wpf` 设为启动项目并运行。也可以在终端使用：

- 启动：`dotnet run --project MathGraph/Code/MathTest/MathGraph.GdIfs.Wpf/MathGraph.GdIfs.Wpf.csproj`
- 构建：`dotnet build MathGraph/Code/MathTest/MathGraph.GdIfs.Wpf/MathGraph.GdIfs.Wpf.csproj`
- 测试：`dotnet test MathGraph/Code/MathTest/MathGraph.GdIfs.Wpf.Tests/MathGraph.GdIfs.Wpf.Tests.csproj`

输出位置遵循仓库现有 `UseArtifactsOutput` 配置，不假设输出在项目 bin 目录。

## 交互

- 点击顶层或任意可见层的节点，或通过下拉框选择唯一超点；所有层同步采用该局部编号。
- 深度范围 0–8；收缩比范围 0.12–0.36。
- 滚轮围绕鼠标缩放，拖动空白平移；按钮提供居中缩放与复位。视图缩放不影响模型参数。
- 节点标签为 `L层号 · 节点号`，节点号为 1–5；模型索引为 0–4。颜色按层区分。
- 点击区域至少保持固定屏幕像素大小。极深层投影重叠时无法同时辨认所有点，可在目标附近继续放大或使用下拉框；最大放大倍数为一亿倍。

## 数学规则与限制

原型是单位外接圆上的正五边形闭环 C5。设固定选择为 s，收缩比为 r，第 l 层中心为 c_l、半径为 R_l，局部单位顶点为 u_i：

- c_0 = (0, 0)，R_0 = 1。
- v_(l,i) = c_l + R_l u_i。
- c_(l+1) = v_(l,s)，R_(l+1) = r R_l。
- 仅所选节点包含下一层 C5，另外四点不递归；该规则每层一致。

深度 d 表示 d 次嵌入，因此共有 d+1 个环、5(d+1) 个节点和 5(d+1) 条边。深度 0 仅含基础 C5。虚线圆包围子环，仅是超点的可视化容器，不是额外顶点。最后一层的所选点是递归截断处，不显示空容器。

外部边仍连接本层节点；内部图只作层级包含，**不自动与父图重接，不存在跨层边**。因此不把整个图解释成一个已定义端口接线的连通替换图。

这是单分支图引导自相似结构的有限层示意，不是五分支 IFS；不宣称产生非平凡分形维数，不计算 `log(5)/log(1/r)`。容器、固定像素节点标记和标签仅用于交互，不构成新的数学几何对象。

## 代码组织

- `Model/CycleModel.cs`：不可变输出的纯几何与拓扑模型，不依赖 WPF 或 UI 线程，时间和空间均为 O(d)。
- `GraphView.cs`：WPF 绘制、层级颜色、点击命中和视图变换。
- `MainWindow.xaml` 与代码后置：控制面板与同步。
- `Strings.xaml`：集中存放面向用户的中文字符串，可用于后续本地化。
- 相邻 `MathGraph.GdIfs.Wpf.Tests`：MSTest，直接通过公开模型 API 验证闭环、位置、相似变换、计数及非法输入，无 UI 线程依赖。
