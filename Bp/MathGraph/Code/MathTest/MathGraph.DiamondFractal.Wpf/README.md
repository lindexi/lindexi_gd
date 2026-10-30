# 有向菱形递归实验

## 用户意图与默认图

独立的新 WPF 实验，不替换旧五点环。白底，蓝色节点与箭头，从左到右流动；外部虚点深蓝并标“外部”。根菱形上中间点默认展开为蓝色大圆，内部黄色菱形和箭头；根下中间点保持实节点。后续层交替蓝/黄。根几何约为入口(0.25,0.5)、出口(0.8,0.5)、上中间(0.525,0.25)、下中间(0.525,0.75)。

## 实际拓扑规则

diamond 的真实顶点 L、U、D、R，边 L→U、U→R、L→D、D→R，加外部 S→L、R→T。仅叶子 U/D 可展开。

展开 U/D 时**删除原顶点**，以四顶点 diamond 替换，原入边接子 L、子 R 接原出边。超点圆只作视觉容器，不是额外拓扑顶点。所有内部图与外部图通过真实边连通，不是独立图的装饰嵌套。边端避开节点圆心，箭头指向目标圆周；跨容器边穿过圆边界直连内部端口。

每展开一次真实节点+3、总边+4。展开 k 次：真实 V=4+3k，含两外部虚点 V=6+3k，总 E=6+4k。默认 k=1，所以真实7、含外部9、边10。

- 每个 diamond 入口真实入1出2，出口真实入2出1。
- 叶子中间点入1出1。
- 超点**作为模块聚合**入1出1；绝不表示内部各顶点都入1出1。
- 外部 S 仅1出，T 仅1入，不计入真实节点统计。

## 操作

点击任意小中间节点展开，节点命中优先于容器；点击容器或从侧栏选择模块，再按“收起所选子树”。收起删除后代并恢复原节点及父边。稳定路径如 `root/U/D` 不依赖显示顺序。

“重置附图”恢复仅展开 root/U；“全部收起”恢复最初四节点。深度滑块0–6，选择仅上分支或上下同时递归，设置展开次数预算后生成。上分支深度 d 展开 d 次；双分支展开 2^(d+1)-2 次。生成前校验完整预算，失败不修改现有图。手动展开同样受硬上限约束：深度6、126次展开、382个真实节点（含外部384），总边最多510。

滚轮以指针为中心缩放，空白拖动平移，支持按钮缩放和适应画布。节点命中半径至少10设备无关像素；深层节点过小隐藏标签。容器按父菱形局部映射递归放置，子图及其容器保持在父圆内；极深层固定命中区域可能重叠，可先缩放再选取。侧栏展示真实统计、所选节点真实度数或模块聚合度数。

## 数学范围

双分支展开提供“层内无限点”的有限逼近，深度无限仅是数学极限。本程序每次只构造有限有向无环图，不声称生成真正无限节点，也不把视觉缩放比或拓扑替换规则误称为严格 GDIFS 维数。未建立满足分离条件的图导向迭代函数系统、测度和维数证明。

## 目录与启动

`Model/DiamondGraph.cs` 不依赖 WPF，公开 API 用于测试。界面用户文案集中在 `Strings.xaml`；不增加第三方 UI 库或独立模型项目。相邻 `.Tests` 使用已有 Microsoft.NET.Test.Sdk 17.12.0 / MSTest 3.6.4，目标 net9.0-windows。

Windows 安装 .NET 9 SDK 和桌面运行时后，在 Visual Studio 打开 `MathGraph/MathGraph.slnx`，将本项目设为启动项目运行。也可在仓库根目录使用：

- 构建：`dotnet build MathGraph/Code/GraphExperiments/MathGraph.DiamondFractal.Wpf/MathGraph.DiamondFractal.Wpf.csproj`
- 启动：`dotnet run --project MathGraph/Code/GraphExperiments/MathGraph.DiamondFractal.Wpf/MathGraph.DiamondFractal.Wpf.csproj`
- 测试：`dotnet test MathGraph/Code/GraphExperiments/MathGraph.DiamondFractal.Wpf.Tests/MathGraph.DiamondFractal.Wpf.Tests.csproj`

遵循现有 `Directory.Build.props` 的 artifacts 输出约定，不修改其 UseArtifactsOutput 或 LangVersion；旧实验保持原位。
