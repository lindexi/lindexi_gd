<!--more-->
<!-- 发布 -->
<!-- 博客 -->

本文内容由人类主导 AI 辅助编写

# dotnet 使用 GitignoreParserNet 实现接近 Git 的分层 gitignore 匹配

本文介绍如何在 .NET 10 项目中使用 `GitignoreParserNet`，从指定扫描目录出发，组合仓库内不同层级的 `.gitignore`，判断文件或文件夹是否应该被排除，并使用真实 Git 验证结果。

## 已实现的规则

当前探索代码实现了以下行为：

- 从一个自身没有 `.gitignore` 的扫描目录开始，向上查找 Git 仓库根目录。
- 加载仓库根目录到各级子目录中的 `.gitignore`。
- 每份 `.gitignore` 中的规则只匹配其所在目录及后代路径。
- 仓库上层的 `.gitignore` 会被扫描目录继承。
- 子目录中的 `.gitignore` 可以覆盖祖先目录的匹配结果。
- 同一份 `.gitignore` 中最后一条匹配规则优先。
- 支持以 `!` 开头的否定规则，将之前排除的路径重新包含。
- 如果父目录本身已被排除，则不能只通过子文件的否定规则重新包含该文件。
- 判断目录时会在路径末尾追加 `/`，区分 `folder` 与 `folder/` 规则。
- 将 Windows 路径分隔符 `\` 转换为 gitignore 使用的 `/`。
- 已探索 `*`、`**`、`?`、字符范围、根锚定、目录规则和连续正反规则。
- 使用 `git check-ignore --verbose --no-index` 对每个结果进行对照验证。
- 当前实验包含 28 个候选路径，探索结果与真实 Git 的判断一致。

// 注: 审核确认这里的“一致”只适用于 `Scenario.Create` 当前构造的 28 个用例，并不表示已经完整兼容标准 Git。补充前导空格加 `#`、尾随空格、根目录否定规则、`[!a]`、非 ASCII `?`、`abc/**`、大小写和符号链接 `.gitignore` 等用例后，当前实现可以观察到差异。

## 核心实现是什么

核心实现不是简单地把一份 `.gitignore` 交给 `GitignoreParserNet`，而是由调用方补充分层规则管理。

整体步骤如下：

1. 从扫描目录向上找到 Git 仓库根目录。
2. 找到仓库中的所有 `.gitignore`，让祖先目录规则排在子目录规则之前。
3. 保留每份 `.gitignore` 的原始行顺序，并将每一条有效规则独立交给 `GitignoreParserNet` 编译。
4. 判断路径时，将路径转换成相对于规则所在目录的路径。
5. 使用 `Inspects` 判断当前规则是否命中，再使用 `Denies` 判断命中结果是排除还是包含。
6. 后命中的规则覆盖之前的结果，以模拟 Git 的“最后匹配规则优先”。
7. 判断文件之前先检查各级父目录；只要某一级父目录已被排除，就直接判定文件被排除。
8. 调用真实 Git 对相同路径进行判断，输出两者差异。

`GitignoreParserNet` 负责单条 gitignore 模式的解析和匹配，探索代码负责仓库发现、规则作用域、层级顺序、父目录阻断和实验验证。这两部分组合起来，才能得到更接近 Git 的行为。

// 注: 标准 Git 的排除机制不只读取逐目录 `.gitignore`。其来源还包括命令行规则、`$GIT_DIR/info/exclude` 和 `core.excludesFile`，并且不同来源之间存在优先级。当前实现只覆盖逐目录 `.gitignore` 这一层，因此这里应理解为“接近 Git 的 `.gitignore` 分层行为”，不是完整 Git 排除引擎。

## 引用 GitignoreParserNet

项目通过 NuGet 引用 `GitignoreParserNet`：

```xml
<ItemGroup>
  <PackageReference Include="GitignoreParserNet" Version="0.2.0.15" />
</ItemGroup>
```

本文使用的主要 API 有三个：

- `Inspects(path)`：是否有规则匹配这个路径。
- `Denies(path)`：匹配结果是否为排除。
- `Accepts(path)`：匹配结果是否为接受。

在分层规则处理中，`Inspects` 很重要。如果子目录的 `.gitignore` 没有匹配当前路径，就不能让它无条件覆盖父目录的判断。

## 从扫描目录向上查找仓库

扫描入口不一定正好放在 `.gitignore` 所在目录。例如扫描入口是 `work/scan`，仓库根目录和 `work` 目录都可能有 `.gitignore`。

程序从当前目录开始逐级向上检查 `.git` 目录：

