# 调试 PresentationBuildTasks 的方法

先打开 `PresentationBuildTasks\PresentationBuildTasks.sln` 文件，接着使用命令行 dotnet build 构建 Walterlv.Demo.XamlProperties 项目，在构建时将会弹出 VisualStudio 附加进程调试窗口，选择使用 PresentationBuildTasks.sln 所在的 VisualStudio 进行调试，下一步按下 F10 就可以看到 PresentationBuildTasks 的源代码