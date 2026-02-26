# TabBar 右键菜单与文件资源管理器联动经验

## 场景

在 `TabBar` 的标签项（`DataTemplate` 内 `Border`）上，右键提供“在文件资源管理器中打开”。

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
