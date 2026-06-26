using PptxGenerator.Models;
using PptxGenerator.Prompt;

namespace PptxGenerator.Tests.Prompt;

[TestClass]
public sealed class SlideMlPromptProviderTests
{
    [TestMethod]
    public void BuildSystemPrompt_WithOverride_ReturnsOverriddenValue()
    {
        var provider = new SlideMlPromptProvider();
        const string customPrompt = "自定义系统提示词";

        provider.UpdatePrompts(systemPrompt: customPrompt, userPromptTemplate: null);

        Assert.AreEqual(customPrompt, provider.BuildSystemPrompt());
    }

    [TestMethod]
    public void BuildInitialUserPrompt_WithOverride_ReplacesPlaceholder()
    {
        var provider = new SlideMlPromptProvider();
        const string template = "请根据图片生成：{USER_INPUT}";

        provider.UpdatePrompts(systemPrompt: null, userPromptTemplate: template);

        var result = provider.BuildInitialUserPrompt("测试占位");
        Assert.AreEqual("请根据图片生成：测试占位", result);
    }

    [TestMethod]
    public void BuildSystemPrompt_PartialUpdate_OnlyAffectsSpecifiedPrompt()
    {
        var provider = new SlideMlPromptProvider();

        // 只更新 systemPrompt
        provider.UpdatePrompts(systemPrompt: "自定义系统", userPromptTemplate: null);
        Assert.AreEqual("自定义系统", provider.BuildSystemPrompt());
        Assert.IsTrue(provider.BuildInitialUserPrompt("X").Contains("SlideML"), "UserPrompt 不应受影响");

        // 只更新 userPromptTemplate
        provider.UpdatePrompts(systemPrompt: null, userPromptTemplate: "自定义用户：{USER_INPUT}");
        Assert.AreEqual("自定义系统", provider.BuildSystemPrompt(), "SystemPrompt 应保持不变");
        Assert.AreEqual("自定义用户：X", provider.BuildInitialUserPrompt("X"));
    }

    [TestMethod]
    public void ResetToDefault_RestoresBothPrompts()
    {
        var provider = new SlideMlPromptProvider();
        var originalSystemPrompt = provider.BuildSystemPrompt();
        var originalUserPrompt = provider.BuildInitialUserPrompt("测试");

        provider.UpdatePrompts("覆盖系统", "覆盖用户：{USER_INPUT}");
        provider.ResetToDefault();

        Assert.AreEqual(originalSystemPrompt, provider.BuildSystemPrompt());
        Assert.AreEqual(originalUserPrompt, provider.BuildInitialUserPrompt("测试"));
    }

    [TestMethod]
    public void BuildSystemPrompt_CustomCanvasSize_ContainsCorrectDimensions()
    {
        var context = new SlideDocumentContext(1920, 1080);
        var provider = new SlideMlPromptProvider(context);
        var result = provider.BuildSystemPrompt();

        Assert.IsTrue(result.Contains("1920×1080"), "系统提示词应包含自定义画布尺寸");
        Assert.IsFalse(result.Contains("1280×720"), "系统提示词不应包含默认画布尺寸");
    }

    [TestMethod]
    public void BuildInitialUserPrompt_CustomCanvasSize_ContainsCorrectDimensions()
    {
        var context = new SlideDocumentContext(1920, 1080);
        var provider = new SlideMlPromptProvider(context);
        var result = provider.BuildInitialUserPrompt("测试");

        Assert.IsTrue(result.Contains("1920x1080"), "用户提示词应包含自定义画布尺寸");
        Assert.IsFalse(result.Contains("1280x720"), "用户提示词不应包含默认画布尺寸");
    }
}
