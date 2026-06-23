using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine.ResolveOrigin 逻辑测试。
/// 对应测试用例文档第 9 章。
/// ResolveOrigin 为 internal static 方法，通过 InternalsVisibleTo 直接测试。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineResolveOriginTests
{
    // ───────── 水平方向 ─────────

    [TestMethod]
    public void ResolveOrigin_Horizontal_ExplicitX()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(
            parentOrigin: 0, parentSize: 1280, elementSize: 100,
            explicitOffset: 50, alignment: SlideMlHorizontalAlignment.Left);

        Assert.AreEqual(50, result, 0.01, "显式 X → parentOrigin + X");
    }

    [TestMethod]
    public void ResolveOrigin_Horizontal_AlignmentCenter()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(
            parentOrigin: 0, parentSize: 1280, elementSize: 200,
            explicitOffset: null, alignment: SlideMlHorizontalAlignment.Center);

        Assert.AreEqual((1280 - 200) / 2, result, 0.01, "居中对齐");
    }

    [TestMethod]
    public void ResolveOrigin_Horizontal_AlignmentRight()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(
            parentOrigin: 0, parentSize: 1280, elementSize: 200,
            explicitOffset: null, alignment: SlideMlHorizontalAlignment.Right);

        Assert.AreEqual(1280 - 200, result, 0.01, "右对齐");
    }

    [TestMethod]
    public void ResolveOrigin_Horizontal_ElementLargerThanParent()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(
            parentOrigin: 0, parentSize: 200, elementSize: 400,
            explicitOffset: null, alignment: SlideMlHorizontalAlignment.Center);

        // Math.Max(0, (200-400)/2) = Math.Max(0, -100) = 0
        Assert.AreEqual(0, result, 0.01, "元素比父容器大时不应产生负坐标");
    }

    // ───────── 垂直方向 ─────────

    [TestMethod]
    public void ResolveOrigin_Vertical_AlignmentCenter()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(
            parentOrigin: 0, parentSize: 600, elementSize: 200,
            explicitOffset: null, alignment: SlideMlVerticalAlignment.Center);

        Assert.AreEqual((600 - 200) / 2, result, 0.01, "垂直居中");
    }

    [TestMethod]
    public void ResolveOrigin_Vertical_AlignmentBottom()
    {
        var result = SlideMlLayoutEngine.ResolveOrigin(
            parentOrigin: 0, parentSize: 600, elementSize: 200,
            explicitOffset: null, alignment: SlideMlVerticalAlignment.Bottom);

        Assert.AreEqual(600 - 200, result, 0.01, "底部对齐");
    }
}