```csharp
private static string FindRepositoryRoot(string currentDirectory)
{
    DirectoryInfo? directory = new(currentDirectory);
    while (directory is not null)
    {
        if (Directory.Exists(Path.Join(directory.FullName, ".git")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException($"未能从当前目录向上找到 Git 仓库: {currentDirectory}");
}
```

找到仓库根目录以后，才能知道应该从哪里开始收集祖先规则，也可以防止路径判断越过当前仓库边界。

当前代码只判断 `.git` 是否为目录。这适合普通仓库，但 Git worktree 或部分子模块中的 `.git` 可能是文件。如果后续需要支持这些场景，可以同时检查 `File.Exists`，并解析其中的 `gitdir:` 指向。

// 注: 已使用 `git init --separate-git-dir` 实测，标准 Git 可以在工作树中的 `.git` 为文件时正常定位仓库，而当前 `FindRepositoryRoot` 会直接失败。除 worktree 和部分子模块外，Git 还可能通过环境变量等方式指定仓库与工作树，因此生产实现更适合调用 `git rev-parse --show-toplevel`，或完整实现仓库发现规则。

## 加载不同层级的 gitignore

程序递归查找仓库中的 `.gitignore`：

```csharp
string[] gitignoreFiles = Directory.GetFiles(
    rootDirectory,
    ".gitignore",
    SearchOption.AllDirectories);
```

接着按照路径深度排序，让仓库根目录的规则先执行，子目录规则后执行：

```csharp
Array.Sort(gitignoreFiles, static (left, right) =>
{
    int depthComparison = GetDepth(left).CompareTo(GetDepth(right));
    return depthComparison != 0
        ? depthComparison
        : StringComparer.OrdinalIgnoreCase.Compare(left, right);
});
```

同一深度可能存在多个兄弟目录，但兄弟目录的 `.gitignore` 不会同时作用于同一个候选路径。真正匹配时还会检查候选路径是否位于规则目录之下，因此兄弟目录之间不会互相影响。

// 注: 标准 Git 按遍历路径逐级读取有关的 `.gitignore`，不会预先递归扫描整个仓库。当前做法不仅有性能和权限边界，还会读取符号链接形式的 `.gitignore`；Git 明确不会跟随这类符号链接。实测符号链接 `.gitignore` 时，当前实现会应用其中规则，而 Git 不会。

// 注: `_rules` 只在 `LayeredGitignoreMatcher` 构造时加载一次。构造后新增、删除或修改 `.gitignore`，Git 会使用新内容，现有匹配器仍使用旧快照；若用于长期运行的扫描器，需要失效缓存或监听规则文件变化。

## 为什么要逐行构造解析器

最初的实现是把整份 `.gitignore` 内容交给一个 `GitignoreParser`：

```csharp
var parser = new GitignoreParser(content);
```

实验发现，以下规则与 Git 的结果不一致：

```gitignore
order.txt
!order.txt
order.txt
```

Git 按照最后一条匹配规则决定结果，因此 `order.txt` 最终应该被排除。但当前版本的 `GitignoreParserNet` 将整份规则编译后，没有完整保留这种逐行优先级。

探索代码改为按原始顺序读取规则，每条规则独立构造一个解析器：

```csharp
for (int index = 0; index < lines.Length; index++)
{
    string pattern = lines[index];
    if (string.IsNullOrWhiteSpace(pattern) || pattern.TrimStart().StartsWith('#'))
    {
        continue;
    }

    rules.Add(new IgnoreRule(
        baseDirectory,
        gitignorePath,
        index + 1,
        pattern,
        new GitignoreParser(pattern)));
}
```

`IgnoreRule` 同时保存规则所在目录、文件、行号和原始模式：

```csharp
internal sealed record IgnoreRule(
    string BaseDirectory,
    string GitignorePath,
    int LineNumber,
    string Pattern,
    GitignoreParser Parser);
```

这样既继续使用了 `GitignoreParserNet` 的通配符解析能力，又能由调用方按照 Git 的顺序逐条应用规则。输出差异时还可以直接显示决定结果的是哪一个文件的哪一行。

// 注: 这里的预处理已经与 Git 存在确定差异。Git 只把第一个字符就是未转义 `#` 的行当注释；`pattern.TrimStart().StartsWith('#')` 会错误跳过例如 `  #literal.txt` 这类用于匹配前导空格文件名的合法规则。

// 注: 当前依赖版本 `GitignoreParserNet` 0.2.0.15 对规则末尾空格的行为也未完全对齐 Git。实测未转义尾随空格会额外匹配名称末尾带空格的文件，而使用反斜杠保留尾随空格的规则反而没有命中；因此不能只把原始行直接交给单条解析器就视为完整保留了 Git 的规则文本语义。

