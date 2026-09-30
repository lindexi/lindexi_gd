# DotNetCampus.CommandLine 使用与踩坑记录

本文记录 `XiaoXiIme.Cli` 使用 `DotNetCampus.CommandLine` 进行命令行解析的实践，适用于希望使用命令模型、源生成器、内置帮助和异步命令处理的开发者。

当前项目使用：

- .NET 10
- `DotNetCampus.CommandLine` 4.2.0
- 顶层语句作为程序入口
- Native AOT 发布场景

## 1. 基本结构

推荐将代码分为两部分：

- `Program.cs`：程序入口、处理器注册和命令执行逻辑。
- `CommandOptions.cs`：使用特性声明的命令模型。

项目当前采用以下入口结构：

```csharp
if (args.Length == 0)
{
    args = ["--help"];
}

return await CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<InstallOptions>(Install)
    .AddHandler<SystemTestRunOptions>(RunSystemTests)
    .RunAsync();
```

要点：

1. `CommandLine.Parse(args)` 解析命令行。
2. `AddHelpHandler()` 启用内置帮助。
3. `AddHandler<T>()` 注册命令模型及其处理逻辑。
4. 如果任一处理器是异步处理器，使用 `RunAsync()`。
5. 顶层语句本身支持 `await`，不需要使用 `GetAwaiter().GetResult()`。

## 2. 定义命令

使用 `[Command]` 将类型映射到命令名称：

```csharp
[Command("install", Description = "Install the IME.")]
internal sealed class InstallOptions
{
    [Value(0, Description = "Full path to the .ime file.")]
    public string? ImeFile { get; init; }
}
```

调用形式：

```text
XiaoXiIme.Cli install C:\Program Files\XiaoXiIme\XiaoXiIme.ime
```

命令名称应使用 kebab-case，例如：

- `install`
- `install-checklist`
- `system-test-run`

不要在特性中写 `--install`。`[Command]` 接收的是命令名称，不是选项名称。

## 3. 位置参数

使用 `[Value(index)]` 声明位置参数：

```csharp
[Value(0)]
public string? AbiHost { get; init; }

[Value(1)]
public string? TsfDll { get; init; }
```

对应命令：

```text
XiaoXiIme.Cli system-test-run AbiHost.exe XiaoXiIme.Tsf.dll
```

索引从 `0` 开始。

### 必填参数如何建模

可以使用 `required` 表达解析层面的必填约束：

```csharp
[Value(0)]
public required string InputFile { get; init; }
```

如果项目需要自行控制错误消息和退出码，也可以使用可空属性，然后在处理器中验证：

```csharp
if (string.IsNullOrWhiteSpace(options.ImeFile))
{
    Console.Error.WriteLine("The install command requires the full path to an .ime file.");
    return 2;
}
```

两种方式的差异是：

- `required`：由库负责缺失参数错误，适合接受库默认错误行为的场景。
- 可空属性加业务验证：可以精确控制提示文本和退出码。

## 4. 命名选项

使用 `[Option]` 定义命名选项：

```csharp
[Option("json", Description = "Write the plan as JSON.")]
public bool Json { get; init; }
```

调用形式：

```text
XiaoXiIme.Cli system-test-plan --json
```

带值的选项：

```csharp
[Option("report", Description = "Path to the generated report.", ValueName = "file")]
public string? Report { get; init; }
```

调用形式：

```text
XiaoXiIme.Cli system-test-run host.exe tsf.dll --report report.json
```

`ValueName` 只影响帮助信息中的占位符显示。例如帮助文本会显示 `--report <file>`，而不是默认的 `--report <value>`。

也可以同时声明短选项和长选项：

```csharp
[Option('o', "output")]
public string? Output { get; init; }
```

## 5. 注册同步处理器

同步处理器可以返回 `int` 作为进程退出码：

```csharp
.AddHandler<InstallOptions>(Install)
```

```csharp
static int Install(InstallOptions options)
{
    // 执行业务逻辑
    return 0;
}
```

也可以使用内联委托：

```csharp
.AddHandler<PublishChecklistOptions>(_ =>
{
    PrintPublishChecklist();
    return 0;
})
```

约定上：

- `0` 表示成功。
- 非零值表示失败或特定错误。
- 错误信息写入 `Console.Error`。
- 正常结果写入 `Console.Out` 或使用 `Console.WriteLine`。

