using PptxGenerator.Evaluation;

namespace PptxGenerator.Tests.Evaluation;

[TestClass]
public sealed class PromptOptimizationResultTests
{
    [TestMethod]
    public void Failed_CreatesErrorResult()
    {
        var result = PromptOptimizationResult.Failed("测试错误");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("测试错误", result.ErrorMessage);
        Assert.IsNull(result.OptimizedSystemPrompt);
        Assert.IsNull(result.OptimizedUserPromptTemplate);
    }

    [TestMethod]
    public void DefaultResult_IsSuccess()
    {
        var result = new PromptOptimizationResult();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.ErrorMessage);
    }
}

[TestClass]
public sealed class PromptEvaluationResultTests
{
    [TestMethod]
    public void OverallScore_CalculatesAverage()
    {
        var result = new PromptEvaluationResult
        {
            Clarity = 8,
            Completeness = 6,
            ConstraintQuality = 8,
            Actionability = 7,
            Redundancy = 6,
        };

        Assert.AreEqual(7.0, result.OverallScore, 0.01);
    }

    [TestMethod]
    public void ToDisplayText_Failed_ShowsError()
    {
        var result = PromptEvaluationResult.Failed("网络超时");

        var text = result.ToDisplayText();
        Assert.Contains("失败", text);
        Assert.Contains("网络超时", text);
    }

    [TestMethod]
    public void ToDisplayText_Success_ShowsScores()
    {
        var result = new PromptEvaluationResult
        {
            Clarity = 9,
            Completeness = 8,
            ConstraintQuality = 7,
            Actionability = 8,
            Redundancy = 6,
            Suggestions = new[] { "建议1", "建议2" },
        };

        var text = result.ToDisplayText();
        Assert.Contains("9/10", text);
        Assert.Contains("建议1", text);
        Assert.Contains("建议2", text);
    }
}

[TestClass]
public sealed class SlideEvaluationResultTests
{
    [TestMethod]
    public void OverallScore_IncludesScreenshotFidelity_WhenHasOriginalScreenshot()
    {
        var result = new SlideEvaluationResult
        {
            XmlWellFormedness = 8,
            LayoutStructure = 7,
            VisualBalance = 6,
            ConstraintAdherence = 8,
            SemanticAlignment = 7,
            AestheticQuality = 6,
            ScreenshotFidelity = 5,
            HasOriginalScreenshot = true,
        };

        // (8+7+6+8+7+6+5)/7 ≈ 6.71
        Assert.AreEqual(6.71, result.OverallScore, 0.02);
    }

    [TestMethod]
    public void OverallScore_ExcludesScreenshotFidelity_WhenNoOriginalScreenshot()
    {
        var result = new SlideEvaluationResult
        {
            XmlWellFormedness = 8,
            LayoutStructure = 7,
            VisualBalance = 6,
            ConstraintAdherence = 8,
            SemanticAlignment = 7,
            AestheticQuality = 6,
            ScreenshotFidelity = 5,
            HasOriginalScreenshot = false,
        };

        // (8+7+6+8+7+6)/6 = 7.0
        Assert.AreEqual(7.0, result.OverallScore, 0.02);
    }

    [TestMethod]
    public void ToDisplayText_IncludesScreenshotFidelity()
    {
        var result = new SlideEvaluationResult
        {
            XmlWellFormedness = 8,
            LayoutStructure = 7,
            VisualBalance = 6,
            ConstraintAdherence = 8,
            SemanticAlignment = 7,
            AestheticQuality = 6,
            ScreenshotFidelity = 5,
        };

        var text = result.ToDisplayText();
        Assert.Contains("截图还原", text, "显示文本应包含截图还原度维度");
        Assert.Contains("5/10", text);
    }
}
