using PptxGenerator.Evaluation;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;

namespace PptxGenerator.Tests.Pipeline;

[TestClass]
public sealed class IterationOptionsTests
{
    [TestMethod]
    public void DefaultOptions_HaveExpectedValues()
    {
        var options = new IterationOptions();

        Assert.AreEqual(5, options.MaxRounds);
        Assert.AreEqual(8.0, options.ScoreThreshold);
        Assert.AreEqual(2, options.ConvergenceRounds);
    }

    [TestMethod]
    public void CustomOptions_ArePreserved()
    {
        var options = new IterationOptions
        {
            MaxRounds = 3,
            ScoreThreshold = 7.0,
            ConvergenceRounds = 1,
        };

        Assert.AreEqual(3, options.MaxRounds);
        Assert.AreEqual(7.0, options.ScoreThreshold);
        Assert.AreEqual(1, options.ConvergenceRounds);
    }
}

[TestClass]
public sealed class IterationResultTests
{
    [TestMethod]
    public void IterationResult_DefaultValues()
    {
        var result = new IterationResult();

        Assert.AreEqual(0, result.TotalRounds);
        Assert.AreEqual(0, result.FinalScore);
        Assert.IsFalse(result.IsConverged);
        Assert.AreEqual(string.Empty, result.TerminateReason);
        Assert.IsEmpty(result.IterationHistory);
    }
}

[TestClass]
public sealed class IterationRoundTests
{
    [TestMethod]
    public void IterationRound_RecordsTimestamp()
    {
        var before = DateTimeOffset.Now;
        var round = new IterationRound { Round = 1 };
        var after = DateTimeOffset.Now;

        Assert.AreEqual(1, round.Round);
        Assert.IsTrue(round.Timestamp >= before);
        Assert.IsTrue(round.Timestamp <= after.AddSeconds(1));
    }
}

[TestClass]
public sealed class PipelineConfigurationTests
{
    [TestMethod]
    public void PipelineConfiguration_AllNull_ByDefault()
    {
        var config = new PipelineConfiguration();

        Assert.IsNull(config.SlideEvaluator);
        Assert.IsNull(config.PromptEvaluator);
        Assert.IsNull(config.PromptOptimizer);
    }

    [TestMethod]
    public void PipelineConfiguration_CanSetProperties()
    {
        var stubEvaluator = new StubSlideEvaluator();
        var stubOptimizer = new StubPromptOptimizer();

        var config = new PipelineConfiguration
        {
            SlideEvaluator = stubEvaluator,
            PromptOptimizer = stubOptimizer,
        };

        Assert.AreSame(stubEvaluator, config.SlideEvaluator);
        Assert.AreSame(stubOptimizer, config.PromptOptimizer);
    }

    private sealed class StubSlideEvaluator : ISlideEvaluator
    {
        public Task<SlideEvaluationResult> EvaluateAsync(
            string userPrompt, string slideXml, string renderedXml, string warnings,
            byte[]? previewImageBytes, IPreviewImage? originalScreenshot = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(SlideEvaluationResult.Failed("stub"));
    }

    private sealed class StubPromptOptimizer : IPromptOptimizer
    {
        public Task<PromptOptimizationResult> OptimizeAsync(
            SlideEvaluationResult evaluation, string systemPrompt, string userPromptTemplate,
            CancellationToken cancellationToken = default)
            => Task.FromResult(PromptOptimizationResult.Failed("stub"));
    }
}
