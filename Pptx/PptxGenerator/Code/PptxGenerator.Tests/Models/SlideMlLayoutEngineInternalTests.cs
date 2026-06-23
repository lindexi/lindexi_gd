using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlLayoutEngineInternalTests
{
    [TestMethod]
    public void SlideRectContains_ContainerContainsInner_ReturnsTrue()
    {
        var container = new SlideMlRect(0, 0, 200, 200);
        var inner = new SlideMlRect(10, 10, 100, 100);

        var result = SlideMlLayoutEngine.SlideRectContains(container, inner);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SlideRectContains_InnerExceedsRight_ReturnsFalse()
    {
        var container = new SlideMlRect(0, 0, 200, 200);
        var inner = new SlideMlRect(150, 10, 100, 100);

        var result = SlideMlLayoutEngine.SlideRectContains(container, inner);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SlideRectContains_InnerExceedsBottom_ReturnsFalse()
    {
        var container = new SlideMlRect(0, 0, 200, 200);
        var inner = new SlideMlRect(10, 150, 100, 100);

        var result = SlideMlLayoutEngine.SlideRectContains(container, inner);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SlideRectContains_InnerOutside_ReturnsFalse()
    {
        var container = new SlideMlRect(0, 0, 200, 200);
        var inner = new SlideMlRect(300, 300, 100, 100);

        var result = SlideMlLayoutEngine.SlideRectContains(container, inner);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SlideRectContains_ExactlyAtEdge_ReturnsTrue()
    {
        var container = new SlideMlRect(0, 0, 200, 200);
        var inner = new SlideMlRect(0, 0, 200, 200);

        var result = SlideMlLayoutEngine.SlideRectContains(container, inner);

        Assert.IsTrue(result);
    }

    // --- ResolveOrigin 水平方向 ---

    [TestMethod]
    public void ResolveOrigin_Horizontal_ExplicitOffset()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(0, 1280, 100, 50, SlideMlHorizontalAlignment.Left);

        Assert.AreEqual(50, result);
    }

    [TestMethod]
    public void ResolveOrigin_Horizontal_Center()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(0, 1280, 200, null, SlideMlHorizontalAlignment.Center);

        Assert.AreEqual(540, result);
    }

    [TestMethod]
    public void ResolveOrigin_Horizontal_Right()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(0, 1280, 200, null, SlideMlHorizontalAlignment.Right);

        Assert.AreEqual(1080, result);
    }

    [TestMethod]
    public void ResolveOrigin_Horizontal_Left_Default()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(100, 1280, 200, null, SlideMlHorizontalAlignment.Left);

        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void ResolveOrigin_ElementLargerThanParent_Center_ReturnsZero()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(0, 200, 400, null, SlideMlHorizontalAlignment.Center);

        Assert.AreEqual(0, result);
    }

    // --- ResolveOrigin 垂直方向 ---

    [TestMethod]
    public void ResolveOrigin_Vertical_Center()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(0, 600, 200, null, SlideMlVerticalAlignment.Center);

        Assert.AreEqual(200, result);
    }

    [TestMethod]
    public void ResolveOrigin_Vertical_Bottom()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(0, 600, 200, null, SlideMlVerticalAlignment.Bottom);

        Assert.AreEqual(400, result);
    }

    [TestMethod]
    public void ResolveOrigin_Vertical_Top_Default()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(50, 600, 200, null, SlideMlVerticalAlignment.Top);

        Assert.AreEqual(50, result);
    }

    [TestMethod]
    public void ResolveOrigin_NullAlignment_ReturnsParentOrigin()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(50, 600, 200, null, (SlideMlVerticalAlignment?)null);

        Assert.AreEqual(50, result);
    }
}