## 规则只能作用于所在目录的后代

假设规则来自 `work/scan/src/.gitignore`，它不能影响 `work/scan/readme.txt`，也不能影响其他兄弟目录。

因此在应用规则之前，需要确认候选路径位于规则所在目录中：

```csharp
if (!IsUnderOrEqual(fullPath, rule.BaseDirectory))
{
    continue;
}
```

接着把完整路径转换为相对于 `.gitignore` 所在目录的路径：

```csharp
string relativePath = Path
    .GetRelativePath(rule.BaseDirectory, fullPath)
    .Replace('\\', '/');
```

这是分层 gitignore 实现中非常关键的一步。`.gitignore` 中以 `/` 开头的规则，是相对于这份 `.gitignore` 所在目录锚定，而不是相对于磁盘根目录或统一的仓库根目录锚定。

例如 `work/scan/src/.gitignore` 中的规则：

```gitignore
/local-only/
```

应该匹配 `work/scan/src/local-only/`，但不应该匹配 `work/scan/local-only/`。

## 目录路径必须追加斜杠

Gitignore 规则会区分文件和目录：

```gitignore
folder-only/
```

这条规则只表示目录。`GitignoreParserNet` 要求调用方通过路径末尾的 `/` 告诉解析器当前对象是目录：

```csharp
if (isDirectory)
{
    relativePath += '/';
}
```

如果不追加 `/`，目录专用规则可能无法按照预期匹配。因此调用匹配方法时，需要同时提供路径和 `isDirectory` 信息，不能只依赖字符串猜测文件类型。

## 使用 Inspects 和 Denies 应用最后匹配规则

每条规则的判断分为两步：

```csharp
if (!rule.Parser.Inspects(relativePath))
{
    continue;
}

isIgnored = rule.Parser.Denies(relativePath);
decidingRule = rule;
```

`Inspects` 为 `false` 表示当前规则完全没有涉及此路径，此时应保留之前规则的结果。

`Inspects` 为 `true` 表示规则命中，再通过 `Denies` 获取当前规则的决定：

- 普通排除规则的 `Denies` 为 `true`。
- `!` 否定规则的 `Denies` 为 `false`。

规则按照祖先到后代、文件内从上到下的顺序执行。每次命中都覆盖 `isIgnored`，最后留下的自然就是 Git 所要求的最后匹配结果。

// 注: “最后匹配规则优先”在当前 28 个用例中成立，但单条模式本身仍可能与 Git 不同。已实测的差异包括：`abc/**` 会被当前解析器用于排除 `abc` 目录本身，而 Git 只匹配目录内部；`[!a].txt` 在 Git 中可以匹配 `b.txt`，当前解析器未命中；Git 的 `?` 按路径字节匹配，当前解析器按 .NET 字符处理，所以中文、表情等非 ASCII 文件名会得到不同结果。

// 注: 大小写也不是固定语义。Windows 上 `git init` 通常会设置 `core.ignoreCase=true`，实测规则 `Case.txt` 可以匹配 `case.txt`；当前 `GitignoreParserNet` 调用仍区分大小写。反过来，在 `core.ignoreCase=false` 的仓库中 Git 又会区分大小写，因此生产实现需要遵循仓库配置，而不是固定使用一种比较方式。

## 为什么还要检查父目录

下面的规则看起来像是希望保留 `shared/keep.txt`：

```gitignore
shared/
!shared/keep.txt
```

但 Git 不会重新遍历一个已经被排除的目录。因此只取消忽略其中一个文件是不够的，`shared/keep.txt` 仍然会被排除。

程序在判断候选路径之前，先逐级判断它的父目录：

```csharp
DirectoryInfo? ancestor = Directory.GetParent(fullPath);
while (ancestor is not null && IsUnderOrEqual(ancestor.FullName, _rootDirectory))
{
    MatchResult ancestorResult = MatchDirect(ancestor.FullName, isDirectory: true);
    if (ancestorResult.IsIgnored)
    {
        return new MatchResult(
            true,
            $"父目录已排除: {Path.GetRelativePath(_rootDirectory, ancestor.FullName)}");
    }

    ancestor = ancestor.Parent;
}
```

只有所有父目录都允许遍历时，才继续判断文件自身的规则。

