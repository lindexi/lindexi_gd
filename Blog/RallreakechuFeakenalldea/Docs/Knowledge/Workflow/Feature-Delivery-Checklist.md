# 功能交付检查清单（本次任务复盘）

## 需求理解

- 在标签项右键菜单中提供“在文件资源管理器中打开”。
- 同时补齐关键 XAML 控件命名。

## 实施顺序

1. 先改 XAML（菜单与命名）。
2. 再补 code-behind 事件处理。
3. 进行编译验证并修正错误。

## 本次实际修正点

- 补充 `using System;`，以使用 `OperatingSystem.IsWindows()`。
- 移除 `ItemsPanelTemplate` 的 `x:Name`，解决 Avalonia XAML 编译错误。

## 可复用经验

- 任何 XAML 结构性改动后，都应立即运行一次构建。
- 事件处理与绑定上下文（`DataContext`）应显式做模式匹配和空值防护。
- 先做最小改动达成目标，再根据构建反馈迭代修正。
