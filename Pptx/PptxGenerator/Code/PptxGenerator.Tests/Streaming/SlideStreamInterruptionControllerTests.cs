using PptxGenerator.Streaming;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideStreamInterruptionControllerTests
{
    [TestMethod]
    public void ReportTolerableError_BelowThreshold_DoesNotInterrupt()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);
        controller.StartRound();

        // Act
        var firstResult = controller.ReportTolerableError("err1");
        var secondResult = controller.ReportTolerableError("err2");

        // Assert
        Assert.IsFalse(firstResult);
        Assert.IsFalse(secondResult);
        Assert.IsFalse(controller.IsInterruptionRequested);
    }

    [TestMethod]
    public void ReportTolerableError_AtThreshold_TriggersInterrupt()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);
        controller.StartRound();

        // Act
        var firstResult = controller.ReportTolerableError("err1");
        var secondResult = controller.ReportTolerableError("err2");
        var thirdResult = controller.ReportTolerableError("err3");

        // Assert
        Assert.IsFalse(firstResult);
        Assert.IsFalse(secondResult);
        Assert.IsTrue(thirdResult);
        Assert.IsTrue(controller.IsInterruptionRequested);
        Assert.IsTrue(controller.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void ResetErrorCount_OnSuccess_ClearsCount()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);
        controller.StartRound();
        controller.ReportTolerableError("err1");
        controller.ReportTolerableError("err2");

        // Act
        controller.ResetErrorCount();

        // Assert
        Assert.IsFalse(controller.ReportTolerableError("err1"));
        Assert.IsFalse(controller.ReportTolerableError("err2"));
        Assert.IsFalse(controller.IsInterruptionRequested);
    }

    [TestMethod]
    public void ReportFatalError_ImmediatelyInterrupts()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);
        controller.StartRound();

        // Act
        controller.ReportFatalError("fatal");

        // Assert
        Assert.IsTrue(controller.IsInterruptionRequested);
        Assert.IsTrue(controller.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void CanRetry_BelowMaxRetries_ReturnsTrue()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);
        controller.StartRound();

        // Act
        var result = controller.CanRetry();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CanRetry_AtMaxRetries_ReturnsFalse()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);

        // Act & Assert
        controller.StartRound();
        Assert.AreEqual(0, controller.RetryRound);
        Assert.IsTrue(controller.CanRetry());

        controller.StartRound();
        Assert.AreEqual(1, controller.RetryRound);
        Assert.IsTrue(controller.CanRetry());

        controller.StartRound();
        Assert.AreEqual(2, controller.RetryRound);
        Assert.IsFalse(controller.CanRetry());
    }

    [TestMethod]
    public void StartRound_CreatesNewCts()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController();

        // Act
        controller.StartRound();
        var token1 = controller.Token;

        controller.StartRound();
        var token2 = controller.Token;

        // Assert
        Assert.IsFalse(token1 == token2);
    }

    [TestMethod]
    public void Cancel_TriggersCancellation()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController();
        controller.StartRound();

        // Act
        controller.Cancel();

        // Assert
        Assert.IsTrue(controller.IsInterruptionRequested);
        Assert.IsTrue(controller.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void Constructor_DefaultValues()
    {
        // Arrange & Act
        var controller = new SlideStreamInterruptionController();

        // Assert
        Assert.AreEqual(0, controller.RetryRound);
        Assert.IsFalse(controller.MaxRetriesReached);
        Assert.IsFalse(controller.IsInterruptionRequested);
    }
}