// 注: 当前循环还会检查仓库根目录本身，而且 `MatchDirect` 会让根 `.gitignore` 的规则作用到相对路径 `./`。例如根规则为 `*`、`!foo.txt` 时，Git 会重新包含顶层 `foo.txt`，当前实现却先把仓库根目录判为已排除，最终仍排除该文件。子目录 `.gitignore` 的基准目录也有同类问题；父目录阻断检查应停在规则作用域根之前，不能把 `.gitignore` 所在目录本身当成其可排除后代。

如果确实希望重新包含目录中的某个文件，需要先逐级重新包含目录。例如：

```gitignore
shared/*
!shared/keep.txt
```

这里没有排除 `shared` 目录本身，只排除了它的直接内容，因此否定规则才有机会重新包含 `keep.txt`。

## 子目录 gitignore 如何覆盖父目录

假设 `work/.gitignore` 包含：

```gitignore
*.log
```

而 `work/scan/src/.gitignore` 包含：

```gitignore
!important.log
```

判断 `work/scan/src/important.log` 时，执行顺序如下：

1. 相对于 `work/.gitignore` 的路径是 `scan/src/important.log`，命中 `*.log`，暂时判定为排除。
2. 相对于 `work/scan/src/.gitignore` 的路径是 `important.log`，命中 `!important.log`，更新为不排除。
3. 后面没有其他匹配规则，因此最终结果为不排除。

这就是子目录规则覆盖父目录规则的核心。不是把多份 `.gitignore` 简单拼接成字符串，而是保留每份规则自己的基准目录，然后按层级顺序执行。

## 使用真实 Git 作为对照

探索代码会在临时目录中执行 `git init`，然后对每一个候选路径执行：

```text
git check-ignore --verbose --no-index -- <path>
```

参数含义如下：

- `--verbose`：输出命中的 `.gitignore`、行号和规则。
- `--no-index`：即使路径已经被 Git 索引，也继续检查忽略规则。
- `--`：结束选项解析，避免特殊文件名被解释成命令参数。

// 注: `--no-index` 使这个实验比较的是“排除规则是否匹配路径”，而不是 Git 对工作树的最终忽略状态。标准 Git 的 ignore 机制只影响未跟踪文件；已跟踪文件即使命中 `.gitignore`，仍由 Git 管理。实测同一路径在当前匹配器和 `--no-index` 下为忽略，但普通 `git check-ignore` 不会把已跟踪文件报告为忽略。

程序使用 `ProcessStartInfo.ArgumentList` 分别添加参数，避免手工拼接命令行时出现空格和转义问题。

// 注: `GitCheckIgnore.Match` 没有接收 `isDirectory`。当前场景先在磁盘上创建了所有目录，Git 可以根据文件系统识别目录；如果以后验证尚不存在的候选目录，应给传入 Git 的路径显式追加 `/`，否则 `folder/` 一类规则在解析器侧按目录判断、在 Git 侧却可能按文件路径判断。

需要注意，`git check-ignore` 命中否定规则时，退出码仍可能是 `0`。因此不能只看退出码，还要检查详细输出中的规则是否以 `!` 开头：

```csharp
return process.ExitCode switch
{
    0 => new GitMatchResult(!IsNegatedMatch(output), output),
    1 => new GitMatchResult(false, "未被任何规则匹配"),
    _ => throw new InvalidOperationException(
        $"git check-ignore 失败，退出码 {process.ExitCode}: {error}"),
};
```

// 注: 当前 `IsNegatedMatch` 通过查找前两个冒号解析 `--verbose` 文本，在规则来源是 Windows 绝对路径时不可靠，因为盘符本身包含冒号。实测将 `core.excludesFile` 配置为绝对路径并命中 `!kept.txt` 时，Git 表示“不排除”，当前验证代码却会误报为“排除”。机器解析建议改用 `git check-ignore --stdin -z --verbose`，按 NUL 分隔字段读取来源、行号、规则和路径。

每个候选项都会同时打印解析器结果和 Git 结果：

```text
[OK] work/scan/src/order.txt
  Parser: ignored=True  最后规则=work/scan/src/.gitignore:6:order.txt
  Git:    ignored=True  work/scan/src/.gitignore:6:order.txt
```

如果两者不同，则输出 `DIFF`。这种方式很适合继续添加排列组合，观察第三方库和 Git 的语义差异。

## 当前实验覆盖的模式

临时仓库中包含多层 `.gitignore`，目前探索了以下模式：

```gitignore
*.root.tmp
/work/scan/root-only/
shared/
!shared/keep.txt
**/temp?.dat
range[0-2].bin
```

还包括子目录规则：

```gitignore
*.log
scan/build/
!important.log
anchored.txt
folder-only/
```

