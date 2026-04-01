# XAML 命名补充经验（Avalonia）

## 目标

为后续维护提升可读性与定位效率：给关键控件补充 `x:Name`。

## 可执行策略

- 优先给“可交互/可查找/关键布局节点”命名：
  - 根 `UserControl`
  - 容器 `Border`
  - `ListBox`
  - 标签项 `Border`
  - `ContextMenu` / `MenuItem`
  - 展示文本 `TextBlock`

## 本次踩坑记录

- `ItemsPanelTemplate` 不能设置 `x:Name`（会触发编译错误，无法解析 `Name` 属性）。
- 处理方式：
  - 移除 `ItemsPanelTemplate` 上的 `x:Name`
  - 保留其内部可命名控件（如 `StackPanel`）的命名

## 建议

- 新增命名后应立即构建验证，优先发现 XAML 编译期问题。
- 命名应语义化，避免 `Control1` 这类无语义名称。

## 控件获取约定

- 只要 XAML 已经声明了 `x:Name`，就在对应 `.axaml.cs` 里直接使用生成的同名字段。
- 不要再为这些已命名控件额外包一层 `FindControl<T>(...)` 属性或方法。
- `FindControl<T>(...)` 只用于运行时模板内容、名称无法在编译期直接生成字段的场景；常规视图控件不是这个用法。

## 本次修正记录

- `RightSlideBar` 已在 XAML 中声明 `x:Name="ToggleSidebarButton"`、`x:Name="ToggleChevronTextBlock"`、`x:Name="SidebarContentHost"`。
- 正确写法是在 `RightSlideBar.axaml.cs` 中直接访问 `ToggleSidebarButton`、`ToggleChevronTextBlock`、`SidebarContentHost`。
- 之前额外定义 `ToggleSidebarButtonControl => this.FindControl<Button>(...)` 这类包装属性会降低可读性，也偏离当前项目对 Avalonia 的常规写法，后续不要再引入。
