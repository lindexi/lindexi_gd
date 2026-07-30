using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareSlideContextBudgetValidatorTests
{
    [TestMethod(DisplayName = "课件上下文预算应按原样流式用户 Prompt 估算")]
    [Timeout(60_000)]
    public void ValidateShouldUseUnwrappedCoursewarePrompt()
    {
        var prompt = "# 单页课件美化任务\n用户原样内容";
        var provider = new SlideMlPromptProvider(new SlideDocumentContext(1600, 900));
        provider.UpdatePrompts(null, null, streamingUserPromptTemplate: "{USER_INPUT}");
        var renderTool = new SlideMlRenderTool(
            new FakeSlideMlRenderPipeline(),
            new FakeMainThreadDispatcher());
        var model = new ModelDefinition
        {
            Provider = "test",
            ModelName = "budget-model",
            ContextWindowSize = 100_000,
            MaxOutputTokens = 8_000,
        };

        var budget = CoursewareSlideContextBudgetValidator.ValidateIfConfigured(
            model,
            provider,
            renderTool,
            pageNumber: 1,
            prompt);

        Assert.IsNotNull(budget);
        Assert.AreEqual(CoursewareTokenEstimator.Estimate(prompt), budget.UserPromptTokenCount);
        StringAssert.Contains(provider.BuildStreamingSystemPrompt(), "1600");
        Assert.AreEqual(prompt, provider.BuildStreamingUserPrompt(prompt));
    }

    [TestMethod(DisplayName = "课件上下文超预算时应明确拒绝且不建议静默截断")]
    [Timeout(60_000)]
    public void ValidateShouldRejectOversizedPromptWithoutSilentTruncation()
    {
        var prompt = new string('长', 20_000);
        var provider = new SlideMlPromptProvider(new SlideDocumentContext(1600, 900));
        provider.UpdatePrompts(null, null, streamingUserPromptTemplate: "{USER_INPUT}");
        var renderTool = new SlideMlRenderTool(
            new FakeSlideMlRenderPipeline(),
            new FakeMainThreadDispatcher());
        var model = new ModelDefinition
        {
            Provider = "test",
            ModelName = "small-budget-model",
            ContextWindowSize = 4_000,
            MaxOutputTokens = 1_000,
        };

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CoursewareSlideContextBudgetValidator.ValidateIfConfigured(
                model,
                provider,
                renderTool,
                pageNumber: 3,
                prompt));

        StringAssert.Contains(exception.Message, "第 3 页");
        StringAssert.Contains(exception.Message, "系统不会静默截断当前页 Markdown 或主题");
        StringAssert.Contains(exception.Message, "请缩短用户补充要求或改用更大上下文模型后重试");
    }
}