以及连续覆盖规则：

```gitignore
order.txt
!order.txt
order.txt
```

候选路径包含普通文件、目录、深层文件、通配符命中项、通配符未命中项、父目录已排除项和子目录重新包含项。程序每次运行都会删除并重新创建临时实验目录，因此可以放心修改 `Scenario.Create`，反复执行实验。

## 如何运行

在项目目录执行：

```powershell
dotnet run
```

程序会输出：

1. 临时 Git 仓库路径。
2. 自身没有 `.gitignore` 的扫描起点。
3. 每个候选路径的 `GitignoreParserNet` 判断。
4. `git check-ignore` 的判断和命中规则。
5. 一致项数量和差异项数量。

当前实验预期结果为：

```text
完成: 28 个候选项，0 个差异。
```

// 注: 该结果已在 Git 2.53.0.windows.3 与当前依赖版本 0.2.0.15 下复现，但只能作为这 28 个固定样本的回归基线，不能作为“与标准 Git 等价”的证明。

添加新规则时，建议同时添加命中和不命中的候选路径。例如测试 `temp?.dat` 时，不仅测试 `temp1.dat`，还测试 `temp10.dat`，避免只验证正常路径而遗漏边界。

## 如何继续扩充实验

可以在 `Scenario.Create` 中继续添加组合：

- 带空格的文件名和规则。
- 以 `#`、`!` 开头且经过转义的文件名。
- 规则末尾空格及转义空格。
- 多个 `**` 组合。
- 大小写差异。
- `.gitignore` 使用不同换行格式。
- Git worktree 和子模块中的 `.git` 文件。
- 符号链接和目录循环。
- 无权限访问的目录。
- 大型目录树中的加载性能。

每添加一类规则，都应让 `GitCheckIgnore` 同时参与判断。不要根据对 Git 文档的记忆直接填写预期结果，让真实 Git 提供基准更适合探索项目。

// 注: 上述待扩充项中的若干项已经能复现差异，不再只是潜在边界：规则末尾空格及转义空格、大小写差异、`.git` 文件、符号链接 `.gitignore`、`[!a]` 字符类、非 ASCII `?`、`abc/**` 与根目录否定规则均需要加入正式回归用例。

## 当前实现的边界

这份代码用于探索 `GitignoreParserNet`，还不是完整的 Git 排除规则引擎。目前主要边界如下：

- 只读取仓库中的 `.gitignore`，没有读取 `.git/info/exclude`。
- 没有读取用户级 `core.excludesFile` 全局忽略规则。
- 只把 `.git` 目录识别为仓库标记，暂未处理 `.git` 文件。
- 使用 `SearchOption.AllDirectories` 扫描全部 `.gitignore`，大型仓库中可能产生额外开销。
- 遇到无权限目录时，递归扫描可能抛出异常。
- 当前路径相等比较使用 `OrdinalIgnoreCase`，主要面向 Windows 环境。
- 对注释行、空白和转义的预处理还应增加更多与 Git 对照的实验。
- 没有处理 Git 命令行临时传入的排除规则来源。

// 注: 还应补充以下已确认边界：不读取 Git 索引，因此无法区分已跟踪与未跟踪文件；不读取 `core.ignoreCase`；单条模式对尾随空格、`[!a]`、非 ASCII `?` 和 `abc/**` 存在差异；父目录检查会错误包含仓库根及 `.gitignore` 基准目录；规则快照不会随文件变化刷新；验证器对 Windows 绝对规则源中的冒号解析不安全。

如果要将探索代码用于生产，应按实际需求补齐这些规则来源，并将当前的一次性全仓库扫描改造成可缓存、可增量更新的规则树。

## 总结

使用 `GitignoreParserNet` 实现 Git 风格的目录排除，难点不只在通配符解析，更在规则的作用域和顺序：

- 每份 `.gitignore` 都有自己的相对路径基准。
- 子目录规则只有命中时才能覆盖父目录结果。
- 同一文件中最后匹配的规则优先。
- 父目录已经排除时，不能直接重新包含内部文件。
- 判断目录时必须传递目录信息。

因此，比较合适的职责划分是：让 `GitignoreParserNet` 处理单条模式匹配，让调用方处理仓库发现、规则分层、顺序覆盖、父目录状态和真实 Git 对照。

完整探索代码位于本项目的 `Program.cs`，可以通过修改 `Scenario.Create` 快速增加实验排列组合。

更多技术博客，请参阅 [博客导航](https://blog.lindexi.com/post/%E5%8D%9A%E5%AE%A2%E5%AF%BC%E8%88%AA.html )
