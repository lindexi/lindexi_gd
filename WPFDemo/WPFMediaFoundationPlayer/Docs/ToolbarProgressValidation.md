# 本轮进度条、底部更多及缩放核查

范围仅 WPFMediaFoundationPlayer，未修改 Core 缩放管线、TFM 或包。已读目录 README、TESTING 及相关源码；未找到 AGENTS/instructions/global.json。

## 根因与修改

- Controls.xaml 已有 PART_Track，不能归因为缺少 Track。原轨道是 DecreaseLarge/IncreaseLarge RepeatButton，并非点击位置定位；自定义模板可命中轨道仅为细线，外围未有透明背景。
- MediaPlayerControl 新增轨道预览鼠标按下：按 Track.ValueFromPoint 立即 seek，然后把 MouseDown 委托真实 Thumb 捕获，支持同一次按下继续拖动；不使用单独 IsMoveToPointEnabled。拖动期间不接收轮询值，释放提交改变的值；取消不提交，重复目标不重复 seek。
- DragStarted/Completed 接收已处理事件，重模板/Dispose 断开，Unloaded 取消捕获但不释放播放器；Escape 取消拖动。按钮保留自身键盘操作，Slider 保留原生键盘连续操作。Track 范围/方向/双向 Value 显式绑定，Frame 透明背景扩大轨道命中区域。
- 新增有 XML 文档的 ToolbarContent DP。默认底部工具栏使用 WrapPanel；Demo 只提供更多按钮，不复制控制行。复用动态 PlayerButtonStyle、现有 DemoMoreText、ToolTip 和自动化名称。诊断提示独立放在底部，不叠在视频 HWND 上；实验及诊断菜单不变，ContextMenu 锚定更多按钮，继承播放器禁用状态。

## 缩放实现（未修改）

位置均相对于本目录：

- MediaFoundation.Core/EvrPrototypePresenter.cs:340-365：Negotiate 选择 mixer 提供的 RGB32 类型，Width/Height 取 MF_MT_FRAME_SIZE。不按窗口尺寸重新设定输出，也没有强制从输入视频尺寸生成输出类型。因此准确说是“mixer 协商输出分辨率”，常见源尺寸但不能保证任意媒体必等于编码原分辨率。本次样本为 640x360。
- 同文件 :368-385：display、latest staging 和 sample pool 都为 Width×Height 的 X8R8G8B8 render target。
- 同文件 :568-569、:489-491：sample→latest→display 均为全表面、同尺寸 StretchRect，D3DTEXF_NONE，并等待 GPU 完成；不是面向窗口尺寸的缩放。
- 同文件 :188-195：GetNativeVideoSize 的 native 与 aspect 都回报 Width×Height；:214-225 拒绝非全帧 source crop，destination 仅存储；:234-237 aspect mode 只接受并存储 1。没有显式读取/应用 geometric/minimum display aperture、像素宽高比或 DAR。mixer 自身可能处理部分媒体属性，但此 presenter 未保证非方形像素、有效裁剪区域或变形素材的显示比例正确。
- MediaFoundation.Wpf/MfPlayD3DImagePresenter.cs:54：Image.Source=D3DImage，Stretch=Uniform；:90-99 SetBackBuffer 指向原 display，AddDirtyRect 使用输出像素尺寸。未按窗口/DPI 重建表面，未设置自定义 bitmap scaling mode。Generic.xaml:9 将 VideoView 拉伸到视频布局区域。
- 最终等比适配发生在 WPF Image 布局/合成层：可用 DIP 区域结合设备 DPI 得到屏幕像素尺寸，Uniform 保持的是表面宽高比，不是额外校正后的 DAR。放大不增加源细节，缩小会重采样；高 DPI/非整数缩放可能变软，不承诺逐像素或特定高质量滤镜。
- 性能：窗口缩小不会减少解码、mixer 输出及两次全分辨率复制成本；表面/带宽随协商像素数增长。GPU 同步、8ms UI timer、D3DImage 合成及软件 WPF 回退也有成本。本次未做性能基准，不证明硬件合成路径性能。

## 实际验证与边界

Release Player slnx 构建；win-x64 self-contained 发布。现有 self-test 增加真实 user32 鼠标/键盘输入，不用设置 Value 代替鼠标测试：轨道按下立即公开 Position 定位、真实 Thumb capture、点击续拖、直接 Thumb 命中、按住跨轮询、释放定位。额外 Escape 取消不 seek、连续 Right 键最终位置在 D3DImage 最终运行通过。

日志与截图：
- results/toolbar-progress-hwnd-final/self-test.log：退出 0；默认 HWND（自测原有软件 WPF 设置），真实点击/拖动、模式切换、暂停连续 seek/恢复/close-reopen、80 次 resize、全屏、640 宽布局、更多动态样式/可访问名称/禁用继承/弹出锚点通过。该次未包含后来新增的 Escape/方向键用例。
- results/toolbar-progress-d3dimage-final/self-test.log：退出 0；--d3dimage --software-wpf --seek-regression，同上并通过 Escape/方向键用例。WPF 像素采样约 5497 种颜色，不以 frame counts 当画面。
- results/toolbar-progress-d3dimage/visual-normal.png、visual-narrow.png、visual-more-narrow.png 经独立图像检查：可辨认人物/画架/电脑画面，底部更多风格一致，窄窗口换行无明显重叠，状态不覆盖视频。菜单弹出为系统浅色样式；所给截图未完整包含菜单右边界，不能据此证明所有屏幕边缘条件。

未做：x86、本轮完整长自测/速率扫描、硬件 WPF 合成、多 DPI、所有边缘弹出位置、持键自动重复、Unloaded/重模板/Dispose 中途拖动的输入自动化、声音回采与 A/V 同步、性能基准。实现有相应清理逻辑，不把源码检查称作运行验证。

发布刷新目标：publish/mfplay-unified 与 publish/mfplay-gui-fix，均使用相同当前 Release 源码及 win-x64 参数，IncludeSelfTestMedia=true 供现有沙箱测试使用。
