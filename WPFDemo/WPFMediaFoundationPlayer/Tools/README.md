# 文件组织工具

使用 .NET 10 的 file-based app，无需独立 csproj。执行一次：

```text
dotnet run --file c:\lindexi\Code\Workspace\WPFMediaFoundation\WPFMediaFoundationPlayer\Tools\OrganizeFiles.cs
```

根目录由脚本源文件位置确定，与启动工作目录无关。工具按固定清单移动 20 个文件，并更新 ToolbarProgressValidation.md 的两处源码路径。没有路径预检、链接检查、备份、异常拦截或重复执行兼容；移动失败由 File.Move 直接抛出。不要重复执行。

## 目标结构

```text
WPFMediaFoundationPlayer/
├─ MediaFoundation.Core/
│  ├─ Playback/
│  │  ├─ MediaPlaybackSession.cs
│  │  ├─ NativeCallback.cs
│  │  ├─ EvrPrototypePresenter.cs
│  │  ├─ MediaEngineSession.cs
│  │  └─ EngineCallback.cs
│  ├─ IPlaybackSession.cs
│  ├─ PlaybackState.cs
│  ├─ PlaybackFailedEventArgs.cs
│  ├─ VideoScalingMode.cs
│  ├─ VideoSurfaceInfo.cs
│  ├─ SourceDiagnostics.cs
│  ├─ SR.cs
│  ├─ Strings.resx
│  ├─ NativeMethods.txt / NativeMethods.json
│  ├─ README.md
│  └─ MediaFoundation.Core.csproj
├─ MediaFoundation.Wpf/
│  ├─ Rendering/
│  │  ├─ VideoHost.cs
│  │  ├─ MfPlayD3DImagePresenter.cs
│  │  ├─ D3DImagePresenter.cs
│  │  └─ SharedVideoSurface.cs
│  ├─ MediaPlayer.cs
│  ├─ MediaPlayerControl.cs
│  ├─ VideoRenderingMode.cs
│  ├─ Text.cs / Strings.resx
│  ├─ Themes/（保持原位）
│  ├─ Properties/（保持原位）
│  ├─ NativeMethods.txt / NativeMethods.json
│  ├─ README.md
│  └─ MediaFoundation.Wpf.csproj
├─ MediaFoundation.PlayerDemo/
│  ├─ Diagnostics/
│  │  ├─ PlaybackDiagnostics.cs
│  │  ├─ MediaProbe.cs
│  │  ├─ MatroskaTracks.cs
│  │  └─ MfPlayPrototype.cs
│  ├─ Testing/
│  │  ├─ SelfTest.cs
│  │  ├─ ScalingTest.cs
│  │  ├─ SourcePathTest.cs
│  │  ├─ VisualTest.cs
│  │  ├─ MediaProbeTest.cs
│  │  └─ LoopbackProbe.cs
│  ├─ App.xaml / App.xaml.cs
│  ├─ RenderingModeMenu.cs
│  ├─ NativeMethods.txt / NativeMethods.json
│  └─ MediaFoundation.PlayerDemo.csproj
├─ Docs/
│  ├─ MEDIA-DIAGNOSTICS.md（从 Demo 移入）
│  └─ 其他现有文档保持原位
├─ Tools/
│  ├─ OrganizeFiles.cs
│  └─ README.md
├─ README.md
└─ WPFMediaFoundationPlayer.slnx
```

只移动物理文件，不改 namespace、公开 API、程序集名称或三个项目的位置。SDK 默认 Compile glob 包含子目录源码。resx、XAML、CsWin32 输入留在原位，因此无需修改资源名称、pack URI 和项目引用。bin、obj、运行日志及发布产物不参与迁移。

执行后验证：

```text
dotnet build c:\lindexi\Code\Workspace\WPFMediaFoundation\WPFMediaFoundationPlayer\WPFMediaFoundationPlayer.slnx -c Release
```

普通 Demo 启动命令不变。
