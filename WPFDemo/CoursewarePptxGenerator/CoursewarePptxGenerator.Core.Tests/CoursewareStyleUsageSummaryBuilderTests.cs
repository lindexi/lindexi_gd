using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGenerator.Core.Tests;

[TestClass]
public sealed class CoursewareStyleUsageSummaryBuilderTests
{
    [TestMethod(DisplayName = "样式汇总应只读取代码块外的结构化元数据并规范化排序")]
    [Timeout(5000)]
    public void BuildShouldReadOnlyStructuredMetadataOutsideFences()
    {
        var slides = new[]
        {
            CreateSlide("""
                - 背景颜色: #aabbcc
                ### 文本.1
                字号: 32px | 字体: Microsoft YaHei
                #### 内容
                ```
                字号: 99pt | 字体: Fake Font | #123456
                ```
                -------
                ### 文本.2
                字号: 18pt | 字体: microsoft yahei
                - 填充颜色: #11223344
                ### 形状.1
                字号: 88px | 字体: Shape Font
                - 描边颜色: #abcdef
                """),
            CreateSlide("""
                ### 文本.1
                字号: 32PX | 字体: Arial
                - 颜色: #ABCDEF
                """),
        };

        var summary = new CoursewareStyleUsageSummaryBuilder().Build(slides);

        CollectionAssert.AreEqual(
            new[] { "Microsoft YaHei:2", "Arial:1" },
            summary.Fonts.Select(item => $"{item.Value}:{item.Count}").ToArray());
        CollectionAssert.AreEqual(
            new[] { "32px:2", "18pt:1" },
            summary.FontSizes.Select(item => $"{item.Value}:{item.Count}").ToArray());
        CollectionAssert.AreEqual(
            new[] { "#ABCDEF:2", "#11223344:1", "#AABBCC:1" },
            summary.Colors.Select(item => $"{item.Value}:{item.Count}").ToArray());
    }

    [TestMethod(DisplayName = "样式汇总每类应最多返回十二项")]
    [Timeout(5000)]
    public void BuildShouldLimitEachCategoryToTwelveItems()
    {
        var markdown = string.Join(
            Environment.NewLine,
            Enumerable.Range(1, 15).Select(index => $"### 文本.{index}\n字号: {index}px | 字体: Font{index}\n- 颜色: #{index:X6}"));

        var summary = new CoursewareStyleUsageSummaryBuilder().Build([CreateSlide(markdown)]);

        Assert.AreEqual(12, summary.Fonts.Count);
        Assert.AreEqual(12, summary.FontSizes.Count);
        Assert.AreEqual(12, summary.Colors.Count);
    }

    [TestMethod(DisplayName = "样式汇总应响应已取消令牌")]
    [Timeout(5000)]
    public void BuildShouldObserveCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() =>
            new CoursewareStyleUsageSummaryBuilder().Build([CreateSlide("### 文本.1")], cancellationTokenSource.Token));
    }

    private static CoursewareSlideInput CreateSlide(string markdown)
    {
        return new CoursewareSlideInput { MarkdownText = markdown };
    }
}
