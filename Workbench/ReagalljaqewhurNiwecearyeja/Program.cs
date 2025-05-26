using Microsoft.NET.HostModel.AppHost;

Console.WriteLine("Hello");

var appHost = @"C:\Program Files\dotnet\sdk\9.0.203\AppHostTemplate\apphost.exe";

HostWriter.CreateAppHost
(
    // AppHost 可从 .NET SDK 里面拷贝，也可以从 NuGet 缓存里面找找
    // 如 .NET x64 的在 C:\Program Files\dotnet\sdk\<SDK VERSION>\AppHostTemplate\apphost.exe
    // 龙芯的一般要自己下，下载地址： https://ftp.loongnix.cn/dotnet/8.0.14/8.0.14-1/deb/dotnet-apphost-pack-8.0_8.0.14-1_loongarch64.deb
    appHostSourceFilePath: appHost,
    // 输出路径，包括指定输出文件名
    appHostDestinationFilePath: "ConsoleApp1.exe",
    // 入口的 DLL 是哪一个，这是相对于 exe 所在的 dll 路径
    appBinaryFilePath: @"Foo\ReagalljaqewhurNiwecearyeja.dll",
    // 是否传入拷贝资源的程序集，如拷贝图标产品信息等等的程序集
    assemblyToCopyResorcesFrom: null,
    // 是否是 GUI 程序。为 false 代表控制台，可以显示控制台内容。为 true 隐藏控制台，为传统的桌面应用程序，如 WinForms 或 WPF 应用
    windowsGraphicalUserInterface: false
);