using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlPipelineContextTests
{
    [TestMethod]
    public void DefaultConstructor_DefaultCanvasSize()
    {
        var context = new SlideMlPipelineContext();

        Assert.AreEqual(1280, context.SlideDocumentContext.CanvasWidth);
        Assert.AreEqual(720, context.SlideDocumentContext.CanvasHeight);
    }

    [TestMethod]
    public void CustomCanvasSize_Constructor()
    {
        var context = new SlideMlPipelineContext(1920, 1080);

        Assert.AreEqual(1920, context.SlideDocumentContext.CanvasWidth);
        Assert.AreEqual(1080, context.SlideDocumentContext.CanvasHeight);
    }

    [TestMethod]
    public void AddWarning_WarningAdded()
    {
        var context = new SlideMlPipelineContext();

        context.AddWarning("test warning");

        Assert.HasCount(1, context.Warnings);
        Assert.AreEqual("test warning", context.Warnings[0]);
    }

    [TestMethod]
    public void AddWarnings_Multiple_AllAdded()
    {
        var context = new SlideMlPipelineContext();

        context.AddWarnings(["w1", "w2"]);

        Assert.HasCount(2, context.Warnings);
        Assert.AreEqual("w1", context.Warnings[0]);
        Assert.AreEqual("w2", context.Warnings[1]);
    }

    [TestMethod]
    public void AddError_ErrorAdded()
    {
        var context = new SlideMlPipelineContext();

        context.AddError("test error");

        Assert.HasCount(1, context.Errors);
        Assert.AreEqual("test error", context.Errors[0]);
    }

    [TestMethod]
    public void AddErrors_Multiple_AllAdded()
    {
        var context = new SlideMlPipelineContext();

        context.AddErrors(["e1", "e2"]);

        Assert.HasCount(2, context.Errors);
        Assert.AreEqual("e1", context.Errors[0]);
        Assert.AreEqual("e2", context.Errors[1]);
    }

    [TestMethod]
    public void Reset_ClearsWarningsAndErrors()
    {
        var context = new SlideMlPipelineContext();
        context.AddWarning("w1");
        context.AddError("e1");

        context.Reset();

        Assert.IsEmpty(context.Warnings);
        Assert.IsEmpty(context.Errors);
    }

    [TestMethod]
    public void Warnings_ReadOnly_ExposedAsIReadOnlyList()
    {
        var context = new SlideMlPipelineContext();
        context.AddWarning("w1");
        context.AddError("e1");

        // Warnings/Errors 通过 IReadOnlyList<string> 公开，编译期不暴露 Add/Remove 等修改方法。
        // 验证接口类型以确保只读语义。
        Assert.IsInstanceOfType<IReadOnlyList<string>>(context.Warnings);
        Assert.IsInstanceOfType<IReadOnlyList<string>>(context.Errors);
        Assert.HasCount(1, context.Warnings);
        Assert.HasCount(1, context.Errors);
    }

    [TestMethod]
    public void MaterialResourceManager_NotNull()
    {
        var context = new SlideMlPipelineContext();

        Assert.IsNotNull(context.MaterialResourceManager);
    }
}
