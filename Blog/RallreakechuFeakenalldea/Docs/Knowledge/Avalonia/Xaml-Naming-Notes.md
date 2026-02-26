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
