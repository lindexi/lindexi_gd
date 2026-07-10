using System.IO;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class LocalCoursewareSampleTests
{
    private const string DefaultSampleDirectory = @"f:\temp\HewelaweekallJawcabayjeeniwhai\CoursewareMarkdownExport_20260707_102013";

    [TestMethod(DisplayName = "打开本机课件样例后应显示页面并准备每页美化输入")]
    [Timeout(60_000)]
    public async Task OpenLocalSampleShouldPrepareEachSlideForBeautification()
    {
        var sampleDirectory = Environment.GetEnvironmentVariable("COURSEWARE_PPTX_GENERATOR_SAMPLE_DIR") ?? DefaultSampleDirectory;
        if (!Directory.Exists(sampleDirectory))
        {
            Assert.Inconclusive($"未找到本机课件样例目录：{sampleDirectory}");
        }

        var chatManagerFactory = new FakeSlideChatManagerFactory();
        var firstChatManager = await chatManagerFactory.CreateAsync();
        var viewModel = new MainWindowViewModel(
            chatManagerFactory,
            firstChatManager,
            new CoursewareFolderLoader(),
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());

        await viewModel.OpenCoursewareFolderAsync(sampleDirectory);

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
}
