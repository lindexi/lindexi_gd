# XiaoXiIme.ImeUi.Avalonia

小希输入法外部 UI 项目。职责：实现候选窗、状态窗、设置界面和主题系统，并通过 Host 接收状态、候选和位置信息。

## 独立词库调试

不带参数启动应用，打开正常窗口，上方只读 TextBox 显示提交结果，下方只读 TextBox 显示带时间的调试日志，中间可拖动调整高度。日志区支持 Ctrl+A 全选、Ctrl+C 复制；焦点在日志区时不处理词库按键。无需安装输入法或启动 Host；按键交给真实 `ImeContext`，复用候选窗口显示，提交结果仅追加到本窗口，不向其他应用发送输入，也不保存用户学习。

运行：`dotnet run --project src/XiaoXiIme.ImeUi.Avalonia/XiaoXiIme.ImeUi.Avalonia.csproj`。

只加载预编译 package，不读取或编译 TSV、不使用回退词库。默认从应用输出目录加载 `XiaoXiIme.DictionaryPackage`；`XIAOXIIME_INPUT_SCHEME=xiaoheDoublePinyin` 时使用输出目录下的 `XiaoXiIme.DictionaryPackages/xiaoheDoublePinyin`。也可通过环境变量 `XIAOXIIME_DEBUG_PACKAGE` 指定任意已编译 package 目录。修改词库后需另行编译并重启调试应用。加载在后台执行，标题仅显示简短状态；日志区记录实际 package 路径、加载状态、完整异常与堆栈及按键处理结果，不占用结果 TextBox。

支持字母、数字 1–9 选词、空格提交候选、Enter 提交 reading、退格、Esc 取消、上下选择、PageUp/PageDown 翻页、左右移动组合光标、Home/End 选择首末候选及 `/` 符号输入。TextBox 仅接收本地核心的提交结果；窗口与 TextBox 禁用系统输入法通道，拦截 TextInput，不将系统输入法文本送入核心。失去窗口激活时取消组合并隐藏候选窗口，关闭主窗口退出应用。

带参数启动仍使用原有 IPC 模式：第一个参数为管道名称，第二个可选参数为状态文件路径。

当前已有候选状态映射、显示/隐藏、分页、高亮和锚点字段基础。分页和选中状态交互已覆盖固定每页 9 项、末页实际数量、跨页选择、返回上一页、显示索引和唯一高亮同步。尚未完成真实窗口定位与屏幕工作区边界处理，以及多 DPI/缩放比例下的视觉自动化测试；锚点字段或托管状态测试通过不代表候选窗口产品级验收完成。
