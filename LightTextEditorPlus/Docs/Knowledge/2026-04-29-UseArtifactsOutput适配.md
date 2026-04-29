# UseArtifactsOutput 适配

## 背景与目标

本次任务的目标是让仓库默认启用 `UseArtifactsOutput` 构建输出布局，并修复仓库内仍然依赖传统 `bin`、`obj`、`publish` 路径的项目配置，确保构建与测试在新输出布局下仍可正常运行。

## 关键文件与模块

- `Directory.Build.props`
- `Demos/NativeAotLayerTestDemo/NativeAotLayerTestDemo.csproj`
- `Tests/LightTextEditorPlus.Tests/LightTextEditorPlus.Tests.csproj`
- `Tests/LightTextEditorPlus.Avalonia.Tests/LightTextEditorPlus.Avalonia.Tests.csproj`

## 主要决策与原因

1. 在仓库级 `Directory.Build.props` 中启用 `UseArtifactsOutput`，并显式设置 `ArtifactsPath`，让 Visual Studio 与命令行构建都使用统一的 `artifacts` 根目录。
2. 对仍然需要跨项目读取编译产物的项目，不直接硬编码新的 artifacts 路径，而是保留原有路径作为回退，并在 `UseArtifactsOutput=true` 时切换到 artifacts 路径，降低兼容性风险。
3. Avalonia/WPF 测试项目复制卫星资源程序集时，按 artifacts 布局推导 `artifacts/bin/<Project>/<configuration>_<tfm>/en-US/*.resources.dll` 路径，避免测试输出缺失本地化资源。
4. NativeAot 测试演示项目改为按开关切换到 `artifacts/publish/...`，以兼容 `PublishAot` 项目的发布输出目录变化。

## 修改点摘要

- 在 `Directory.Build.props` 中新增 `UseArtifactsOutput` 与 `ArtifactsPath` 默认值。
- 为 `NativeAotLayerTestDemo` 增加 `SkiaAotPublishOutputPath` 属性，根据是否启用 artifacts 输出选择 DLL 来源目录。
- 为 `LightTextEditorPlus.Tests` 增加 `WpfResourceAssemblyPath` 属性，适配 WPF 项目在 artifacts 输出下的 `en-US` 卫星程序集位置。
- 为 `LightTextEditorPlus.Avalonia.Tests` 增加 `AvaloniaResourceAssemblyPath` 属性，适配 Avalonia 项目在 artifacts 输出下的 `en-US` 卫星程序集位置。

## 验证方式（构建/测试/手工验证）

- 运行：`dotnet build .\LightTextEditorPlus.sln -c Release`
  - 结果：成功，输出已进入 `artifacts` 目录。
- 运行：`dotnet test .\Tests\LightTextEditorPlus.Tests\LightTextEditorPlus.Tests.csproj -c Release --no-build`
  - 结果：总计 80，成功 80，失败 0。
  - 说明：测试宿主输出了一段 WPF 关闭期异常堆栈，但命令最终成功，属于既有 UI 测试噪声，不影响本次适配结果。
- 运行：`dotnet test .\Tests\LightTextEditorPlus.Avalonia.Tests\LightTextEditorPlus.Avalonia.Tests.csproj -c Release --no-build`
  - 结果：总计 111，成功 110，跳过 1，失败 0。

## 后续建议

1. 若后续继续新增跨项目复制产物的项目，优先通过属性推导 artifacts 路径，不再直接写死 `bin` 或 `publish` 目录。
2. 可后续再检查 AllInOne 项目中 `Exclude="..\..\obj\**;..\..\bin\**"` 这类源码排除规则是否需要额外兼容 `artifacts/obj`，避免未来源包含范围扩大时误收集中间产物。
3. 若仓库后续增加打包或发布脚本，建议统一改为显式使用 `artifacts` 目录，减少对默认输出布局的隐式假设。
