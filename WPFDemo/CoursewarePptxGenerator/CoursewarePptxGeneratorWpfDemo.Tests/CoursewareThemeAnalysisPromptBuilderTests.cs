using System.IO;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeAnalysisPromptBuilderTests
{
    [TestMethod(DisplayName = "主题分析提示词应逐字按序拼接页面并保留路径文本")]
    [Timeout(5000)]
    public void BuildShouldAppendMarkdownVerbatimInInputOrder()
    {
        const string firstMarkdown = "  第一页\nC:\\Users\\demo\\asset.png\n";
        const string secondMarkdown = "\n第二页尾部  ";
        var package = CreatePackage(firstMarkdown, secondMarkdown);
        var summary = new CoursewareStyleUsageSummary
        {
            Fonts = [new CoursewareStyleUsageItem("Arial", 2)],
            FontSizes = [new CoursewareStyleUsageItem("32px", 1)],
            Colors = [new CoursewareStyleUsageItem("#ABCDEF", 3)],
        };

        var prompt = new CoursewareThemeAnalysisPromptBuilder().Build(package, summary);
        var markdownStart = prompt.IndexOf(firstMarkdown, StringComparison.Ordinal);

        Assert.IsTrue(markdownStart >= 0);
        Assert.AreEqual(firstMarkdown + "\n\n---\n\n" + secondMarkdown, prompt[markdownStart..]);
        StringAssert.Contains(prompt, "仅供参考，不要机械继承");
        StringAssert.Contains(prompt, "C:\\Users\\demo\\asset.png");
    }

    [TestMethod(DisplayName = "主题分析提示词不应包含JSON页数资源截图或指纹协议")]
    [Timeout(5000)]
    public void BuildShouldNotContainLegacyEnvelopeMetadata()
    {
        var prompt = new CoursewareThemeAnalysisPromptBuilder().Build(
            CreatePackage("正文"),
            new CoursewareStyleUsageSummary());

        Assert.IsFalse(prompt.Contains("JSON", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(prompt.Contains("页数", StringComparison.Ordinal));
        Assert.IsFalse(prompt.Contains("资源", StringComparison.Ordinal));
        Assert.IsFalse(prompt.Contains("截图", StringComparison.Ordinal));
        Assert.IsFalse(prompt.Contains("fingerprint", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(prompt.Contains("指纹", StringComparison.Ordinal));
    }

    private static CoursewareInputPackage CreatePackage(params string[] markdownPages)
    {
        return new CoursewareInputPackage
        {
            RootDirectory = new DirectoryInfo(Path.GetTempPath()),
            Slides = markdownPages.Select((markdown, index) => new CoursewareSlideInput
            {
                SlideIndex = index,
                PageNumber = index + 1,
                SlideId = $"slide-{index}",
                Width = 1280,
                Height = 720,
                MarkdownText = markdown,
                MarkdownFile = new FileInfo(Path.Join(Path.GetTempPath(), $"slide-{index}.md")),
            }).ToArray(),
        };
    }
}
