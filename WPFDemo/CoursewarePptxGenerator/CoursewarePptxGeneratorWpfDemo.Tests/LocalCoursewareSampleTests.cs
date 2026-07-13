using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class LocalCoursewareSampleTests
{
    [TestMethod(DisplayName = "打开当前格式课件样例后应显示页面并准备每页美化输入")]
    [Timeout(60_000)]
    public async Task OpenCurrentFormatSampleShouldPrepareEachSlideForBeautification()
    {
        var sampleDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页 Markdown 内容。"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页标题", "第二页 Markdown 内容。"))
            .AddResource("img_1", "image", "img_1.png")
            .Build();
        var loader = new CoursewareFolderLoader();
        var chatManagerFactory = new FakeSlideChatManagerFactory();
        var firstChatManager = await chatManagerFactory.CreateAsync();
        var viewModel = new MainWindowViewModel(
            chatManagerFactory,
            firstChatManager,
            loader,
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());

        await viewModel.OpenCoursewareFolderAsync(sampleDirectory.FullName);

        Assert.IsGreaterThan(1, viewModel.Slides.Count, "左侧页面列表应显示足够的页面数量。") ;
        foreach (var slide in viewModel.Slides)
        {
            viewModel.SelectedSlide = slide;
            Assert.IsFalse(string.IsNullOrWhiteSpace(slide.SlideId), "页面 Id 应可见并可断言。") ;
            Assert.IsFalse(string.IsNullOrWhiteSpace(slide.SourceMarkdownText), "每页应加载 Markdown。") ;
            StringAssert.Contains(viewModel.InputText, slide.SlideId);
            StringAssert.Contains(viewModel.InputText, slide.SourceMarkdownText.Trim().Split('\n')[0].Trim());

            if (slide.HasSourceScreenshot)
            {
                Assert.HasCount(1, viewModel.AttachedImageFiles, "有截图的页面应准备附加图片。") ;
                Assert.AreEqual(slide.SourceScreenshotFilePath, viewModel.AttachedImageFiles[0].FullName);
            }
        }
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 页面信息\n\n- Id: slide-id\n- 尺寸: 1280×720\n- 序号(1-base): 1\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}
