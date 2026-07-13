using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareSlideSummaryServiceTests
{
    [TestMethod(DisplayName = "新版页面 Markdown 应从元素内容提取标题")]
    public void CreateTitleShouldUseFirstContentLine()
    {
        var markdown = "## 页面信息\n\n- Id: slide-id\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n任务一·提取信息\n理清思路\n```";
        var service = new CoursewareSlideSummaryService();

        var title = service.CreateTitle(markdown, 1);

        Assert.AreEqual("任务一·提取信息", title);
    }

    [TestMethod(DisplayName = "新版页面 Markdown 应合并多个内容行生成摘要")]
    public void CreateSummaryShouldCombineContentLines()
    {
        var markdown = "## 页面信息\n\n- Id: slide-id\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n````\n第一行\n第二行\n````\n-------\n### 文本.2\n#### 内容\n```\n第三行\n```";
        var service = new CoursewareSlideSummaryService();

        var summary = service.CreateSummary(markdown);

        Assert.AreEqual("第一行 第二行 第三行", summary);
    }

    [TestMethod(DisplayName = "页面没有文本内容时标题应回退到页码")]
    public void CreateTitleShouldFallBackToPageNumber()
    {
        var markdown = "## 页面信息\n\n- Id: slide-id\n\n---\n\n## 元素简要信息\n\n(无元素)";
        var service = new CoursewareSlideSummaryService();

        var title = service.CreateTitle(markdown, 3);

        Assert.AreEqual("第 3 页", title);
    }
}