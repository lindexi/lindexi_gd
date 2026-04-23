# TabBar 交互与横向滚动经验

## 场景

`TabBar` 当前覆盖两类常见交互：

1. 在标签项（`DataTemplate` 内 `Border`）上，右键提供“在文件资源管理器中打开”。
2. 标签数量变多后，允许横向滚动浏览全部标签，同时尽量不让滚动条占用太多 UI 空间。

## 横向滚动实现要点

1. 继续使用 `ListBox` 承载标签项，不额外包一层独立滚动容器。
2. 直接给 `ListBox` 设置：
   - `ScrollViewer.HorizontalScrollBarVisibility="Auto"`
   - `ScrollViewer.VerticalScrollBarVisibility="Disabled"`
3. 保持 `ItemsPanelTemplate` 为横向 `StackPanel`，让标签自然按水平方向扩展。
4. 通过 `ListBox.Styles` 定位模板内的横向 `ScrollBar` 和 `Thumb`，把高度压缩到较小像素值。
5. 当前实现选择“细条可见”的方案，而不是完全隐藏滚动条，原因是它更容易被用户发现可滚动。

## 横向滚动样式建议

- 优先只压缩横向 `ScrollBar` 的高度，例如 `4` 像素。
- `Thumb` 颜色复用 `Brushes.axaml` 中已有画刷，避免局部硬编码颜色。
- `pointerover` 时再提高透明度或对比度，平时保持弱存在感，减少对标签内容的干扰。
- 若后续确实要做“可滚动但不显示滚动条”，可以继续沿用同一结构，只把横向滚动条改成隐藏策略；但需要额外评估鼠标、触控板和触摸场景下的可发现性。

## 实现要点

1. 在标签 `Border` 上挂载 `ContextMenu`。
2. 添加 `MenuItem`，通过 `Click` 事件处理。
3. 在事件中从 `MenuItem.DataContext` 取到当前 `EditorModel`。
4. 从 `EditorModel.FileInfo` 获取文件路径。
5. Windows 下使用：`explorer.exe /select,"<fullPath>"` 定位文件。

## 关键代码思路

- 事件处理匹配模式建议：
  - `sender is MenuItem { DataContext: EditorModel { FileInfo: { } fileInfo } }`
- 保护性判断：
  - 文件不存在时直接返回
  - 非 Windows 平台直接返回（避免平台 API 误用）

## 注意事项

- `UseShellExecute = true` 更符合桌面环境调用外壳程序的用法。
- 若后续要支持 Linux/macOS，可扩展为按平台分别调用系统文件管理器。