## 6. 注册异步处理器

异步处理器可以返回 `Task<int>`：

```csharp
.AddHandler<SystemTestRunOptions>(RunSystemTests)
```

```csharp
static Task<int> RunSystemTests(SystemTestRunOptions options)
{
    return SystemTestRunner.RunAsync(...);
}
```

入口直接等待：

```csharp
return await CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<SystemTestRunOptions>(RunSystemTests)
    .RunAsync();
```

不要写成：

```csharp
.RunAsync().GetAwaiter().GetResult();
```

在现代 C# 顶层语句中没有必要同步阻塞异步调用。直接使用 `await` 更清晰，也避免同步等待可能产生的问题。

只要处理器链中存在异步处理器，就应使用 `RunAsync()`。如果所有处理器都是同步处理器，可以使用 `Run()`。

## 7. 内置帮助

`DotNetCampus.CommandLine` 自带帮助生成机制，不需要手写 `help` 命令、`PrintHelp` 方法或 `switch` 分支。

启用方式：

```csharp
CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<InstallOptions>(Install)
    .RunAsync();
```

默认 Flexible 风格可以识别：

- `--help`
- `-h`
- `/help`
- `/h`
- `/?`
- `-?`

建议为命令、位置参数和选项填写 `Description`：

```csharp
[Command("export-checklist", Description = "Print export verification commands.")]
internal sealed class ExportChecklistOptions
{
    [Value(0, Description = "Path to the IME binary.")]
    public string? ImeFile { get; init; }
}
```

### 无参数时显示帮助

内置帮助会响应帮助标志，但应用是否将“无参数”解释为帮助，可由入口自行决定：

```csharp
if (args.Length == 0)
{
    args = ["--help"];
}
```

这比额外创建一个 `[Command("help")]` 类型更直接。

### 帮助处理器的注册位置

本项目将 `AddHelpHandler()` 紧跟在 `Parse(args)` 后面：

```csharp
CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<...>()
```

这是经过当前项目验证的写法，也能清楚表达“先启用全局能力，再注册业务命令”的意图。

需要注意：在 4.2.0 的实际验证过程中，将 `AddHelpHandler()` 放在已经转为异步处理器链的末尾时，曾观察到 `system-test-plan --help` 仍执行了命令处理器。将帮助处理器直接注册在 `Parse` 后可以避免这一问题。升级库版本后应重新验证帮助行为。

## 8. 命令与处理逻辑是否需要放在同一个类

不需要。

简单 CLI 可以让命令模型只负责描述参数，让 `Program.cs` 中的本地函数负责业务处理：

```csharp
.AddHandler<InstallOptions>(Install)
```

这种方式适合：

- 程序规模较小。
- 处理逻辑只在入口使用。
- 不需要依赖注入容器。
- 希望清晰看到所有命令注册关系。

较复杂的命令也可以实现 `ICommandHandler`，把参数和处理逻辑封装在同一个类型中。但不应为了使用库而无条件增加包装类、工厂接口或没有明确职责的 Application 类型。

## 9. 源生成器与顶层语句的坑

`DotNetCampus.CommandLine` 使用源生成器和拦截器生成高性能解析代码。使用 4.2.0 时，本项目发现一个需要特别注意的问题：

> 不要把带 `[Command]`、`[Value]`、`[Option]` 的命令模型声明在包含顶层语句的 `Program.cs` 文件中。

曾出现的生成错误包括：

```text
CS0111: 类型“<invalid-global-code>”已定义了一个名为“BuildCore”的具有相同参数类型的成员
CS0111: 类型“<invalid-global-code>”已定义了一个名为“AssignPropertyValue”的具有相同参数类型的成员
```

错误来自生成目录中的文件，例如：

```text
DotNetCampus.CommandLine.Generators.ModelBuilderGenerator\CommandLine.Models\...
```

解决方式是：

- `Program.cs` 保留顶层入口和处理逻辑。
- 将命令模型移动到独立的 `CommandOptions.cs`。
- 为模型设置正常的文件作用域命名空间。

```csharp
namespace XiaoXiIme.Cli;

[Command("install")]
internal sealed class InstallOptions
{
    // ...
}
```

这不是要求创建额外的应用包装层，只是为源生成器提供稳定的类型和命名空间上下文。

