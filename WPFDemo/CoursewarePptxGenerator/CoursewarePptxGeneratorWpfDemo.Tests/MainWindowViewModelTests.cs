using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
    [TestMethod(DisplayName = "打开课件文件夹后应准备页面列表截图附件和美化提示词")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldPrepareSlidesAttachmentsAndPrompt()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-alpha", CreateSlideMarkdown("第一页主题", "第一页 Markdown 内容。"))
            .AddSlide("slide-beta", CreateSlideMarkdown("第二页主题", "第二页 Markdown 内容。"))
            .AddResource("img_1", "image", "picture.png")
            .Build();
        var chatManagerFactory = new FakeSlideChatManagerFactory();
        var firstChatManager = await chatManagerFactory.CreateAsync();
        var viewModel = new MainWindowViewModel(
            chatManagerFactory,
            firstChatManager,
            new CoursewareFolderLoader(),
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.HasCount(2, viewModel.Slides);
        Assert.AreEqual("slide-alpha", viewModel.Slides[0].SlideId);
        Assert.AreEqual("slide-beta", viewModel.Slides[1].SlideId);
        Assert.AreSame(viewModel.Slides[0], viewModel.SelectedSlide);
        Assert.HasCount(1, viewModel.AttachedImageFiles);
        Assert.AreEqual(viewModel.Slides[0].SourceScreenshotFilePath, viewModel.AttachedImageFiles[0].FullName);
        StringAssert.Contains(viewModel.InputText, "页面 Id：slide-alpha");
        StringAssert.Contains(viewModel.InputText, "第一页 Markdown 内容");
        StringAssert.Contains(viewModel.StatusText, "已加载 2 页");

        viewModel.SelectedSlide = viewModel.Slides[1];

        Assert.HasCount(1, viewModel.AttachedImageFiles);
        Assert.AreEqual(viewModel.Slides[1].SourceScreenshotFilePath, viewModel.AttachedImageFiles[0].FullName);
        StringAssert.Contains(viewModel.InputText, "页面 Id：slide-beta");
        StringAssert.Contains(viewModel.InputText, "第二页 Markdown 内容");
    }

    [TestMethod(DisplayName = "打开课件文件夹时聊天管理器初始化失败也应显示页面和美化提示词")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldStillShowSlidesWhenSlideChatManagerCreationFails()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-alpha", CreateSlideMarkdown("第一页主题", "第一页 Markdown 内容。"))
            .AddSlide("slide-beta", CreateSlideMarkdown("第二页主题", "第二页 Markdown 内容。"))
            .Build();
        var firstChatManager = await new FakeSlideChatManagerFactory().CreateAsync();
        var viewModel = new MainWindowViewModel(
            new FailingSlideChatManagerFactory(),
            firstChatManager,
            new CoursewareFolderLoader(),
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.HasCount(2, viewModel.Slides);
        Assert.AreEqual("slide-alpha", viewModel.Slides[0].SlideId);
        Assert.AreSame(viewModel.Slides[0], viewModel.SelectedSlide);
        Assert.AreNotSame(firstChatManager, viewModel.Slides[0].SlideChatManager);
        Assert.AreNotSame(viewModel.Slides[0].SlideChatManager, viewModel.Slides[1].SlideChatManager);
        StringAssert.Contains(viewModel.InputText, "页面 Id：slide-alpha");
        StringAssert.Contains(viewModel.InputText, "第一页 Markdown 内容");
        StringAssert.Contains(viewModel.StatusText, "已加载 2 页");
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 页面信息\n\n- Id: slide-id\n- 尺寸: 1280×720\n- 序号(1-base): 1\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }

}
