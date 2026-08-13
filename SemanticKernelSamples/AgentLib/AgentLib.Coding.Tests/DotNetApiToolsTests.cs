using AgentLib.Coding;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

/// <summary>
/// <see cref="DotNetApiTools"/> 的单元测试。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DotNetApiToolsTests
{
    [TestMethod(DisplayName = "构造工具时工作区为空应抛出参数异常")]
    public void Constructor_WhenWorkspaceIsEmpty_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new DotNetApiTools(" "));
    }

    [TestMethod(DisplayName = "构造工具时工作区不存在应抛出目录异常")]
    public void Constructor_WhenWorkspaceDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "missing");

        Assert.ThrowsExactly<DirectoryNotFoundException>(() => new DotNetApiTools(workspacePath));
    }

    [TestMethod(DisplayName = "工具集合应注册概览和详情工具")]
    public void AsAITools_ReturnsExpectedTools()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string[] names = tools.AsAITools().Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(new[] { "GetDotNetTypeApi", "ListDotNetApi" }, names);
    }

    [TestMethod(DisplayName = "DLL 概览应返回公开类型的自然语言清单")]
    public async Task ListDotNetApiAsync_WhenDllExists_ReturnsPublicTypeList()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);
        string assemblyPath = typeof(DotNetApiToolsTests).Assembly.Location;

        string result = await tools.ListDotNetApiAsync(assemblyPath, nameof(DotNetApiToolsTests));

        StringAssert.Contains(result, "共");
        StringAssert.Contains(result, "已按关键字“DotNetApiToolsTests”过滤");
        StringAssert.Contains(result, "当前为第 1 页，每页 50 个");
        StringAssert.Contains(result, "AgentLib.Coding.Tests.DotNetApiToolsTests（类）");
    }

    [TestMethod(DisplayName = "DLL 相对路径应相对于工作区解析")]
    public async Task ListDotNetApiAsync_WhenDllPathIsRelative_UsesWorkspacePath()
    {
        string workspacePath = CreateTestDirectory();
        string assemblyFileName = Path.GetFileName(typeof(DotNetApiToolsTests).Assembly.Location);
        File.Copy(typeof(DotNetApiToolsTests).Assembly.Location, Path.Join(workspacePath, assemblyFileName));
        var tools = new DotNetApiTools(workspacePath);

        string result = await tools.ListDotNetApiAsync(assemblyFileName, nameof(ApiFixture));

        StringAssert.Contains(result, "DotNetApiToolsTests.ApiFixture（类）");
    }

    [TestMethod(DisplayName = "类型关键字过滤应忽略大小写")]
    public async Task ListDotNetApiAsync_WhenKeywordCaseDiffers_MatchesType()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(ApiFixture).Assembly.Location, "apIfIxTuRe");

        StringAssert.Contains(result, "DotNetApiToolsTests.ApiFixture（类）");
    }

    [TestMethod(DisplayName = "无匹配类型时应返回自然语言空结果")]
    public async Task ListDotNetApiAsync_WhenNoTypeMatches_ReturnsEmptyResultMessage()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(ApiFixture).Assembly.Location, Guid.NewGuid().ToString("N"));

        StringAssert.Contains(result, "过滤为 0 个");
        StringAssert.Contains(result, "没有符合条件的公开类型。");
    }

    [TestMethod(DisplayName = "概览第二页应使用连续序号并提供分页信息")]
    public async Task ListDotNetApiAsync_WhenSecondPageRequested_ReturnsContinuousSequence()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(PagedType01).Assembly.Location, "PagedType", 2);

        StringAssert.Contains(result, "当前为第 2 页，每页 50 个");
        StringAssert.Contains(result, "51. ");
    }

    [TestMethod(DisplayName = "概览非最后一页应提示下一页")]
    public async Task ListDotNetApiAsync_WhenMorePagesExist_ReturnsNextPageHint()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(PagedType01).Assembly.Location, "PagedType", 1);

        StringAssert.Contains(result, "查看下一页请传入 page: 2");
    }

    [TestMethod(DisplayName = "XML 类型摘要超过一百字符时应截断")]
    public async Task ListDotNetApiAsync_WhenSummaryIsLong_TruncatesSummary()
    {
        string workspacePath = CreateAssemblyWorkspace("summary.dll");
        string summary = new('甲', 101);
        await File.WriteAllTextAsync(Path.Join(workspacePath, "summary.xml"), CreateXmlDocumentation(
            $"<member name=\"T:{GetMetadataTypeName(typeof(ApiFixture))}\"><summary>{summary}</summary></member>"));
        var tools = new DotNetApiTools(workspacePath);

        string result = await tools.ListDotNetApiAsync("summary.dll", nameof(ApiFixture));

        StringAssert.Contains(result, $"摘要：{new string('甲', 100)}…");
        Assert.IsFalse(result.Contains(summary, StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "概览应识别公开类型种类")]
    [DataRow(typeof(ApiFixture), "类")]
    [DataRow(typeof(IApiFixture), "接口")]
    [DataRow(typeof(ApiStructFixture), "结构")]
    [DataRow(typeof(ApiEnumFixture), "枚举")]
    [DataRow(typeof(ApiDelegateFixture), "委托")]
    public async Task ListDotNetApiAsync_WhenTypeKindVaries_ReturnsExpectedKind(Type type, string expectedKind)
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(type.Assembly.Location, type.Name);

        StringAssert.Contains(result, $"{GetMetadataTypeName(type)}（{expectedKind}）");
    }

    [TestMethod(DisplayName = "概览不应返回非公开嵌套类型")]
    public async Task ListDotNetApiAsync_WhenTypeIsPrivate_DoesNotReturnType()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(ApiFixture).Assembly.Location, "PrivateApiFixture");

        StringAssert.Contains(result, "过滤为 0 个");
    }

    [TestMethod(DisplayName = "类型详情应返回全部公开成员类别")]
    public async Task GetDotNetTypeApiAsync_WhenTypeExists_ReturnsPublicMembers()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ApiFixture).Assembly.Location, typeof(ApiFixture).FullName!);

        StringAssert.Contains(result, "public sealed class AgentLib.Coding.Tests.DotNetApiToolsTests.ApiFixture");
        StringAssert.Contains(result, "构造函数：");
        StringAssert.Contains(result, "public ApiFixture(string name)");
        StringAssert.Contains(result, "属性：");
        StringAssert.Contains(result, "public string Name { get; }");
        StringAssert.Contains(result, "字段：");
        StringAssert.Contains(result, "public const int DefaultCount = 3");
        StringAssert.Contains(result, "事件：");
        StringAssert.Contains(result, "public event System.EventHandler Changed");
        StringAssert.Contains(result, "方法：");
        StringAssert.Contains(result, "public int Count(string value, int start = 0)");
    }

    [TestMethod(DisplayName = "类型详情不应把属性和事件访问器重复列为方法")]
    public async Task GetDotNetTypeApiAsync_WhenTypeHasAccessors_FiltersAccessorMethods()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ApiFixture).Assembly.Location, typeof(ApiFixture).FullName!);

        Assert.IsFalse(result.Contains("get_Name", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("add_Changed", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("remove_Changed", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "类型详情应返回复杂参数修饰符和泛型约束")]
    public async Task GetDotNetTypeApiAsync_WhenMethodSignatureIsComplex_FormatsSignature()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ApiFixture).Assembly.Location, typeof(ApiFixture).FullName!);

        StringAssert.Contains(result, "public static T Transform<T>(ref int number, out string text, in long value, params string[] items) where T : class, new()");
    }

    [TestMethod(DisplayName = "类型详情应返回运算符签名")]
    public async Task GetDotNetTypeApiAsync_WhenTypeHasOperator_ReturnsOperatorSignature()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(OperatorFixture).Assembly.Location, typeof(OperatorFixture).FullName!);

        StringAssert.Contains(result, "operator +");
    }

    [TestMethod(DisplayName = "类型详情应返回 Obsolete 特性")]
    public async Task GetDotNetTypeApiAsync_WhenMemberIsObsolete_ReturnsObsoleteAttribute()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ObsoleteApiFixture).Assembly.Location, typeof(ObsoleteApiFixture).FullName!);

        StringAssert.Contains(result, "[Obsolete(\"请使用 NewMethod\")]");
    }

    [TestMethod(DisplayName = "类型详情应关联 XML 摘要参数和返回值文档")]
    public async Task GetDotNetTypeApiAsync_WhenXmlDocumentationExists_ReturnsDocumentation()
    {
        string workspacePath = CreateAssemblyWorkspace("documented.dll");
        string typeName = GetMetadataTypeName(typeof(ApiFixture));
        string xml = CreateXmlDocumentation($$"""
            <member name="T:{{typeName}}"><summary>类型摘要。</summary></member>
            <member name="M:{{typeName}}.Count(System.String,System.Int32)">
              <summary>统计字符。</summary>
              <param name="value">待统计文本。</param>
              <param name="start">初始值。</param>
              <returns>统计结果。</returns>
            </member>
            """);
        await File.WriteAllTextAsync(Path.Join(workspacePath, "documented.xml"), xml);
        var tools = new DotNetApiTools(workspacePath);

        string result = await tools.GetDotNetTypeApiAsync("documented.dll", typeof(ApiFixture).FullName!);

        StringAssert.Contains(result, "摘要：类型摘要。");
        StringAssert.Contains(result, "摘要：统计字符。");
        StringAssert.Contains(result, "参数 value：待统计文本。");
        StringAssert.Contains(result, "参数 start：初始值。");
        StringAssert.Contains(result, "返回：统计结果。");
    }

    [TestMethod(DisplayName = "类型详情应接受加号形式的嵌套类型名")]
    public async Task GetDotNetTypeApiAsync_WhenNestedTypeUsesRuntimeName_ReturnsType()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ApiFixture).Assembly.Location, typeof(ApiFixture).FullName!);

        StringAssert.Contains(result, GetMetadataTypeName(typeof(ApiFixture)));
    }

    [TestMethod(DisplayName = "不存在的公开类型应返回明确错误")]
    public async Task GetDotNetTypeApiAsync_WhenTypeDoesNotExist_ReturnsNotFoundMessage()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ApiFixture).Assembly.Location, "Missing.Type");

        Assert.AreEqual("程序集中未找到公开类型“Missing.Type”。", result);
    }

    [TestMethod(DisplayName = "超出范围的页码应返回可操作的页码提示")]
    public async Task ListDotNetApiAsync_WhenPageIsOutOfRange_ReturnsPageRange()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(DotNetApiToolsTests).Assembly.Location, "不存在的类型关键字", 2);

        Assert.AreEqual("页码 2 超出范围：该查询结果共 1 页，请输入 1 到 1 之间的页码。", result);
    }

    [TestMethod(DisplayName = "非正数页码应返回格式错误")]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task ListDotNetApiAsync_WhenPageIsNotPositive_ReturnsValidationMessage(int page)
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(typeof(ApiFixture).Assembly.Location, page: page);

        Assert.AreEqual("页码必须是从 1 开始的正整数。", result);
    }

    [TestMethod(DisplayName = "空来源应返回明确错误")]
    public async Task ListDotNetApiAsync_WhenSourceIsEmpty_ReturnsValidationMessage()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(" ");

        Assert.AreEqual("NuGet 包或 DLL 来源不能为空。", result);
    }

    [TestMethod(DisplayName = "不存在的 DLL 应返回明确错误")]
    public async Task ListDotNetApiAsync_WhenDllDoesNotExist_ReturnsNotFoundMessage()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync("missing.dll");

        Assert.AreEqual("不存在 DLL 文件“missing.dll”。", result);
    }

    [TestMethod(DisplayName = "非托管 DLL 内容应返回元数据错误")]
    public async Task ListDotNetApiAsync_WhenDllIsInvalid_ReturnsMetadataError()
    {
        string workspacePath = CreateTestDirectory();
        await File.WriteAllTextAsync(Path.Join(workspacePath, "invalid.dll"), "not an assembly");
        var tools = new DotNetApiTools(workspacePath);

        string result = await tools.ListDotNetApiAsync("invalid.dll");

        Assert.AreEqual("无法读取“invalid.dll”的 .NET 程序集元数据。", result);
    }

    [TestMethod(DisplayName = "NuGet 来源未指定版本时应返回格式错误")]
    [DataRow("Newtonsoft.Json")]
    [DataRow("Newtonsoft.Json/")]
    [DataRow("/13.0.3")]
    [DataRow("Parent/Newtonsoft.Json/13.0.3")]
    public async Task ListDotNetApiAsync_WhenPackageSourceIsInvalid_ReturnsFormatError(string source)
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.ListDotNetApiAsync(source);

        Assert.AreEqual("NuGet 查询必须指定精确版本，格式为“包名/版本号”。", result);
    }

    [TestMethod(DisplayName = "本地缓存缺少指定包版本时应返回明确错误")]
    public async Task ListDotNetApiAsync_WhenPackageVersionDoesNotExist_ReturnsNotFoundMessage()
    {
        string packageRoot = CreateTestDirectory();
        string? originalValue = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        Environment.SetEnvironmentVariable("NUGET_PACKAGES", packageRoot);
        try
        {
            var tools = new DotNetApiTools(Environment.CurrentDirectory);

            string result = await tools.ListDotNetApiAsync("Sample.Package/1.2.3");

            Assert.AreEqual("本地 NuGet 缓存中不存在包“Sample.Package”版本“1.2.3”。", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", originalValue);
        }
    }

    [TestMethod(DisplayName = "NuGet 包应从 lib 目录读取程序集")]
    public async Task ListDotNetApiAsync_WhenPackageHasLibAssembly_ReadsLibDirectory()
    {
        string packageRoot = CreateTestDirectory();
        string packageDirectory = Path.Join(packageRoot, "sample.package", "1.2.3", "lib", "net10.0");
        Directory.CreateDirectory(packageDirectory);
        File.Copy(typeof(ApiFixture).Assembly.Location, Path.Join(packageDirectory, "Sample.Package.dll"));

        string result = await WithNuGetCacheAsync(packageRoot, tools => tools.ListDotNetApiAsync("Sample.Package/1.2.3", nameof(ApiFixture)));

        StringAssert.Contains(result, "包 Sample.Package/1.2.3");
        StringAssert.Contains(result, "DotNetApiToolsTests.ApiFixture（类）");
    }

    [TestMethod(DisplayName = "NuGet 包同时存在 ref 和 lib 时应优先读取 ref")]
    public async Task GetDotNetTypeApiAsync_WhenPackageHasRefAndLib_PrefersRefDirectory()
    {
        string packageRoot = CreateTestDirectory();
        string packageDirectory = Path.Join(packageRoot, "sample.package", "1.2.3");
        string refDirectory = Path.Join(packageDirectory, "ref", "net10.0");
        string libDirectory = Path.Join(packageDirectory, "lib", "net10.0");
        Directory.CreateDirectory(refDirectory);
        Directory.CreateDirectory(libDirectory);
        File.Copy(typeof(ApiFixture).Assembly.Location, Path.Join(refDirectory, "Sample.Package.dll"));
        File.Copy(typeof(CodingWorkspaceToolProvider).Assembly.Location, Path.Join(libDirectory, "Sample.Package.dll"));

        string result = await WithNuGetCacheAsync(packageRoot, tools => tools.GetDotNetTypeApiAsync(
            "Sample.Package/1.2.3",
            typeof(CodingWorkspaceToolProvider).FullName!));

        Assert.AreEqual($"程序集中未找到公开类型“{typeof(CodingWorkspaceToolProvider).FullName}”。", result);
    }

    [TestMethod(DisplayName = "NuGet 包没有 ref 或 lib DLL 时应返回明确错误")]
    public async Task ListDotNetApiAsync_WhenPackageHasNoAssembly_ReturnsAssemblyNotFoundMessage()
    {
        string packageRoot = CreateTestDirectory();
        Directory.CreateDirectory(Path.Join(packageRoot, "sample.package", "1.2.3", "lib", "net10.0"));

        string result = await WithNuGetCacheAsync(packageRoot, tools => tools.ListDotNetApiAsync("Sample.Package/1.2.3"));

        Assert.AreEqual("NuGet 包“Sample.Package”版本“1.2.3”的 ref 或 lib 目录中没有 DLL 文件。", result);
    }

    [TestMethod(DisplayName = "类型名为空时应返回明确错误")]
    public async Task GetDotNetTypeApiAsync_WhenTypeNameIsEmpty_ReturnsValidationMessage()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);

        string result = await tools.GetDotNetTypeApiAsync(typeof(ApiFixture).Assembly.Location, " ");

        Assert.AreEqual("完整类型名不能为空。", result);
    }

    [TestMethod(DisplayName = "概览取消时应传播取消异常")]
    public async Task ListDotNetApiAsync_WhenCanceled_ThrowsOperationCanceledException()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => tools.ListDotNetApiAsync(
            typeof(DotNetApiToolsTests).Assembly.Location,
            cancellationToken: cancellationTokenSource.Token));
    }

    [TestMethod(DisplayName = "类型详情取消时应传播取消异常")]
    public async Task GetDotNetTypeApiAsync_WhenCanceled_ThrowsOperationCanceledException()
    {
        var tools = new DotNetApiTools(Environment.CurrentDirectory);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => tools.GetDotNetTypeApiAsync(
            typeof(DotNetApiToolsTests).Assembly.Location,
            typeof(DotNetApiToolsTests).FullName!,
            cancellationTokenSource.Token));
    }

    private static async Task<string> WithNuGetCacheAsync(string packageRoot, Func<DotNetApiTools, Task<string>> action)
    {
        string? originalValue = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        Environment.SetEnvironmentVariable("NUGET_PACKAGES", packageRoot);
        try
        {
            return await action(new DotNetApiTools(Environment.CurrentDirectory));
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", originalValue);
        }
    }

    private static string CreateAssemblyWorkspace(string fileName)
    {
        string workspacePath = CreateTestDirectory();
        File.Copy(typeof(ApiFixture).Assembly.Location, Path.Join(workspacePath, fileName));
        return workspacePath;
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Join(Path.GetTempPath(), "AgentLib.Coding.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetMetadataTypeName(Type type) => type.FullName!.Replace('+', '.');

    private static string CreateXmlDocumentation(string members) => $$"""
        <?xml version="1.0"?>
        <doc>
          <assembly><name>AgentLib.Coding.Tests</name></assembly>
          <members>
            {{members}}
          </members>
        </doc>
        """;

    public interface IApiFixture;

    public readonly struct ApiStructFixture;

    public enum ApiEnumFixture
    {
        None,
    }

    public delegate void ApiDelegateFixture();

    public sealed class ApiFixture
    {
        public const int DefaultCount = 3;

        public ApiFixture(string name)
        {
            Name = name;
        }

        public event EventHandler? Changed;

        public string Name { get; }

        public int Count(string value, int start = 0) => value.Length + start;

        public static T Transform<T>(ref int number, out string text, in long value, params string[] items)
            where T : class, new()
        {
            text = value.ToString();
            return new T();
        }

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }

    public readonly struct OperatorFixture
    {
        public static OperatorFixture operator +(OperatorFixture left, OperatorFixture right) => left;
    }

    public sealed class ObsoleteApiFixture
    {
        [Obsolete("请使用 NewMethod")]
        public void OldMethod()
        {
        }

        public void NewMethod()
        {
        }
    }

    private sealed class PrivateApiFixture;

    public sealed class PagedType01; public sealed class PagedType02; public sealed class PagedType03; public sealed class PagedType04; public sealed class PagedType05;
    public sealed class PagedType06; public sealed class PagedType07; public sealed class PagedType08; public sealed class PagedType09; public sealed class PagedType10;
    public sealed class PagedType11; public sealed class PagedType12; public sealed class PagedType13; public sealed class PagedType14; public sealed class PagedType15;
    public sealed class PagedType16; public sealed class PagedType17; public sealed class PagedType18; public sealed class PagedType19; public sealed class PagedType20;
    public sealed class PagedType21; public sealed class PagedType22; public sealed class PagedType23; public sealed class PagedType24; public sealed class PagedType25;
    public sealed class PagedType26; public sealed class PagedType27; public sealed class PagedType28; public sealed class PagedType29; public sealed class PagedType30;
    public sealed class PagedType31; public sealed class PagedType32; public sealed class PagedType33; public sealed class PagedType34; public sealed class PagedType35;
    public sealed class PagedType36; public sealed class PagedType37; public sealed class PagedType38; public sealed class PagedType39; public sealed class PagedType40;
    public sealed class PagedType41; public sealed class PagedType42; public sealed class PagedType43; public sealed class PagedType44; public sealed class PagedType45;
    public sealed class PagedType46; public sealed class PagedType47; public sealed class PagedType48; public sealed class PagedType49; public sealed class PagedType50;
    public sealed class PagedType51; public sealed class PagedType52; public sealed class PagedType53; public sealed class PagedType54; public sealed class PagedType55;
}