升级 `DotNetCampus.CommandLine` 后，可以重新验证该限制是否仍然存在，但不要未经验证就把模型移回顶层语句文件。

## 10. 不要重复手写解析逻辑

迁移到库之后，应删除以下手写模式：

```csharp
var command = args[0];
switch (command)
{
    // ...
}
```

也不应继续手写选项扫描：

```csharp
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--report")
    {
        return args[i + 1];
    }
}
```

这些职责应分别交给：

- `[Command]`：命令匹配。
- `[Value]`：位置参数。
- `[Option]`：命名选项。
- `AddHelpHandler()`：帮助参数。

否则项目会同时存在两套解析规则，容易产生参数兼容性、大小写、分隔符和错误处理不一致的问题。

## 11. 避免没有实际价值的抽象

本次迁移初版曾引入以下设计：

- `CliApplication.Run(...)`
- `TextWriter output`
- `TextWriter error`
- `Func<IImeInstaller> createInstaller`
- `IImeInstaller`
- `RunAsync().GetAwaiter().GetResult()`

对于当前 CLI，这些抽象没有提供足够价值，反而：

- 隐藏了真正的程序入口。
- 增加了参数传递。
- 让异步入口被迫同步阻塞。
- 为单一实现创建了不必要的接口和工厂。
- 让库自带的帮助功能被手写逻辑替代。

当前项目最终采用：

- 顶层 `Program.cs` 直接注册和执行命令。
- 直接使用 `Console.Out`、`Console.Error`。
- 直接创建单一安装器实现。
- 直接 `await RunAsync()`。
- 使用库内置帮助。

抽象应由真实需求驱动，例如存在多个实现、需要独立复用、需要替换外部资源或确实需要单元测试边界时再引入。

## 12. Native AOT 和平台判断

当前 CLI 的最终发布目标是 Windows Native AOT 可执行文件，并调用 Windows IME API。

在这种明确限定发布平台的项目中，运行时再次检查：

```csharp
OperatingSystem.IsWindows()
```

通常没有实际价值，因为发布产物和 P/Invoke 本身已经限定平台。项目因此移除了这层冗余判断。

但这不是所有项目的通用规则。如果同一个托管程序集会跨平台运行，或者 NuGet 包会被其他平台引用，则仍应保留平台保护或使用平台特性标注。

## 13. Native AOT 注意事项

`DotNetCampus.CommandLine` 的主要优势之一是通过源生成器避免运行时反射，适合 Native AOT。

仍需注意：

1. 构建和发布时检查所有 AOT、裁剪警告。
2. 不要在命令处理逻辑中随意引入依赖运行时反射的功能。
3. 命令模型应保持简单、明确和静态可分析。
4. 修改命令模型后必须重新构建，不能只依赖旧的 `--no-build` 输出验证。
5. 如果源生成器报错，应检查生成目录中的错误类型和方法名，而不是通过禁用分析器隐藏问题。

## 14. 验证建议

每次新增或修改命令后，至少验证：

### 构建

```powershell
dotnet build
```

### 全局帮助

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- --help
```

### 命令正常执行

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- system-test-plan --json
```

### 缺少参数

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- install
```

### 未知命令或选项

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- unknown-command
```

### 帮助标志

至少检查：

```powershell
--help
-h
/?
```

如果使用命令级帮助，也应单独验证：

```powershell
system-test-plan --help
```

特别注意：不要在修改代码后立即使用 `dotnet run --no-build` 验证，否则可能运行旧产物并得到误导结果。应先确认项目已经重新生成成功。

## 15. 推荐实践清单

- 使用 kebab-case 命令和长选项名称。
- 使用 `[Command]`、`[Value]`、`[Option]` 描述命令行结构。
- 为命令和参数填写 `Description`。
- 使用 `ValueName` 改善帮助占位符。
- 使用 `AddHelpHandler()`，不要手写帮助解析。
- 有异步处理器时直接 `await RunAsync()`。
- 保持退出码稳定，并区分标准输出和错误输出。
- 顶层入口放在 `Program.cs`，命令模型放在独立文件。
- 不要同时保留手写 `args` 解析和库解析。
- 不要为单一实现提前创建接口、工厂和无明确职责的包装类型。
- 修改源生成模型后进行完整构建和真实命令冒烟验证。
- Native AOT 项目应持续检查生成器、裁剪和 AOT 警告。
