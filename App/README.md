# ImageViewer

ImageViewer 是一个基于 Avalonia 的跨平台图片查看器示例项目，目标框架为 .NET 10。项目用于演示桌面端图片查看、缩放、旋转、目录浏览以及 Linux deb 包发布等能力。

## 功能特性

- 支持打开单张图片，并自动读取同目录下的其它图片。
- 支持 JPEG、PNG、BMP、GIF、WebP、TIFF 图片格式。
- 支持上一张、下一张、第一张、最后一张图片导航。
- 支持适应窗口、1:1 实际大小、鼠标滚轮缩放和拖拽平移。
- 支持图片向左、向右旋转 90 度。
- 支持全屏查看，全屏后工具栏和状态栏会自动隐藏。
- 状态栏显示当前图片名称、序号、尺寸、文件大小、格式和缩放状态。

## 技术栈

- .NET 10
- Avalonia 12
- Avalonia Fluent 主题
- Packaging.DebUOS

## 项目结构

```text
ImageViewer
├── Code                  # 应用源码
│   ├── Models            # 图片信息模型
│   ├── Services          # 图片目录、缩放和状态服务
│   ├── Icons             # 应用图标资源
│   ├── App.axaml         # Avalonia 应用定义
│   ├── MainWindow.axaml  # 主窗口界面
│   └── Program.cs        # 应用入口
├── Docs                  # 需求、设计和测试相关文档
└── README.md             # 项目说明
```

## 本地运行

进入项目源码目录：

```bash
cd Code
```

还原依赖并运行：

```bash
dotnet restore
dotnet run
```

也可以在启动时传入图片路径：

```bash
dotnet run -- "D:\Pictures\sample.png"
```

## 常用操作

### 工具栏

- `打开`：选择并打开图片文件。
- `←` / `→`：切换上一张或下一张图片。
- `适应窗口`：按当前窗口大小显示完整图片。
- `1:1`：按图片实际像素尺寸显示。
- `↺` / `↻`：向左或向右旋转图片。
- `全屏`：进入或退出全屏查看。

### 鼠标操作

- 鼠标滚轮：切换上一张或下一张图片。
- `Ctrl` + 鼠标滚轮：以鼠标位置为中心缩放图片。
- 鼠标左键拖拽：在图片大于视口时平移图片。
- 双击图片区域：在适应窗口和 1:1 实际大小之间切换。

### 快捷键

| 快捷键 | 功能 |
| --- | --- |
| `Ctrl` + `O` | 打开图片 |
| `←` / `→` | 上一张 / 下一张 |
| `Home` / `End` | 第一张 / 最后一张 |
| `Ctrl` + `0` | 适应窗口 |
| `Ctrl` + `1` 或 `Ctrl` + `9` | 1:1 实际大小 |
| `Ctrl` + `+` / `Ctrl` + `-` | 放大 / 缩小 |
| `Ctrl` + `,` / `Ctrl` + `.` | 向左 / 向右旋转 |
| `F11` 或 `F` | 切换全屏 |
| `Esc` | 退出全屏 |
| `Ctrl` + `Q` | 关闭应用 |

## 构建

在 `Code` 目录下执行：

```bash
dotnet build -c Release
```

## 发布说明

发布 Linux deb 包的方法是在发布命令中带上 `-t:CreateDebUOS` 参数。如对此感兴趣，请阅读 [Packaging.DebUOS 专门为 dotnet 应用制作 UOS 安装包 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/17995729)。

发布龙芯版本需要添加新世界包源：

```bash
dotnet nuget add source -n lnuget.loongnix.cn --protocol-version 3 https://lnuget.loongnix.cn/v3/index.json
```

添加完成后，如果遇到了以下错误：

```text
error NU1302: 你使用的 NuGet 源 "https://lnuget.loongnix.cn/v3/index.json" 包含 "HTTP" 服务索引资源终结点: "http://lnuget.loongnix.cn/v3/package/microsoft.netcore.app.runtime.linux-loongarch64/index.json"。这是不安全的，不建议这样做。若要允许 HTTP 资源，必须在 NuGet.Config 文件中将 "allowInsecureConnections" 显式设置为 true。有关详细信息，请访问 https://aka.ms/nuget-https-everywhere。
```

需要参照 [dotnet 解决使用本地不安全 http 的 NuGet 源 NU1803 警告或构建失败问题 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18312649) 这篇博客的内容进行修复。

示例发布命令：

```bash
dotnet publish -r linux-loongarch64 -c release --self-contained true -t:CreateDebUOS
```

完成发布之后，可以在 `artifacts\publish\ImageViewer` 里面找到发布内容，取出 deb 包即可。

## 参考文档

- [Avalonia 官方文档](https://docs.avaloniaui.net/)
- [Packaging.DebUOS 专门为 dotnet 应用制作 UOS 安装包 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/17995729)
- [dotnet 解决使用本地不安全 http 的 NuGet 源 NU1803 警告或构建失败问题 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18312649)
- [使用 PublishAotClang 轻松交叉编译 Linux Native AOT - jiulang - 博客园](https://www.cnblogs.com/kewei/p/20887495 )

---

## 待办

- 触控优化
  - 双指缩放