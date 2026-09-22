# DotNetHostDemo

这些项目演示如何通过 `DotNetHost.Win32Metadata` 10.0.2.1 和 hostfxr，从不同宿主加载普通 .NET 10 程序集。

## 项目

- `ManagedComponent.Alpha`、`ManagedComponent.Beta`：被宿主加载的 .NET 10 组件。
- `FrameworkHost`：.NET Framework 4.8.1 宿主，无交互地运行 Alpha。
- `NativeAotHost`：Native AOT 宿主，运行 Alpha，并把启动参数传给组件。
- `NativeAotRouter`：Native AOT 宿主，根据第一个启动参数选择 Alpha 或 Beta。

所有宿主固定为 Windows x64。机器需要安装 .NET 10 SDK 和 .NET Framework 4.8.1 Developer Pack。

## 构建和运行

构建解决方案：

```powershell
dotnet build .\DotNetHostDemo.slnx -c Release
```

运行 .NET Framework 示例：

```powershell
.\FrameworkHost\bin\Release\net481\FrameworkHost.exe
```

发布并运行 Native AOT 示例：

```powershell
dotnet publish .\NativeAotHost\NativeAotHost.csproj -c Release
.\NativeAotHost\bin\Release\net10.0\win-x64\publish\NativeAotHost.exe one two
```

按参数选择组件：

```powershell
dotnet publish .\NativeAotRouter\NativeAotRouter.csproj -c Release
.\NativeAotRouter\bin\Release\net10.0\win-x64\publish\NativeAotRouter.exe alpha one two
.\NativeAotRouter\bin\Release\net10.0\win-x64\publish\NativeAotRouter.exe beta three four
```

路由器的第一个参数是组件名，剩余参数原样传入被加载组件。组件返回值会成为宿主进程退出码，因此 Alpha 返回 101，Beta 返回 202。
