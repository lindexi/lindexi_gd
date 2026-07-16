using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareAnalysisInputBuilderTests
{
    [TestMethod(DisplayName = "分析输入应包含全部页面标题且不泄露本地路径")]
    public async Task BuildShouldIncludeSlideTitlesWithoutLocalPaths()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("封面标题", "封面内容"))
            .AddSlide("slide-second", CreateSlideMarkdown("章节标题", "章节内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        StringAssert.Contains(result.Prompt, "封面标题");
        StringAssert.Contains(result.Prompt, "章节标题");
        Assert.IsFalse(result.Prompt.Contains(exportDirectory.FullName, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(2, result.AnalyzedSlideCount);
    }

    [TestMethod(DisplayName = "字符预算不足时应记录页面覆盖警告")]
    public async Task BuildShouldReportWarningWhenBudgetCannotIncludeAllSlides()
    {
        var builder = new TestCoursewareExportBuilder();
        for (var index = 0; index < 30; index++)
        {
            builder.AddSlide($"slide-{index}", CreateSlideMarkdown($"标题 {index}", new string('文', 800)));
        }

        var package = await new CoursewareFolderLoader().LoadAsync(builder.Build().FullName);

        var result = new CoursewareAnalysisInputBuilder(4_000).Build(package);

        Assert.IsLessThan(package.SlideCount, result.AnalyzedSlideCount);
        Assert.IsNotEmpty(result.Warnings);
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}